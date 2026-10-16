using System;

using TLMG.Client.TLMGService;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Datos del evento que anuncia la respuesta del servidor al registro.
    /// </summary>
    public class RegistrationResultEventArgs : EventArgs
    {
        /// <summary>Crea los argumentos con el resultado recibido.</summary>
        /// <param name="result">Resultado enviado por el servidor.</param>
        public RegistrationResultEventArgs(OperationResultData result)
        {
            Result = result;
        }

        /// <summary>Resultado enviado por el servidor.</summary>
        public OperationResultData Result { get; }
    }
}