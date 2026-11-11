using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Resultado de solicitar amistad por nickname. En un envío exitoso exactamente uno de SentFriendRequest y
    /// NewFriend viene lleno.
    /// </summary>
    [DataContract]
    public class OperationSendFriendRequestResult : OperationResult
    {
        /// <summary>Solicitud pendiente creada o reactivada; null si se aceptó sola o si la petición falló.</summary>
        [DataMember]
        public FriendRequest SentFriendRequest { get; set; }

        /// <summary>Amigo nuevo si la otra persona ya había solicitado (solicitud cruzada); null si no.</summary>
        [DataMember]
        public Friend NewFriend { get; set; }
    }
}
