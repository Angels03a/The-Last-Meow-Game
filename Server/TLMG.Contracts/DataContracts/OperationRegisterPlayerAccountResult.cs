using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts {

    /// <summary>
    /// Resultado genérico de una operación del servidor.
    /// </summary>
    [DataContract]
    public class OperationRegisterPlayerAccountResult
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorKey { get; set; }
    }
}
