using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Resultado de suscribirse a los amigos: la lista de amigos o la clave de error para que el cliente la traduzca.
    /// </summary>
    [DataContract]
    public class OperationSubscribeToFriendsResult
    {
        /// <summary>Indica si el canal quedó registrado como el canal de amigos de la sesión.</summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>Clave del texto de error; null si la suscripción se hizo.</summary>
        [DataMember]
        public string ErrorKey { get; set; }

        /// <summary>Amigos aceptados ordenados por nickname, con su presencia; null si la operación falló.</summary>
        [DataMember]
        public List<Friend> Friends { get; set; }
    }
}