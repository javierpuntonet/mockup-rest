using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VocaliRestServer
{
    public class Funciones
    {
        public static byte[] GetBytes(string cadena)
        {
            return System.Text.Encoding.UTF8.GetBytes(cadena);
        }
    }
}
