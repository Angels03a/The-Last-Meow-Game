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

                if (profileResult.IsSuccess && profileResult.ErrorKey != string.Empty)
                {
                    _log.Warn("El servidor devolvió una consulta de perfil inconsistente.");
                    profileResult = CreateFailedResult<OperationPlayerProfileResult>("Common_UnexpectedError");
                }
                else if (!profileResult.IsSuccess && (string.IsNullOrWhiteSpace(profileResult.ErrorKey)
                    || (System.Windows.Application.Current != null
                        && !(System.Windows.Application.Current.TryFindResource(profileResult.ErrorKey) is string))))
                {
                    _log.Warn("El servidor devolvió una clave de consulta de perfil inválida.");
                    profileResult.ErrorKey = "Common_UnexpectedError";
                }
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

                if (updateResult.IsSuccess && updateResult.ErrorKey != string.Empty)
                {
                    _log.Warn("El servidor devolvió un resultado de perfil inconsistente.");
                    updateResult.IsSuccess = false;
                    updateResult.ErrorKey = ProfileUnexpectedErrorKey;
                }
                else if (!updateResult.IsSuccess && (string.IsNullOrWhiteSpace(updateResult.ErrorKey)
                    || (System.Windows.Application.Current != null
                        && !(System.Windows.Application.Current.TryFindResource(updateResult.ErrorKey) is string))))
                {
                    _log.Warn("El servidor devolvió una clave de error de perfil inválida.");
                    updateResult.ErrorKey = ProfileUnexpectedErrorKey;
                }

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
