namespace TLMG.Services.Sessions
{
    public sealed class GuestFriendship
    {
        public int FriendshipId { get; internal set; }

        public int SenderUserId { get; internal set; }

        public int ReceiverUserId { get; internal set; }

        public string SenderNickname { get; internal set; }

        public string ReceiverNickname { get; internal set; }

        public GuestFriendshipState State { get; internal set; }

        internal GuestFriendship CreateAccepted()
        {
            GuestFriendship acceptedFriendship = new GuestFriendship
            {
                FriendshipId = FriendshipId,
                SenderUserId = SenderUserId,
                ReceiverUserId = ReceiverUserId,
                SenderNickname = SenderNickname,
                ReceiverNickname = ReceiverNickname,
                State = GuestFriendshipState.Accepted
            };

            return acceptedFriendship;
        }
    }
}