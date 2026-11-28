namespace TLMG.Services.Sessions
{
    public sealed class GuestFriendshipChange
    {
        private GuestFriendshipChange(GuestFriendshipChangeStatus status, GuestFriendship friendship)
        {
            Status = status;
            Friendship = friendship;
        }

        public GuestFriendshipChangeStatus Status { get; }

        public GuestFriendship Friendship { get; }

        internal static GuestFriendshipChange CreateApplied(GuestFriendship friendship)
        {
            return new GuestFriendshipChange(GuestFriendshipChangeStatus.Applied, friendship);
        }

        internal static GuestFriendshipChange CreateRejected(
            GuestFriendshipChangeStatus status,
            GuestFriendship friendship)
        {
            return new GuestFriendshipChange(status, friendship);
        }
    }
}