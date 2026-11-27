using TLMG.Services.Sessions;

namespace TLMG.Services.Lobby
{
    /// <summary>Coordinates durable match starts without adding persistence dependencies to WCF contracts.</summary>
    public interface IMatchStartCoordinator
    {
        /// <summary>Publishes a start only after its identity is committed.</summary>
        LobbyMembershipChange RequestMatchStart(SessionChannelCaller caller, int roomId, long countdownId);
        /// <summary>Retries cleanup of registrations whose reservations were invalidated.</summary>
        void ReconcileCancelledStarts();
    }
}
