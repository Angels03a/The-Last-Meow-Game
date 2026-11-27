using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>
    /// Exposes moderation operations without granting clients authority over another account's blocks.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IModerationManagerCallback))]
    public interface IModerationManager
    {
        /// <summary>Creates a block owned by the authenticated registered player.</summary>
        [OperationContract(IsOneWay = true)]
        void BlockPlayer(string sessionToken, int targetUserId);

        /// <summary>Removes only the authenticated player's block against the target account.</summary>
        [OperationContract(IsOneWay = true)]
        void UnblockPlayer(string sessionToken, int targetUserId);

        /// <summary>Retrieves the authenticated player's outgoing blocks without revealing incoming blocks.</summary>
        [OperationContract(IsOneWay = true)]
        void GetBlockedPlayers(string sessionToken);

        /// <summary>Registers a report only if the authenticated player is eligible in the shared room.</summary>
        [OperationContract(IsOneWay = true)]
        void ReportPlayer(string sessionToken, ReportSubmission submission);

        /// <summary>Records a vote against another eligible participant of the identified room.</summary>
        [OperationContract(IsOneWay = true)]
        void VoteToKick(string sessionToken, KickVote vote);
    }
}
