using System;

namespace TLMG.Models
{
    /// <summary>
    /// Representa los datos de un jugador para mostrarse en el leaderboard.
    /// </summary>
    public class PlayerRankEntry
    {
        private const double WinRateBarMaxWidthPixels = 120.0;
        private const int HighWinRateThresholdPercentage = 80;
        private const double PercentageDivisor = 100.0;

        /// <summary>
        /// Obtiene o establece la posición en el ranking general.
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// Obtiene o establece la ruta de la imagen de avatar del jugador.
        /// </summary>
        public string AvatarImagePath { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece el nombre o nickname del jugador.
        /// </summary>
        public string PlayerName { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece el puntaje total acumulado por el jugador.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Obtiene o establece la cantidad total de victorias obtenidas.
        /// </summary>
        public int Wins { get; set; }

        /// <summary>
        /// Obtiene o establece el porcentaje de victorias del jugador.
        /// </summary>
        public int WinRatePercentage { get; set; }

        /// <summary>
        /// Obtiene la etiqueta formateada del rango del jugador (por ejemplo, "#1").
        /// </summary>
        public string RankLabel => $"#{Rank}";

        /// <summary>
        /// Obtiene la etiqueta formateada del puntaje con separador de miles.
        /// </summary>
        public string ScoreLabel => Score.ToString("N0");

        /// <summary>
        /// Obtiene la etiqueta formateada del porcentaje de victorias (por ejemplo, "80%").
        /// </summary>
        public string WinRateLabel => $"{WinRatePercentage}%";

        /// <summary>
        /// Obtiene el ancho en píxeles para la barra visual, proporcional al porcentaje
        /// de victorias del jugador.
        /// </summary>
        public double WinRateBarWidth => WinRatePercentage / PercentageDivisor * WinRateBarMaxWidthPixels;

        /// <summary>
        /// Obtiene un valor que indica si el porcentaje de victoria supera el umbral destacado.
        /// </summary>
        public bool IsHighWinRate => WinRatePercentage >= HighWinRateThresholdPercentage;
    }
}