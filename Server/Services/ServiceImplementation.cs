using System;
using System.ServiceModel;
using log4net;

namespace TLMG.Services
{
    /// <summary>
    /// Implementación de todos los contratos del servidor; cada contrato vive en su propio archivo partial.
    /// </summary>
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.PerSession,
        ConcurrencyMode = ConcurrencyMode.Single)]
    public partial class ServiceImplementation
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ServiceImplementation));

        /// <summary>
        /// Envía una notificación al cliente sin que un cliente caído afecte al servidor.
        /// </summary>
        /// <param name="notificationOfResult">Invocación del callback.</param>
        private void SafeNotify(Action notificationOfResult) 
        {
            try
            {
                notificationOfResult(); 
            }
            catch (CommunicationObjectAbortedException ex)
            {
                _log.Warn("El cliente cerró el canal antes de recibir la notificación.", ex);
            }
            catch (CommunicationObjectFaultedException ex) 
            {
                _log.Warn("El canal del cliente está en falla.", ex);
            }
            catch (CommunicationException ex) 
            {
                _log.Warn("Error de comunicación al notificar al cliente, verifque la conexión.", ex);
            }
            catch (TimeoutException ex)
            {
                _log.Warn("El cliente tardó demasiado en recibir la notificación.", ex);
            }
            catch (ObjectDisposedException ex) 
            {
                _log.Warn("El canal del cliente ya fue liberado.", ex);
            }
        }
    }
}