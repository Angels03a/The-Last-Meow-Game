using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Public pending punishment showing who chooses and who loses a card. It contains no cards. The punished player
    /// sees their own hand in their private view. A player choosing blindly receives only the number of slots.
    /// </summary>
    [DataContract]
    public class MatchPunishmentView
    {
        /// <summary>Account of the player choosing the lost card.</summary>
        [DataMember]
        public int ChooserUserId { get; set; }

        /// <summary>Account of the player losing the card.</summary>
        [DataMember]
        public int PunishedUserId { get; set; }

        /// <summary>
        /// Whether the choice is blind because another player's Fatal Cat was revealed. Otherwise the punished player
        /// sees their hand when choosing.
        /// </summary>
        [DataMember]
        public bool IsBlindChoice { get; set; }

        /// <summary>Available choices, equal to the punished player's lives.</summary>
        [DataMember]
        public int CardSlotCount { get; set; }
    }
}
