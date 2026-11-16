namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Castigo pendiente que deja un reto fallido: quién elige la carta que se pierde y a quién se le quita. Mientras
    /// exista, el motor rechaza toda acción de juego; resolverlo es de la etapa siguiente.
    /// </summary>
    public sealed class PendingPunishment
    {
        /// <summary>Crea el castigo pendiente de un reto fallido.</summary>
        /// <param name="chooserPlayerId">Jugador que elige la carta: el dueño de la pila del Gato.</param>
        /// <param name="punishedPlayerId">Jugador al que se le quita la carta: el apostador.</param>
        /// <param name="isBlindChoice">true si quien elige no ve las cartas del castigado.</param>
        internal PendingPunishment(int chooserPlayerId, int punishedPlayerId, bool isBlindChoice)
        {
            ChooserPlayerId = chooserPlayerId;
            PunishedPlayerId = punishedPlayerId;
            IsBlindChoice = isBlindChoice;
        }

        /// <summary>Obtiene el jugador que elige la carta que se pierde.</summary>
        public int ChooserPlayerId { get; }

        /// <summary>Obtiene el jugador castigado, que es el apostador del reto fallido.</summary>
        public int PunishedPlayerId { get; }

        /// <summary>Obtiene si quien elige lo hace a ciegas; false si el Gato era del propio apostador.</summary>
        public bool IsBlindChoice { get; }
    }
}
