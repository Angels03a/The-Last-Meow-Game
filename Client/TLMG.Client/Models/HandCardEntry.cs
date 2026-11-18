namespace TLMG.Client.Models
{
    /// <summary>Carta de la mano del jugador local tal como la muestra la pantalla de partida.</summary>
    public sealed class HandCardEntry
    {
        /// <summary>Posición enviada al servidor durante el castigo.</summary>
        public int Index { get; set; }

        /// <summary>Indica que la posición oculta su identidad.</summary>
        public bool IsBlind { get; set; }

        /// <summary>Tipo de la carta; decide la imagen, el texto y los colores.</summary>
        public HandCardKind Kind { get; set; }
    }
}