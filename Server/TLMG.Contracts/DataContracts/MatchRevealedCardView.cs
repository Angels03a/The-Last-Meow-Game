using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Revealed challenge card visible to all players.</summary>
    [DataContract]
    public class MatchRevealedCardView
    {
        /// <summary>Account of the player whose pile supplied the card.</summary>
        [DataMember]
        public int PileOwnerUserId { get; set; }

        /// <summary>Revealed card type.</summary>
        [DataMember]
        public MatchCardKind Card { get; set; }
    }
}
