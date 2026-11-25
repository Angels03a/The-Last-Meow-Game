using System;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Lets GameplayViewModel follow the match authorized by the room and request card placement.
    /// ServerConnectionManager implements this interface. Events run on the UI thread and carry the server's ErrorKey
    /// without translated text. Requests return results through events. The server decides all game actions.
    /// </summary>
    public interface IMatchConnection
    {
        event EventHandler RecoveryStateChanged;
        bool IsMatchRecoveryPending { get; }
        bool IsMatchRecoveryExhausted { get; }
        void RetryMatchRecovery();

        /// <summary>Raised for each new private view. Views with the same or an older version are ignored.</summary>
        event EventHandler<ServerEventArgs<MatchView>> MatchViewChanged;

        /// <summary>Raised with the ErrorKey when the server rejects a subscription or card placement.</summary>
        event EventHandler<ServerEventArgs<string>> MatchErrorReceived;

        /// <summary>Player's catch attempt result.</summary>
        event EventHandler<ServerEventArgs<OperationResult>> FugitiveKittenCatchResultReceived;

        /// <summary>Player's deadline check result.</summary>
        event EventHandler<ServerEventArgs<OperationResult>> FugitiveKittenDeadlineResultReceived;

        /// <summary>Raised when match availability changes.</summary>
        event EventHandler MatchAvailabilityChanged;

        /// <summary>Whether the server accepted the current match subscription.</summary>
        bool IsMatchSubscribed { get; }

        bool CanSendGameplayAction { get; }

        /// <summary>Latest accepted view, or null before the first view arrives.</summary>
        MatchView CurrentMatchView { get; }

        /// <summary>Time since the current view arrived.</summary>
        TimeSpan CurrentMatchViewAge { get; }

        /// <summary>Requests a subscription to the room's match. Events deliver the result and view.</summary>
        /// <returns>
        /// True if sent. False without a registered session or an authorized match, or if the send fails.
        /// </returns>
        bool SubscribeToCurrentMatch();

        /// <summary>Sends an attempt to catch the kitten.</summary>
        bool TryCatchFugitiveKitten();

        /// <summary>Asks the server to check the kitten deadline.</summary>
        bool CheckFugitiveKittenDeadline();

        /// <summary>Requests to place a card from the hand. The server decides and responds through an event.</summary>
        /// <param name="cardKind">Type of card to place.</param>
        /// <returns>True if sent. False without an active subscription or if the send fails.</returns>
        bool PlaceCard(MatchCardKind cardKind);

        /// <summary>Sends a bid for the server to check.</summary>
        bool PlaceBet(int betValue);

        /// <summary>Requests to fold in the current bidding round.</summary>
        bool WithdrawFromBet();

        /// <summary>Requests to reveal the top card of a pile.</summary>
        bool RevealCard(int pileOwnerUserId);

        /// <summary>Sends the position chosen for the punishment.</summary>
        bool ChoosePunishmentCard(int cardIndex);
    }
}
