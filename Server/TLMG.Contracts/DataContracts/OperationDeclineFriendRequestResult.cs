using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Resultado de rechazar una solicitud de amistad recibida.</summary>
    [DataContract]
    public class OperationDeclineFriendRequestResult : OperationResult
    {
        /// <summary>Identificador de la solicitud rechazada, para quitarla de las pendientes; 0 si falló.</summary>
        [DataMember]
        public int FriendRequestId { get; set; }
    }
}
