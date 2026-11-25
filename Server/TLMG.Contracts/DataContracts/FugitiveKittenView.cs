using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Public state of the pre-match minigame.</summary>
    [DataContract]
    public class FugitiveKittenView
    {
        /// <summary>Server time in UTC when this view was built.</summary>
        [DataMember] public DateTime ServerTimeUtc { get; set; }
        [DataMember] public DateTime StartedAtUtc { get; set; }
        [DataMember] public DateTime? PausedAtUtc { get; set; }
        [DataMember] public DateTime ExpiresAtUtc { get; set; }
        [DataMember] public string Color { get; set; }
        [DataMember] public FugitiveKittenResult Result { get; set; }
        [DataMember] public int? WinnerUserId { get; set; }
        [DataMember] public string WinnerNickname { get; set; }
        [DataMember] public List<KittenSegmentView> Segments { get; set; }
    }
}
