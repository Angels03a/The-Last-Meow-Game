namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Groups what the friends service needs from the session registry besides its own state: authorization and the
    /// temporary friendships of guests.
    /// </summary>
    public interface IFriendsSessionAccess : ISessionAuthorizer, IGuestFriendshipRegistry
    {
    }
}