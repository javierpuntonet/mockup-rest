using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VocaliRestServer.Modelos
{
    public class RespuestaTranscripcion
    {
        [JsonIgnore]
        public int Codigo { get; set; }
        public String Transcripcion { get; set; }
    }
}
