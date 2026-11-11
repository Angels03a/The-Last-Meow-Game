using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Resultado de aceptar una solicitud de amistad recibida.</summary>
    [DataContract]
    public class OperationAcceptFriendRequestResult : OperationResult
    {
        /// <summary>Identificador de la solicitud aceptada, para quitarla de las pendientes; 0 si falló.</summary>
        [DataMember]
        public int FriendRequestId { get; set; }

        /// <summary>Amigo nuevo con su presencia actual; null si la petición falló.</summary>
        [DataMember]
        public Friend NewFriend { get; set; }
    }
}
