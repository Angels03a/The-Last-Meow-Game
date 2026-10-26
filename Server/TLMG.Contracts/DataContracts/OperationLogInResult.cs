using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Resultado del inicio de sesión: éxito o clave de error para que el cliente la traduzca.
    /// </summary>
    [DataContract]
    public class OperationLogInResult
    {
        /// <summary>Indica si el inicio de sesión fue aceptado.</summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>Clave del texto de error; null si el inicio de sesión fue aceptado.</summary>
        [DataMember]
        public string ErrorKey { get; set; }
    }
}