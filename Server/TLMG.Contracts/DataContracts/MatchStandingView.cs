using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Participant's public result.</summary>
    [DataContract]
    public class MatchStandingView
    {
        /// <summary>Participant's public identity.</summary>
        [DataMember]
        public int UserId { get; set; }

        /// <summary>Display name recorded at match start.</summary>
        [DataMember]
        public string Nickname { get; set; }

        /// <summary>Final rank calculated by the engine.</summary>
        [DataMember]
        public int Position { get; set; }

        /// <summary>Points calculated by the engine.</summary>
        [DataMember]
        public int Points { get; set; }
    }
}
