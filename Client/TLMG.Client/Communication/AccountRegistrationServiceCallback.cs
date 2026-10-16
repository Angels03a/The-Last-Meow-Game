using System;
using System.ServiceModel;

using TLMG.Client.TLMGService;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Implementación del callback del registro; el servidor invoca sus métodos.
    /// </summary>
    [CallbackBehavior(UseSynchronizationContext = false)]
    public class AccountRegistrationServiceCallback : IAccountRegistrationServiceCallback
    {
        /// <summary>Se produce cuando el servidor responde al registro.</summary>
        public event EventHandler<RegistrationResultEventArgs> RegistrationResultReceived;

        /// <summary>Recibe el resultado del servidor y lo anuncia a quien esté suscrito.</summary>
        /// <param name="result">Resultado con éxito o clave de error.</param>
        public void OnRegisterResult(OperationResultData result)
        {
            RegistrationResultReceived?.Invoke(this, new RegistrationResultEventArgs(result));
        }
    }
}