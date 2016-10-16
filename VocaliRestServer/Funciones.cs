using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VocaliRestServer.Modelos;

namespace VocaliRestServer
{
    public class Funciones
    {
        /// <summary>
        /// Devuelve los Bytes de una cadena
        /// </summary>
        /// <param name="cadena"></param>
        /// <returns></returns>
        public static byte[] GetBytes(string cadena)
        {
            return System.Text.Encoding.UTF8.GetBytes(cadena);
        }

        /// <summary>
        /// Comprueba que la fecha es válida y genera una respuesta de parámetros incorrectos si no lo es.
        /// </summary>
        /// <param name="fecha"></param>
        /// <param name="respuesta"></param>
        /// <returns>Devuelve NULL si la fecha no es válida. Un DateTime si sí lo es</returns>
        public static DateTime? CheckParamFecha(String fecha, ref byte[] respuesta)
        {
            DateTime fechaOut;
            if (!DateTime.TryParse(fecha, out fechaOut))
            {
                RespuestaError re = new RespuestaError()
                {
                    Error = "Los parámetros de fecha no tienen el formato correcto. Formato necesario: YYYY-MM-DD"
                };
                respuesta = Funciones.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(re));
                return null;
            }
            else return fechaOut;
        }
    }
}
