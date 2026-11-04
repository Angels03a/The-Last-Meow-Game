using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Petición para validar el vencimiento de una cuenta del servidor.</summary>
    [DataContract]
    public class OperationRequestMatchStartRequest
    {
        /// <summary>Sala a la que pertenece la petición.</summary>
        [DataMember]
        public int RoomId { get; set; }

        /// <summary>Generación de la cuenta observada por el cliente.</summary>
        [DataMember]
        public long CountdownId { get; set; }
    }
}
