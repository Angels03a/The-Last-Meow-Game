using System;
using System.IO;
using System.ServiceModel;

using log4net;
using log4net.Config;

using TLMG.Services;

namespace TLMG.Server
{
    /// <summary>
    /// Host de consola que levanta el servidor WCF con Self-Host.
    /// </summary>
    public class Program
    {
        private const string LogConfigFileName = "log4net.config";

        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// Configura el log, abre el ServiceHost y lo mantiene activo hasta que se presiona Enter.
        /// </summary>
        /// <param name="args">Argumentos de la línea de comandos; no se usan.</param>
        public static void Main(string[] args)
        {
            ConfigureLogging();

            ServiceHost host = null;

            try
            {
                Bootstrapper.Initialize();

                host = new ServiceHost(typeof(ServiceImplementation));
                host.Open();

                _log.Info("El servidor está en ejecución. Presiona Enter para detenerlo.");
                Console.ReadLine();
            }
            catch (AddressAlreadyInUseException ex)
            {
                _log.Fatal("El puerto del servidor ya está en uso.", ex);
            }
            catch (InvalidOperationException ex)
            {
                _log.Fatal("La configuración del servicio es inválida.", ex);
            }
            catch (CommunicationException ex)
            {
                _log.Fatal("No se pudo abrir el servidor.", ex);
            }
            catch (TimeoutException ex)
            {
                _log.Fatal("Se agotó el tiempo al abrir el servidor.", ex);
            }
            finally
            {
                CloseHost(host);
            }
        }

        private static void ConfigureLogging()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogConfigFileName);

            XmlConfigurator.Configure(new FileInfo(configPath));
        }

        private static void CloseHost(ServiceHost host)
        {
            if (host != null)
            {
                try
                {
                    if (host.State == CommunicationState.Faulted)
                    {
                        host.Abort();
                    }
                    else
                    {
                        host.Close();
                    }
                }
                catch (CommunicationException ex)
                {
                    _log.Warn("El servidor no se cerró limpiamente.", ex);
                    host.Abort();
                }
                catch (TimeoutException ex)
                {
                    _log.Warn("Se agotó el tiempo al cerrar el servidor.", ex);
                    host.Abort();
                }
            }
        }
    }
}