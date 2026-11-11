using TLMG.Contracts;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Resultado de sumar una amistad nueva a los canales de amigos de los dos jugadores: quién está en línea y sus
    /// callbacks, para avisar después de liberar el candado.
    /// </summary>
    public sealed class FriendsAddition
    {
        /// <summary>Crea el resultado con la presencia y el callback de cada jugador.</summary>
        /// <param name="isFirstOnline">Indica si el primer jugador tiene canal de amigos.</param>
        /// <param name="firstCallback">Callback del primer jugador; null si está desconectado.</param>
        /// <param name="isSecondOnline">Indica si el segundo jugador tiene canal de amigos.</param>
        /// <param name="secondCallback">Callback del segundo jugador; null si está desconectado.</param>
        public FriendsAddition(
            bool isFirstOnline,
            IFriendsManagerCallback firstCallback,
            bool isSecondOnline,
            IFriendsManagerCallback secondCallback)
        {
            IsFirstOnline = isFirstOnline;
            FirstCallback = firstCallback;
            IsSecondOnline = isSecondOnline;
            SecondCallback = secondCallback;
        }

        /// <summary>Indica si el primer jugador está en línea.</summary>
        public bool IsFirstOnline { get; }

        /// <summary>Callback del primer jugador; null si está desconectado.</summary>
        public IFriendsManagerCallback FirstCallback { get; }

        /// <summary>Indica si el segundo jugador está en línea.</summary>
        public bool IsSecondOnline { get; }

        /// <summary>Callback del segundo jugador; null si está desconectado.</summary>
        public IFriendsManagerCallback SecondCallback { get; }
    }
}
