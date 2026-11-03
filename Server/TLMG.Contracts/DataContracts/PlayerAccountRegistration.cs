using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Datos que el jugador captura en la pantalla de crear cuenta.
    /// </summary>
    [DataContract]
    public class PlayerAccountRegistration
    {
        /// <summary>Nickname que el jugador quiere usar.</summary>
        [DataMember]
        public string Nickname { get; set; }

        /// <summary>Correo de la cuenta.</summary>
        [DataMember]
        public string Email { get; set; }

        /// <summary>
        /// Contraseña tal como la escribió el jugador; el servidor la hashea y nunca la guarda ni la registra.
        /// </summary>
        [DataMember]
        public string Password { get; set; }

        /// <summary>Indica si el jugador aceptó los términos y condiciones.</summary>
        [DataMember]
        public bool HasAcceptedTerms { get; set; }
    }
}