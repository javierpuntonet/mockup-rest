using SimpleLogger;
using SimpleLogger.Logging.Handlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VocaliRestServer.Modelos;

namespace VocaliRestServer
{
    public partial class Servicio : ServiceBase
    {
        HttpListener httpListener = null;
        BackgroundWorker httpServer = null;
        BackgroundWorker envioFicheros = null;

        /*
         * Un BlockingCollection de tamaño 3 (iniciado posteriormente). Se añade un elemento antes de enviarlo al servidor REST y se elimina al terminar dicho servicio.
         * De esta manera aseguramos que nunca habrá más de 3 Tasks procesándose simultaneamente en llamadas al servidor REST.
         */ 
        BlockingCollection<FicheroMP3> ficherosAProcesar = null;

        public Servicio()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Logger.LoggerHandlerManager.AddHandler(new FileLoggerHandler(System.AppDomain.CurrentDomain.BaseDirectory + "log.txt"));
                BD.Init();
                httpListener = new HttpListener();
                httpListener.Prefixes.Add("http://*:8080/");
                httpListener.Start();
                httpServer = new BackgroundWorker();
                httpServer.DoWork += httpServer_DoWork;
                httpServer.RunWorkerAsync();
                envioFicheros = new BackgroundWorker();
                envioFicheros.DoWork += envioFicheros_DoWork;
                envioFicheros.RunWorkerAsync();
                ficherosAProcesar = new BlockingCollection<FicheroMP3>(3);
                Logger.Log("Servicio iniciado correctamente");
            }
            catch (System.Exception ex)
            {
                ExitCode = ((Win32Exception)ex).ErrorCode;
                Stop();
            }
        }

        /// <summary>
        /// Hilo encargado de procesar los ficheros y enviarlo al servidor REST (Mockup)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void envioFicheros_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                DateTime hoy = DateTime.Today;
                DateTime ahora = DateTime.Now;
                DateTime manana = DateTime.Today.AddDays(1);
                Logger.Log("Servicio de Envío en espera por: " + (manana-ahora).TotalHours + " horas");
                Thread.Sleep(manana - ahora);
                IEnumerable<FicheroMP3> ficheros = BD.GetLista(hoy, manana);
                foreach (FicheroMP3 f in ficheros)
                {
                    ficherosAProcesar.Add(f);
                    Logger.Log("Fichero añadido a la cola de envío al servidor REST");
                    Task t = new Task(() =>
                    {
                        f.Estado = FicheroMP3.EstadosFicheroMP3.EnProgreso;
                        BD.ActualizaFicheroMP3(f);
                        Logger.Log("Fichero enviado al servidor REST");
                        byte[] contenido = File.ReadAllBytes(System.AppDomain.CurrentDomain.BaseDirectory + f.Id + ".mp3");
                        RespuestaTranscripcion rt = MockupServer.Enviar(contenido);
                        //Se simula una espera de 2 segundos en la respuesta del servidor REST para comprobar que efectivamente nunca se procesan más de 3 simultaneamente.
                        Thread.Sleep(2000);
                        if (rt.Codigo == 200)
                        {
                            f.FechaTranscripcion = DateTime.Now;
                            f.Transcripcion = rt.Transcripcion;
                            f.Estado = FicheroMP3.EstadosFicheroMP3.Realizada;
                            BD.ActualizaFicheroMP3(f);
                        }
                        else
                        {
                            f.Estado = FicheroMP3.EstadosFicheroMP3.Error;
                            BD.ActualizaFicheroMP3(f);
                        }
                        Logger.Log("Fichero procesado en servidor REST");
                        ficherosAProcesar.Take();
                    });
                    t.Start();
                }
            }
        }

        /// <summary>
        /// Hilo servidor HTTP. Procesa las entradas HTTP y las envía a un Task para que se ejecuten
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void httpServer_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                HttpListenerContext context = httpListener.GetContext();
                Task t = new Task(() =>
                {
                    HttpListenerCallback(context);
                });
                t.Start();
            }
        }

        /// <summary>
        /// Tarea encargada de ejecutar el proceso HTTP
        /// </summary>
        /// <param name="context"></param>
        private void HttpListenerCallback(HttpListenerContext context)
        {
            byte[] respuesta = new byte[0];
            String data_text = new StreamReader(context.Request.InputStream,
                context.Request.ContentEncoding).ReadToEnd();

            try
            {
                String[] ruta = context.Request.RawUrl.Split('/');

                //La ruta es /fichero/{usuario} y es un POST
                if (ruta[1].CompareTo("fichero") == 0 && context.Request.HttpMethod.CompareTo("POST") == 0 && ruta.Length == 3)
                {
                    Logger.Log("Servicio POST fichero iniciado");
                    byte[] fichero = Convert.FromBase64String(data_text);

                    if (fichero.Length > 5 * 1024 * 1024)
                    {
                        context.Response.StatusCode = 413;
                        context.Response.StatusDescription = "Request Entity Too Large";
                        RespuestaError re = new RespuestaError()
                        {
                            Error = "El fichero no puede ser más grande de 5 MB"
                        };
                        respuesta = Funciones.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(re));
                    }

                    else
                    {
                        FicheroMP3 ficheroMP3 = new FicheroMP3()
                        {
                            Usuario = ruta[2],
                            FechaRecepcion = DateTime.UtcNow,
                            Estado = FicheroMP3.EstadosFicheroMP3.Pendiente,
                            FechaTranscripcion = null,
                            Transcripcion = null
                        };

                        BD.AddFicheroMP3(ficheroMP3);

                        using (FileStream fs = new FileStream(System.AppDomain.CurrentDomain.BaseDirectory + ficheroMP3.Id + ".mp3", FileMode.Create, FileAccess.Write))
                        {
                            fs.Write(fichero, 0, fichero.Length);
                        }

                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "Ok";
                    }
                }
                //La ruta es /ficheros/{usuario} y es un GET. Los parámetros desde y hasta al ser opcionales no van en formato Friendly, sino en Query Parameters
                else if (ruta[1].CompareTo("ficheros") == 0 && context.Request.HttpMethod.CompareTo("GET") == 0 && ruta.Length == 3)
                {
                    Logger.Log("Servicio GET ficheros iniciado");
                    DateTime? desde = null;
                    DateTime? hasta = null;
                    Boolean paramsOK = true;

                    if (context.Request.QueryString["desde"] != null)
                    {
                        desde = Funciones.CheckParamFecha(context.Request.QueryString["desde"], ref respuesta);
                        if (desde == null)
                            paramsOK = false;
                    }
                    if (context.Request.QueryString["hasta"] != null)
                    {
                        hasta = Funciones.CheckParamFecha(context.Request.QueryString["hasta"], ref respuesta);

                        if (hasta == null)
                            paramsOK = false;
                    }

                    if (paramsOK)
                    {
                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "Ok";
                        respuesta = Funciones.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(BD.GetLista(ruta[2].Split('?')[0], desde, hasta)));
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.StatusDescription = "Bad Request";
                    }
                }
                //La ruta es /fichero/{usuario}/{id} y es un GET
                else if (ruta[1].CompareTo("fichero") == 0 && context.Request.HttpMethod.CompareTo("GET") == 0 && ruta.Length == 4)
                {
                    Logger.Log("Servicio GET fichero iniciado");
                    Int32 id;

                    if (Int32.TryParse(ruta[3], out id))
                    {
                        RespuestaTranscripcion rt = BD.GetTranscripcion(ruta[2], id);
                        if (rt.Codigo == 0)
                        {
                            context.Response.StatusCode = 200;
                            context.Response.StatusDescription = "Ok";
                            respuesta = Funciones.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(rt));
                        }
                        else if (rt.Codigo == -1)
                        {
                            context.Response.StatusCode = 404;
                            context.Response.StatusDescription = "Not Found";
                        }
                        else if (rt.Codigo == -2)
                        {
                            context.Response.StatusCode = 410;
                            context.Response.StatusDescription = "Gone";
                        }
                        else
                        {
                            context.Response.StatusCode = 204;
                            context.Response.StatusDescription = "No Content";
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.StatusDescription = "Bad Request";
                    }
                }
                //Cualquier otro caso la ruta no existe y se devuelve Not Found.
                else
                {
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Not Found";
                }
            }
            catch (System.Exception ex)
            {
                //Si se produce una excepción se devuelve un Internal Server Error (500)
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";

                respuesta = Funciones.GetBytes(ex.ToString());
            }
            
            context.Response.ContentLength64 = respuesta.Length;
            context.Response.OutputStream.Write(respuesta, 0, respuesta.Length);
            context.Response.Close();
        }

        protected override void OnStop()
        {

        }
    }
}
