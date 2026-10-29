using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Resultado de aceptar una invitación: el id de la sala a la que llamar EnterLobby o la clave de error.
    /// </summary>
    [DataContract]
    public class OperationAcceptRoomInvitationResult
    {
        /// <summary>Indica si la invitación se aceptó y el jugador quedó autorizado a entrar a la sala.</summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>Clave del texto de error; null si la invitación se aceptó.</summary>
        [DataMember]
        public string ErrorKey { get; set; }

        /// <summary>Identificador de la sala a la que el jugador ya puede entrar; 0 si la operación falló.</summary>
        [DataMember]
        public int RoomId { get; set; }
    }
}