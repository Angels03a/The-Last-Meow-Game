using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Petición del estado Listo deseado del jugador.</summary>
    [DataContract]
    public class OperationChangePlayerReadinessRequest
    {
        /// <summary>Sala a la que pertenece la petición.</summary>
        [DataMember]
        public int RoomId { get; set; }

        /// <summary>Estado Listo deseado; repetir el mismo valor no renueva la cuenta.</summary>
        [DataMember]
        public bool IsReady { get; set; }
    }
}
