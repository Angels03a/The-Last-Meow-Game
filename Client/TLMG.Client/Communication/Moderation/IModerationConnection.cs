using System;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>Exposes moderation operations and their individual server confirmations.</summary>
    public interface IModerationConnection
    {
        /// <summary>Raised when the server confirms a block operation.</summary>
        event EventHandler<ServerEventArgs<OperationResult>> BlockPlayerResultReceived;

        /// <summary>Raised when the server confirms an unblock operation.</summary>
        event EventHandler<ServerEventArgs<OperationResult>> UnblockPlayerResultReceived;

        /// <summary>Raised when the server answers the blocked-player lookup.</summary>
        event EventHandler<ServerEventArgs<BlockedPlayerList>> BlockedPlayersReceived;

        /// <summary>Raised when the server answers a report submission.</summary>
        event EventHandler<ServerEventArgs<OperationResult>> ReportPlayerResultReceived;

        /// <summary>Raised when the server answers a vote, without implying expulsion.</summary>
        event EventHandler<ServerEventArgs<OperationResult>> KickVoteResultReceived;

        /// <summary>Raised when unanimous voting removes the local player from a room.</summary>
        event EventHandler<ServerEventArgs<int>> PlayerExpelled;

        bool BlockPlayer(int targetUserId);
        bool UnblockPlayer(int targetUserId);
        bool GetBlockedPlayers();
        bool ReportPlayer(ReportSubmission submission);
        bool VoteToKick(KickVote vote);
    }
}