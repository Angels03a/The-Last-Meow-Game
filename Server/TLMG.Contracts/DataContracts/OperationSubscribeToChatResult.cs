using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Resultado de suscribir el canal al chat de la sala.</summary>
    [DataContract]
    public class OperationSubscribeToChatResult
    {
        /// <summary>Indica si el canal quedó suscrito.</summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>Clave de error; null en caso de éxito.</summary>
        [DataMember]
        public string ErrorKey { get; set; }
    }
}
