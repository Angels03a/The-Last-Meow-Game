using System;

namespace TLMG.Services.Sessions
{
    internal sealed class LinkHealth
    {
        internal LinkHealth(string channelId, DateTime nowUtc)
        {
            ChannelId = channelId;
            EvidenceUtc = nowUtc;
        }

        internal string ChannelId { get; }
        internal DateTime EvidenceUtc { get; set; }
        internal DateTime? LastPulseUtc { get; set; }
        internal int Sequence { get; set; }
    }
}
