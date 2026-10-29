using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Resultado de invitar a un amigo a la sala: éxito o clave de error para que el cliente la traduzca.
    /// </summary>
    [DataContract]
    public class OperationInviteFriendToRoomResult
    {
        /// <summary>Indica si la invitación quedó hecha.</summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>Clave del texto de error; null si la invitación quedó hecha.</summary>
        [DataMember]
        public string ErrorKey { get; set; }
    }
}