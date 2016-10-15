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

        public static void Init() {
            if (db == null)
            {
                db = new LiteDB.LiteDatabase(System.AppDomain.CurrentDomain.BaseDirectory + "datos.db");
            }
        }

        private BD()
        {
            
        }

        public static FicheroMP3 AddFicheroMP3(FicheroMP3 fichero)
        {
            LiteDB.LiteCollection<FicheroMP3> ficheros = db.GetCollection<FicheroMP3>("ficheros");
            ficheros.Insert(fichero);
            ficheros.EnsureIndex(f => f.Usuario);
            return fichero;
        }

        public static IEnumerable<FicheroMP3> GetLista(String usuario)
        {
            LiteDB.LiteCollection<FicheroMP3> ficheros = db.GetCollection<FicheroMP3>("ficheros");
            return ficheros.Find(f => f.Usuario == usuario);
        }

        public static RespuestaTranscripcion GetTranscripcion(String usuario, Int32 id)
        {
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
