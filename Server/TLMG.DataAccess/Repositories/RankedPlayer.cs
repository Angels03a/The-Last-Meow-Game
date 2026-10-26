namespace TLMG.DataAccess.Repositories
{
    /// <summary>
    /// Datos mínimos de un jugador para la tabla de posiciones; no expone la entidad de Entity Framework.
    /// </summary>
    public class RankedPlayer
    {
        /// <summary>Nickname del jugador.</summary>
        public string Nickname { get; set; }

        /// <summary>Imagen de perfil del jugador; puede ser null.</summary>
        public string ProfileImage { get; set; }

        /// <summary>Puntos totales acumulados.</summary>
        public int TotalPoints { get; set; }

        /// <summary>Partidas ganadas.</summary>
        public int TotalWins { get; set; }

        /// <summary>Partidas jugadas.</summary>
        public int TotalGamesPlayed { get; set; }
    }
}