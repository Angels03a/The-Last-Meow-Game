using System;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Lets screens change cards per player and display the countdown without accessing LobbyCountdown directly.
    /// </summary>
    internal sealed partial class ServerConnectionManager : ILobbyConnection
    {
        /// <inheritdoc />
        public event EventHandler CountdownChanged
        {
            add { Countdown.Changed += value; }
            remove { Countdown.Changed -= value; }
        }

        /// <inheritdoc />
        public bool IsCountdownVisible => Countdown.IsVisible;

        /// <inheritdoc />
        public int CountdownSecondsRemaining => Countdown.SecondsRemaining;

        /// <inheritdoc />
        public bool ChangeCardsPerPlayer(int cardsPerPlayerCount)
        {
            bool wasChangeCardsPerPlayerSent = false;
            bool hasLobbyChannel = CurrentRoom != null && _lobbyClient != null;

            if (hasLobbyChannel)
            {
                OperationChangeCardsPerPlayerRequest changeCardsPerPlayerRequest =
                    new OperationChangeCardsPerPlayerRequest
                    {
                        RoomId = CurrentRoom.RoomId,
                        CardsPerPlayerCount = cardsPerPlayerCount
                    };
                wasChangeCardsPerPlayerSent = TrySend(
                    () => _lobbyClient.ChangeCardsPerPlayer(_sessionToken, changeCardsPerPlayerRequest),
                    "cambiar las cartas por jugador", CloseLobbyChannel);
            }

            return wasChangeCardsPerPlayerSent;
        }
    }
}
