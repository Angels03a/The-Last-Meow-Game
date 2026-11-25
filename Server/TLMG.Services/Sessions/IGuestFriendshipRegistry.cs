using System.Collections.Generic;

namespace TLMG.Services.Sessions
{
    public interface IGuestFriendshipRegistry
    {
        bool TryFindGuestByNickname(string nickname, out int guestUserId, out string guestNickname);

        GuestFriendshipChange CreateGuestFriendRequest(int senderUserId, int guestUserId);

        GuestFriendshipChange AnswerGuestFriendRequest(int requestId, int receiverUserId, bool isAccepted);

        bool AreGuestFriends(int userId, int otherUserId);

        IReadOnlyList<GuestFriendship> FindGuestFriendships(int userId);
    }
}