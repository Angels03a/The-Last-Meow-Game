using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Public segment of the authorized route.</summary>
    [DataContract]
    public class KittenSegmentView
    {
        [DataMember] public string Kind { get; set; }
        [DataMember] public string Origin { get; set; }
        [DataMember] public string Destination { get; set; }
        [DataMember] public int StartMilliseconds { get; set; }
        [DataMember] public int EndMilliseconds { get; set; }
        [DataMember] public bool IsCatchable { get; set; }
    }
}
