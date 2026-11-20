using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>How the punishment for a failed challenge was resolved.</summary>
    [DataContract]
    public enum MatchPunishmentResolution
    {
        /// <summary>The challenge did not fail, so there is no punishment.</summary>
        [EnumMember]
        None,

        /// <summary>Punishment is pending while the designated player chooses.</summary>
        [EnumMember]
        Pending,

        /// <summary>The designated player chose the card to remove.</summary>
        [EnumMember]
        ChosenByChooser,

        /// <summary>The system chose at random because time ran out or the player making the choice left.</summary>
        [EnumMember]
        ChosenAtRandom,

        /// <summary>The punished player had one life and was eliminated without a choice.</summary>
        [EnumMember]
        SingleLifeNoChoice,

        /// <summary>The punishment was canceled because the punished player left the match.</summary>
        [EnumMember]
        Cancelled
    }
}
