namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Invitación pendiente de un jugador a la sala de otro; es inmutable.
    /// </summary>
    internal sealed class RoomInvitationRecord
    {
        /// <summary>Crea una invitación pendiente.</summary>
        /// <param name="invitationId">Identificador de la invitación.</param>
        /// <param name="inviterUserId">Identificador de la cuenta que invita.</param>
        /// <param name="inviteeUserId">Identificador de la cuenta invitada.</param>
        /// <param name="roomId">Identificador de la sala a la que se invita.</param>
        public RoomInvitationRecord(int invitationId, int inviterUserId, int inviteeUserId, int roomId)
        {
            InvitationId = invitationId;
            InviterUserId = inviterUserId;
            InviteeUserId = inviteeUserId;
            RoomId = roomId;
        }

        /// <summary>Obtiene el identificador de la invitación.</summary>
        public int InvitationId { get; }

        /// <summary>Obtiene el identificador de la cuenta que invita.</summary>
        public int InviterUserId { get; }

        /// <summary>Obtiene el identificador de la cuenta invitada.</summary>
        public int InviteeUserId { get; }

        /// <summary>Obtiene el identificador de la sala a la que se invita.</summary>
        public int RoomId { get; }
    }
}