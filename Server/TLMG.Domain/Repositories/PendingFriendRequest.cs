namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Solicitud de amistad pendiente vista desde un jugador: la otra persona y si la recibió o la envió.
    /// </summary>
    public class PendingFriendRequest
    {
        /// <summary>Id de la fila de Friendship.</summary>
        public int FriendshipId { get; set; }

        /// <summary>Id de la cuenta de la otra persona.</summary>
        public int OtherUserId { get; set; }

        /// <summary>Nickname de la otra persona.</summary>
        public string OtherNickname { get; set; }

        /// <summary>true si el jugador la recibió; false si la envió.</summary>
        public bool IsIncoming { get; set; }
    }
}
