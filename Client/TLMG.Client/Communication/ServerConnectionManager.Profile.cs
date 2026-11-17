using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    internal sealed partial class ServerConnectionManager : IProfileConnection
    {
        private const string ProfileConnectionErrorKey = "Profile_NetworkError";
        private const string ProfileUnexpectedErrorKey = "Profile_DataModelError";
        private const string InvalidSessionErrorKey = "Common_InvalidSessionError";

        public OperationPlayerProfileResult GetPlayerProfile()
        {
            OperationPlayerProfileResult profileResult;

            if (HasRegisteredSession)
            {
                profileResult = SendUsersManagerRequest(
                    usersClient => usersClient.GetPlayerProfile(_sessionToken),
                    "obtener el perfil del jugador",
                    ProfileConnectionErrorKey,
                    ProfileUnexpectedErrorKey);
            }
            else
            {
                profileResult = CreateFailedResult<OperationPlayerProfileResult>(InvalidSessionErrorKey);
            }

            return profileResult;
        }

        public OperationResult UpdatePlayerProfile(string nickname, string avatarKey)
        {
            OperationResult updateResult;

            if (HasRegisteredSession)
            {
                OperationUpdatePlayerProfileRequest updateProfileRequest = new OperationUpdatePlayerProfileRequest
                {
                    SessionToken = _sessionToken,
                    Nickname = nickname,
                    AvatarKey = avatarKey
                };

                updateResult = SendUsersManagerRequest(
                    usersClient => usersClient.UpdatePlayerProfile(updateProfileRequest),
                    "actualizar el perfil del jugador",
                    ProfileConnectionErrorKey,
                    ProfileUnexpectedErrorKey);

                if (updateResult.IsSuccess)
                {
                    PlayerNickname = nickname;
                }
            }
            else
            {
                updateResult = CreateFailedResult<OperationResult>(InvalidSessionErrorKey);
            }

            return updateResult;
        }
    }
}