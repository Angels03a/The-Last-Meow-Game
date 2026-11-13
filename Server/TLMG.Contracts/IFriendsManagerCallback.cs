using System.ServiceModel;

namespace TLMG.Contracts
{
    /// <summary>
    /// Callback of the friends channel: it joins the friendship and invitation callbacks so a single client
    /// implementation receives everything.
    /// </summary>
    [ServiceContract]
    public interface IFriendsManagerCallback : IFriendshipManagerCallback, IRoomInvitationManagerCallback
    {
    }
}