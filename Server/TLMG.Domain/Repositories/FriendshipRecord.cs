namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Fila de Friendship con los nicknames de quien envió y de quien recibe la solicitud.
    /// </summary>
    public class FriendshipRecord
    {
        /// <summary>Id de la fila; es el id de la solicitud que viaja al cliente.</summary>
        public int FriendshipId { get; set; }

        /// <summary>Id de la cuenta que envió la solicitud.</summary>
        public int SenderUserId { get; set; }

        /// <summary>Id de la cuenta que la recibió.</summary>
        public int ReceiverUserId { get; set; }

        /// <summary>Nickname de quien envió la solicitud.</summary>
        public string SenderNickname { get; set; }

        /// <summary>Nickname de quien la recibió.</summary>
        public string ReceiverNickname { get; set; }

        /// <summary>Estado actual de la fila.</summary>
        public FriendshipState State { get; set; }
    }
}
