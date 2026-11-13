using System.ServiceModel;

namespace TLMG.Contracts
{
    /// <summary>
    /// Single friends channel of a session: groups friendship and room invitation operations so the player stays
    /// online through one channel while each part keeps a small interface.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IFriendsManagerCallback))]
    public interface IFriendsManager : IFriendshipManager, IRoomInvitationManager
    {
    }
}