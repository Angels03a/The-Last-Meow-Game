namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Estado de una fila de Friendship, sin las cadenas FRIEND_* de la base de datos.
    /// </summary>
    public enum FriendshipState
    {
        /// <summary>La solicitud espera la respuesta del receptor (FRIEND_PENDING).</summary>
        Pending,

        /// <summary>Los dos jugadores son amigos (FRIEND_ACCEPTED).</summary>
        Accepted,

        /// <summary>El receptor rechazó la solicitud (FRIEND_REJECTED).</summary>
        Rejected
    }
}
