using SimpleLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VocaliRestServer.Modelos;

namespace VocaliRestServer
{
    public class MockupServer
    {
        public static RespuestaTranscripcion Enviar(FicheroMP3 fichero)
        {
            Logger.Log("Procesamiento del fichero en el Mockup Base de datos");
            Random random = new Random();
            int probabilidad = random.Next(101);
            if (probabilidad <= 5)
            {
                return new RespuestaTranscripcion()
                {
                    Codigo = 500,
                    Transcripcion = null
                };
            }
            else
            {
                probabilidad = random.Next(4);
                switch (probabilidad)
                {
                    case 0:
                        return new RespuestaTranscripcion()
                        {
                            Codigo = 200,
                            Transcripcion = "El paciente ha mejorado"
                        };
                    case 1:
                        return new RespuestaTranscripcion()
                        {
                            Codigo = 200,
                            Transcripcion = "El paciente ha empeorado"
                        };
                    case 2:
                        return new RespuestaTranscripcion()
                        {
                            Codigo = 200,
                            Transcripcion = "El paciente no sigue el tratamiento"
                        };
                    default:
                        return new RespuestaTranscripcion()
                        {
                            Codigo = 200,
                            Transcripcion = "El paciente ha muerto"
                        };
                }
            }
        }
    }
}
