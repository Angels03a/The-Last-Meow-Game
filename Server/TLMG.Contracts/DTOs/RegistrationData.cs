using System.Runtime.Serialization;

namespace TLMG.Contracts.Dtos
{
    /// <summary>
    /// Datos que el jugador captura en la pantalla de crear cuenta.
    /// </summary>
    [DataContract]
    public class RegistrationData
    {
        /// <summary>Nombre visible del jugador.</summary>
        [DataMember]
        public string Nickname { get; set; }

        /// <summary>Correo electrónico de la cuenta.</summary>
        [DataMember]
        public string Email { get; set; }

        /// <summary>Contraseña en claro; el servidor la hashea y nunca la registra.</summary>
        [DataMember]
        public string Password { get; set; }

        /// <summary>Indica si el jugador aceptó los términos de servicio.</summary>
        [DataMember]
        public bool HasAcceptedTerms { get; set; }
    }
}