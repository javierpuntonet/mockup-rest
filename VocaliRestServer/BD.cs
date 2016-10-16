using SimpleLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VocaliRestServer.Modelos;

namespace VocaliRestServer
{
    public class BD
    {
        private static LiteDB.LiteDatabase db = null;

        /// <summary>
        /// Inicia la BD
        /// </summary>
        public static void Init() {
            if (db == null)
            {
                db = new LiteDB.LiteDatabase(System.AppDomain.CurrentDomain.BaseDirectory + "datos.db");
                Logger.Log("Base de datos iniciada");
            }
        }

        /// <summary>
        /// Constructor privado para que se utilize el método Init
        /// </summary>
        private BD()
        {
            
        }

        /// <summary>
        /// Añade un fichero mp3 a la BD
        /// </summary>
        /// <param name="fichero"></param>
        /// <returns></returns>
        public static FicheroMP3 AddFicheroMP3(FicheroMP3 fichero)
        {
            LiteDB.LiteCollection<FicheroMP3> ficheros = db.GetCollection<FicheroMP3>("ficheros");
            ficheros.Insert(fichero);
            ficheros.EnsureIndex(f => f.Usuario);
            Logger.Log("Añadido fichero a la Base de datos");
            return fichero;
        }

        /// <summary>
        /// Actualiza un fichero mp3 ya existente
        /// </summary>
        /// <param name="fichero"></param>
        /// <returns></returns>
        public static FicheroMP3 ActualizaFicheroMP3(FicheroMP3 fichero)
        {
            LiteDB.LiteCollection<FicheroMP3> ficheros = db.GetCollection<FicheroMP3>("ficheros");
            ficheros.Update(fichero);
            Logger.Log("Actualizado fichero en la Base de datos");
            return fichero;
        }

        /// <summary>
        /// Devuelve una lista de todos los ficheros MP3 entre 2 fechas (opcionales) de todos los usuarios
        /// </summary>
        /// <param name="desde"></param>
        /// <param name="hasta"></param>
        /// <returns></returns>
        public static IEnumerable<FicheroMP3> GetLista(DateTime? desde, DateTime? hasta)
        {
            LiteDB.LiteCollection<FicheroMP3> ficheros = db.GetCollection<FicheroMP3>("ficheros");
            IEnumerable<FicheroMP3> listaTemporal = ficheros.FindAll();
            if (desde != null)
            {
                listaTemporal = listaTemporal.Where(f => f.FechaRecepcion >= desde.Value);
            }
            if (hasta != null)
            {
                listaTemporal = listaTemporal.Where(f => f.FechaRecepcion <= hasta.Value);
            }
            Logger.Log("Obtenida lista de la Base de datos");
            return listaTemporal;
        }

        /// <summary>
        /// Devuelve una lista de todos los ficheros MP3 entre 2 fechas (opcionales) de un usuario concreto
        /// </summary>
        /// <param name="usuario"></param>
        /// <param name="desde"></param>
        /// <param name="hasta"></param>
        /// <returns></returns>
        public static IEnumerable<FicheroMP3> GetLista(String usuario, DateTime? desde, DateTime? hasta)
        {
            LiteDB.LiteCollection<FicheroMP3> ficheros = db.GetCollection<FicheroMP3>("ficheros");
            IEnumerable<FicheroMP3> listaTemporal = ficheros.Find(f => f.Usuario == usuario);
            if (desde != null) 
            {
                listaTemporal = listaTemporal.Where(f => f.FechaRecepcion >= desde.Value);
            }
            if (hasta != null)
            {
                listaTemporal = listaTemporal.Where(f => f.FechaRecepcion <= hasta.Value);
            }
            Logger.Log("Obtenida lista de la Base de datos");
            return listaTemporal;
        }

        /// <summary>
        /// Devuelve la transcripción de un fichero mp3 a partir de su usuario y su id. El usuario se usa por seguridad.
        /// </summary>
        /// <param name="usuario"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static RespuestaTranscripcion GetTranscripcion(String usuario, Int32 id)
        {
            Logger.Log("Solicitada transcripción de la Base de datos");
            LiteDB.LiteCollection<FicheroMP3> ficheros = db.GetCollection<FicheroMP3>("ficheros");
            FicheroMP3 fichero = ficheros.Find(f => f.Usuario == usuario && f.Id == id).FirstOrDefault();
            if (fichero == null)
            {
                return new RespuestaTranscripcion()
                {
                    Codigo = -1,
                    Transcripcion = null
                };
            }
            else if (fichero.Estado == FicheroMP3.EstadosFicheroMP3.Realizada)
            {
                return new RespuestaTranscripcion()
                {
                    Codigo = 0,
                    Transcripcion = fichero.Transcripcion
                };
            }
            else if (fichero.Estado == FicheroMP3.EstadosFicheroMP3.Error)
            {
                return new RespuestaTranscripcion()
                {
                    Codigo = -2,
                    Transcripcion = null
                };
            }
            else
            {
                return new RespuestaTranscripcion()
                {
                    Codigo = -3,
                    Transcripcion = null
                };
            }
        }
    }
}
