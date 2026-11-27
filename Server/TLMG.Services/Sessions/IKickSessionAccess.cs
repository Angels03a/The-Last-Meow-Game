using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Sessions
{
    /// <summary>Applies votes to the authoritative room and returns notifications for delivery outside the lock.</summary>
    public interface IKickSessionAccess
    {
        /// <summary>Records an eligible vote and removes the target only after unanimous approval.</summary>
        KickVoteChange RegisterKickVote(string sessionToken, KickVote vote);
    }
}
