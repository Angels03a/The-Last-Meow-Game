using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Resultado del inicio de sesión: éxito o clave de error para que el cliente la traduzca. Si fue aceptado, trae
    /// también la identidad del jugador para mostrarla; el servidor nunca la toma del cliente, sino del token.
    /// </summary>
    [DataContract]
    public class OperationLogInResult : OperationResult
    {
        /// <summary>Token de la sesión del jugador; null si el inicio de sesión falló.</summary>
        [DataMember]
        public string SessionToken { get; set; }

        /// <summary>
        /// Identificador de la cuenta del jugador; 0 si el inicio de sesión falló. Es solo informativo: ninguna
        /// operación del servidor lo acepta como prueba de identidad.
        /// </summary>
        [DataMember]
        public int UserId { get; set; }

        /// <summary>Nickname del jugador; null si el inicio de sesión falló.</summary>
        [DataMember]
        public string Nickname { get; set; }

        /// <summary>Indicates whether the session is a guest session, so the client can warn that nothing is saved.</summary>
        [DataMember]
        public bool IsGuest { get; set; }
    }
}