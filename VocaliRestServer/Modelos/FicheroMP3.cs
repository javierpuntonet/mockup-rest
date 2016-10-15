using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VocaliRestServer.Modelos
{
    public class FicheroMP3
    {
        public enum EstadosFicheroMP3
        {
            Pendiente = 1,
            EnProgreso = 2,
            Realizada = 3,
            Error = 4,
        }

        public int Id { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaRecepcion { get; set; }
        public EstadosFicheroMP3 Estado { get; set; }
        public DateTime? FechaTranscripcion { get; set; }
        [JsonIgnore]
        public String Transcripcion { get; set; }
    }
}
