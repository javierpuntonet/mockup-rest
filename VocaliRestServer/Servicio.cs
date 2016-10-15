using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using VocaliRestServer.Modelos;

namespace VocaliRestServer
{
    public partial class Servicio : ServiceBase
    {
        HttpListener httpListener = null;
        BackgroundWorker httpServer = null;

        public Servicio()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            BD.Init();
            httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://*:8080/");
            try
            {
                httpListener.Start();
                httpServer = new BackgroundWorker();
                httpServer.DoWork += httpServer_DoWork;
                httpServer.RunWorkerAsync();
            }
            catch (System.Exception ex)
            {
                ExitCode = ((Win32Exception)ex).ErrorCode;
                Stop();
            }
        }

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

        private void HttpListenerCallback(HttpListenerContext context)
        {
            byte[] respuesta = null;
            String data_text = new StreamReader(context.Request.InputStream,
                context.Request.ContentEncoding).ReadToEnd();

            try
            {
                String[] ruta = context.Request.RawUrl.Split('/');

                if (ruta[1].CompareTo("fichero") == 0 && context.Request.HttpMethod.CompareTo("POST") == 0 && ruta.Length == 3)
                {
                    byte[] fichero = Convert.FromBase64String(data_text);

                    if (fichero.Length > 5 * 1024 * 1024)
                    {
                        context.Response.StatusCode = 413;
                        context.Response.StatusDescription = "Request Entity Too Large";
                        //TODO: Respuesta
                    }

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
                else if (ruta[1].CompareTo("ficheros") == 0 && context.Request.HttpMethod.CompareTo("GET") == 0 && ruta.Length >= 3)
                {
                    context.Response.StatusCode = 200;
                    context.Response.StatusDescription = "Ok";
                    //TODO: Procesar fechas
                    respuesta = Funciones.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(BD.GetLista(ruta[2])));
                }
                else if (ruta[1].CompareTo("fichero") == 0 && context.Request.HttpMethod.CompareTo("GET") == 0 && ruta.Length == 4)
                {
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
                }
                else
                {
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Not Found";
                }
            }
            catch (System.Exception ex)
            {
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Error";

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
