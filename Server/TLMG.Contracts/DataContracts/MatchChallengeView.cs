using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Latest challenge outcome, visible to all players. It stays available after the challenge until the next opening
    /// bid or card placement. Otherwise null.
    /// </summary>
    [DataContract]
    public class MatchChallengeView
    {
        /// <summary>Account of the bidder taking the challenge.</summary>
        [DataMember]
        public int BettorUserId { get; set; }

        /// <summary>Number of cards the bidder promised to reveal.</summary>
        [DataMember]
        public int PromisedCount { get; set; }

        /// <summary>Challenge status or outcome.</summary>
        [DataMember]
        public MatchChallengeOutcome Outcome { get; set; }

        /// <summary>Cards in reveal order. The last card stopped the reveal.</summary>
        [DataMember]
        public List<MatchRevealedCardView> RevealedCards { get; set; }

        /// <summary>Punished player's account, or 0 unless the challenge failed.</summary>
        [DataMember]
        public int PunishedUserId { get; set; }

        /// <summary>
        /// Account of the player who chose or must choose the lost card, or 0 unless the challenge failed.
        /// </summary>
        [DataMember]
        public int PunishmentChooserUserId { get; set; }

        /// <summary>Punishment status for the challenge.</summary>
        [DataMember]
        public MatchPunishmentResolution PunishmentResolution { get; set; }
    }
}
