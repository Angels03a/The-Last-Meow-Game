using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts {

    /// <summary>
    /// Resultado genérico de una operación del servidor.
    /// </summary>
    [DataContract]
    public class OperationRegisterPlayerAccountResult
    {
        /// <summary>Indica si la cuenta quedó registrada.</summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>Clave del texto de error; null si la cuenta quedó registrada.</summary>
        [DataMember]
        public string ErrorKey { get; set; }
    }
}
