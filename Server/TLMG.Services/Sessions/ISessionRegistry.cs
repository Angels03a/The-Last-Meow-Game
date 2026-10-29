namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Registro en memoria de las sesiones iniciadas, compartido entre todas las sesiones de WCF.
    /// </summary>
    public interface ISessionRegistry
    {
        /// <summary>Inicia una sesión y reemplaza la anterior de la misma cuenta, si existe.</summary>
        /// <param name="userId">Identificador de la cuenta.</param>
        /// <param name="nickname">Nickname de la cuenta.</param>
        /// <returns>El token de la sesión nueva; nunca es null.</returns>
        string StartSession(int userId, string nickname);

        /// <summary>Busca la sesión de un token.</summary>
        /// <param name="sessionToken">Token recibido del cliente; puede ser null o inexistente.</param>
        /// <param name="playerSession">La sesión encontrada; null si no existe.</param>
        /// <returns>true si el token pertenece a una sesión vigente; en caso contrario, false.</returns>
        bool TryFindSessionByToken(string sessionToken, out PlayerSession playerSession);

        /// <summary>Cierra la sesión de un token.</summary>
        /// <param name="sessionToken">Token recibido del cliente; puede ser null o inexistente.</param>
        /// <returns>true si había una sesión vigente y se cerró; en caso contrario, false.</returns>
        bool EndSession(string sessionToken);
    }
}