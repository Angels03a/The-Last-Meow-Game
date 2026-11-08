using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Resultado de aceptar una invitación: el id de la sala a la que llamar EnterLobby o la clave de error.
    /// </summary>
    [DataContract]
    public class OperationAcceptRoomInvitationResult : OperationResult
    {
        /// <summary>Identificador de la sala a la que el jugador ya puede entrar; 0 si la operación falló.</summary>
        [DataMember]
        public int RoomId { get; set; }
    }
}