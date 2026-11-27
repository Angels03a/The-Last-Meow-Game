using System;
using System.ServiceModel;

namespace TLMG.Client.Communication
{
    internal readonly struct HeartbeatLink
    {
        internal HeartbeatLink(ServerCallback callback, IClientChannel channel, int offsetMilliseconds,
            Action<string, int, int> send, Action close)
        {
            Callback = callback;
            Channel = channel;
            OffsetMilliseconds = offsetMilliseconds;
            Send = send;
            Close = close;
        }

        internal ServerCallback Callback { get; }
        internal IClientChannel Channel { get; }
        internal int OffsetMilliseconds { get; }
        internal Action<string, int, int> Send { get; }
        internal Action Close { get; }
    }
}