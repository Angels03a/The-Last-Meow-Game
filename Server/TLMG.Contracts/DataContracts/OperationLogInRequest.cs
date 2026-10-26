using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Credenciales que el jugador captura en la pantalla de inicio de sesión.
    /// </summary>
    [DataContract]
    public class OperationLogInRequest
    {
        /// <summary>Correo de la cuenta.</summary>
        [DataMember]
        public string Email { get; set; }

        /// <summary>Contraseña capturada por el jugador.</summary>
        [DataMember]
        public string Password { get; set; }
    }
}