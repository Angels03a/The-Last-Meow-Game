using System;

using log4net;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace TLMG.Server
{
    /// <summary>
    /// Punto de entrada del servidor de The Last Meow.
    /// </summary>
    public static class Program
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// Registra el inicio del servidor y lo mantiene en ejecución hasta que se presiona Enter.
        /// </summary>
        /// <param name="args">Argumentos de la línea de comandos; por ahora no se usan.</param>
        public static void Main(string[] args)
        {
            _log.Info("El servidor inició correctamente.");
            Console.WriteLine("Servidor de The Last Meow en ejecución. Presiona Enter para terminar.");
            Console.ReadLine();
            _log.Info("El servidor se detuvo.");
        }
    }
}
