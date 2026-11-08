using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Resultado de suscribirse a los amigos: la lista de amigos o la clave de error para que el cliente la traduzca.
    /// </summary>
    [DataContract]
    public class OperationSubscribeToFriendsResult : OperationResult
    {
        /// <summary>Amigos aceptados ordenados por nickname, con su presencia; null si la operación falló.</summary>
        [DataMember]
        public List<Friend> Friends { get; set; }
    }
}