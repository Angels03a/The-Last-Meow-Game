using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Datos de un amigo aceptado y de si está en línea.
    /// </summary>
    [DataContract]
    public class Friend
    {
        /// <summary>Identificador de la cuenta del amigo (id de User); sirve para invitarlo a una sala.</summary>
        [DataMember]
        public int UserId { get; set; }

        /// <summary>Nickname del amigo.</summary>
        [DataMember]
        public string Nickname { get; set; }

        /// <summary>Indica si el amigo está en línea, es decir, si tiene un canal de amigos abierto.</summary>
        [DataMember]
        public bool IsOnline { get; set; }
    }
}