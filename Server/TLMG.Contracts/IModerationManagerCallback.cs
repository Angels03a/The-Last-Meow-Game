using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>
    /// Confirms moderation operations so the client never assumes that a block was persisted.
    /// </summary>
    [ServiceContract]
    public interface IModerationManagerCallback
    {
        /// <summary>Confirms whether the requested block is effective.</summary>
        [OperationContract(IsOneWay = true)]
        void BlockPlayerCompleted(OperationResult operation);

        /// <summary>Confirms whether the requested outgoing block was removed.</summary>
        [OperationContract(IsOneWay = true)]
        void UnblockPlayerCompleted(OperationResult operation);

        /// <summary>Delivers the outgoing block list or a controlled failure.</summary>
        [OperationContract(IsOneWay = true)]
        void BlockedPlayersReceived(BlockedPlayerList blockedPlayers);

        /// <summary>Confirms persistence of the report without disclosing the target account's sanction history.</summary>
        [OperationContract(IsOneWay = true)]
        void ReportPlayerCompleted(OperationResult operation);

        /// <summary>Confirms whether the vote was accepted; acceptance alone does not imply expulsion.</summary>
        [OperationContract(IsOneWay = true)]
        void KickVoteCompleted(OperationResult operation);
    }
}
