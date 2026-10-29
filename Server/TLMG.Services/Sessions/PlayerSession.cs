namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Identidad de un jugador con la sesión iniciada; es inmutable y no incluye el token.
    /// </summary>
    public sealed class PlayerSession
    {
        /// <summary>Crea la identidad de una sesión.</summary>
        /// <param name="userId">Identificador de la cuenta (el mismo de Friendship).</param>
        /// <param name="nickname">Nickname de la cuenta.</param>
        public PlayerSession(int userId, string nickname)
        {
            UserId = userId;
            Nickname = nickname;
        }

        /// <summary>Obtiene el identificador de la cuenta.</summary>
        public int UserId { get; }

        /// <summary>Obtiene el nickname de la cuenta.</summary>
        public string Nickname { get; }
    }
}