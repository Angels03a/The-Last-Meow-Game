namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Ámbito de un mensaje de chat guardado, sin las cadenas de la base de datos.
    /// </summary>
    public enum ChatMessageScope
    {
        /// <summary>Mensaje escrito en la sala de espera (LOBBY).</summary>
        Lobby,

        /// <summary>Mensaje escrito durante la partida (MATCH).</summary>
        Match
    }
}