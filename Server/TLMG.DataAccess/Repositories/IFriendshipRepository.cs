namespace TLMG.DataAccess.Repositories
{
    /// <summary>
    /// Acceso a la persistencia de las amistades entre cuentas; considera ambos sentidos de Friendship.
    /// </summary>
    public interface IFriendshipRepository
    {
        /// <summary>Obtiene los amigos aceptados de una cuenta, sin repetir al de fila en ambos sentidos.</summary>
        /// <param name="userId">Identificador de la cuenta (id de User).</param>
        /// <returns>Los amigos y su estado de consulta; nunca es null.</returns>
        FriendListLookup FindAcceptedFriends(int userId);

        /// <summary>Comprueba si dos cuentas son amigas aceptadas, sin importar quién envió la solicitud.</summary>
        /// <param name="userId">Identificador de la primera cuenta (id de User).</param>
        /// <param name="otherUserId">Identificador de la segunda cuenta (id de User).</param>
        /// <returns>
        /// Accepted si son amigas, NotAccepted si no lo son y DataFailure si la consulta falló; un fallo nunca
        /// debe tratarse como "no son amigos" ni como "son amigos".
        /// </returns>
        FriendshipLookupStatus FindFriendshipStatus(int userId, int otherUserId);
    }
}