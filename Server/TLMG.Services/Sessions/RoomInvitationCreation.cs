using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Resultado inmutable de invitar a un amigo a la sala: qué pasó y a quién hay que avisar fuera del candado.
    /// </summary>
    public sealed class RoomInvitationCreation
    {
        /// <summary>Valor de InvitationId cuando el resultado no se refiere a ninguna invitación.</summary>
        public const int NoInvitationId = 0;

        private RoomInvitationCreation(
            FriendsChangeStatus status,
            int invitationId,
            string inviterNickname,
            bool isNewInvitation,
            IFriendsManagerCallback inviteeCallback)
        {
            Status = status;
            InvitationId = invitationId;
            InviterNickname = inviterNickname;
            IsNewInvitation = isNewInvitation;
            InviteeCallback = inviteeCallback;
        }

        /// <summary>Obtiene qué ocurrió con el intento de invitar.</summary>
        public FriendsChangeStatus Status { get; }

        /// <summary>Obtiene el id de la invitación, nueva o ya existente; NoInvitationId si se rechazó.</summary>
        public int InvitationId { get; }

        /// <summary>Obtiene el nickname de quien invita; null si se rechazó.</summary>
        public string InviterNickname { get; }

        /// <summary>Obtiene si la invitación se creó ahora; false si ya existía una igual.</summary>
        public bool IsNewInvitation { get; }

        /// <summary>Obtiene el callback del canal de amigos del invitado; null si se rechazó.</summary>
        public IFriendsManagerCallback InviteeCallback { get; }

        /// <summary>Crea un resultado rechazado.</summary>
        /// <param name="status">Motivo del rechazo.</param>
        /// <returns>El resultado sin invitación; nunca es null.</returns>
        public static RoomInvitationCreation CreateRejected(FriendsChangeStatus status)
        {
            RoomInvitationCreation roomInvitationCreation =
                new RoomInvitationCreation(status, NoInvitationId, null, false, null);

            return roomInvitationCreation;
        }

        /// <summary>Crea un resultado aplicado.</summary>
        /// <param name="invitationId">Id de la invitación.</param>
        /// <param name="inviterNickname">Nickname de quien invita.</param>
        /// <param name="isNewInvitation">true si la invitación se creó ahora; false si ya existía.</param>
        /// <param name="inviteeCallback">Callback del canal de amigos del invitado.</param>
        /// <returns>El resultado aplicado; nunca es null.</returns>
        public static RoomInvitationCreation CreateApplied(
            int invitationId,
            string inviterNickname,
            bool isNewInvitation,
            IFriendsManagerCallback inviteeCallback)
        {
            RoomInvitationCreation roomInvitationCreation = new RoomInvitationCreation(
                FriendsChangeStatus.Applied,
                invitationId,
                inviterNickname,
                isNewInvitation,
                inviteeCallback);

            return roomInvitationCreation;
        }
    }
}