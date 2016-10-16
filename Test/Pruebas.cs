using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VocaliRestServer;
using VocaliRestServer.Modelos;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    [TestClass]
    public class Pruebas
    {
        [TestMethod]
        public void TestMockupServer()
        {
            RespuestaTranscripcion rt = MockupServer.Enviar(new byte[] { 0x00 });
            Assert.AreEqual(rt.Codigo, 200);
        }

        [TestMethod]
        public void TestAnadirArchivoMP3()
        {
            BD.Init();
            FicheroMP3 fichero = BD.AddFicheroMP3(new FicheroMP3()
            {
                Estado = FicheroMP3.EstadosFicheroMP3.Pendiente,
                FechaRecepcion = DateTime.Now,
                Usuario = "Test"
            });
            Assert.IsTrue(fichero.Id > 0);
        }

        [TestMethod]
        public void TestHoyHayArchivos()
        {
            BD.Init();
            IEnumerable<FicheroMP3> lista = BD.GetLista(DateTime.Today, DateTime.Today.AddDays(1));
            Assert.IsTrue(lista.Count() > 0);
        }

        [TestMethod]
        public void TestFecha1EneroEsValida()
        {
            byte[] respuesta = new byte[0];
            DateTime? fecha = Funciones.CheckParamFecha("2016-01-01", ref respuesta);
            Assert.IsNotNull(fecha);
            Assert.IsTrue(respuesta.Length == 0);
        }

        [TestMethod]
        public void TestFecha30FebreroNoEsValida()
        {
            byte[] respuesta = new byte[0];
            DateTime? fecha = Funciones.CheckParamFecha("2016-02-30", ref respuesta);
            Assert.IsNull(fecha);
            Assert.IsTrue(respuesta.Length > 0);
        }
    }
}
