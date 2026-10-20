using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Datos que el jugador captura en la pantalla de crear cuenta.
    /// </summary>
    [DataContract]
    public class PlayerAccountRegistration
    {
        [DataMember]
        public string Nickname { get; set; }

        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public string Password { get; set; }

        [DataMember]
        public bool HasAcceptedTerms { get; set; }
    }
}