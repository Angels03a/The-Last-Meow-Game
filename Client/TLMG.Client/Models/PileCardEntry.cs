namespace TLMG.Client.Models
{
    /// <summary>Carta de una pila tal como se dibuja en miniatura: solo el símbolo que se muestra.</summary>
    public sealed class PileCardEntry
    {
        /// <summary>Símbolo de la carta; una pila ajena solo muestra cartas boca abajo.</summary>
        public string Symbol { get; set; }
    }
}
