using TLMG.Client.Models;

namespace TLMG.Client.Navigation
{
    /// <summary>
    /// Parámetro de navegación hacia la pantalla de moderación.
    /// </summary>
    public class ModerationNavigationParameter
    {
        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ModerationNavigationParameter"/>.
        /// </summary>
        /// <param name="playerName">Jugador sobre el que se actúa.</param>
        /// <param name="moderationMode">Reporte o bloqueo.</param>
        public ModerationNavigationParameter(string playerName, ModerationMode moderationMode)
        {
            PlayerName = playerName;
            TypeOfModerationMode = moderationMode;
        }

        /// <summary>
        /// Jugador sobre el que se actúa.
        /// </summary>
        public string PlayerName { get; }

        /// <summary>
        /// Reporte o bloqueo.
        /// </summary>
        public ModerationMode TypeOfModerationMode { get; }
    }
}