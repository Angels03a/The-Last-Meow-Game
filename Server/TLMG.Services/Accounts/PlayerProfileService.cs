using log4net;

using TLMG.Contracts.DataContracts;
using TLMG.Domain.Authorization;
using TLMG.Domain.Constants;
using TLMG.Domain.Entities;
using TLMG.Domain.Repositories;
using TLMG.Domain.Validators;
using TLMG.Services.Sessions;

namespace TLMG.Services.Accounts
{
    public class PlayerProfileService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PlayerProfileService));

        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ISessionIdentityProvider _sessionIdentityProvider;
        private readonly ISessionAuthorizer _sessionAuthorizer;

        public PlayerProfileService(
            IPlayerProfileRepository playerProfileRepository,
            ISessionIdentityProvider sessionIdentityProvider,
            ISessionAuthorizer sessionAuthorizer)
        {
            _playerProfileRepository = playerProfileRepository;
            _sessionIdentityProvider = sessionIdentityProvider;
            _sessionAuthorizer = sessionAuthorizer;
        }

        /// <summary>
        /// Retrieves profile information for an authorized registered player.
        /// </summary>
        public OperationPlayerProfileResult GetProfile(string sessionToken)
        {
            OperationPlayerProfileResult profileResult =
                new OperationPlayerProfileResult();

            SessionAuthorization authorization = _sessionAuthorizer.Authorize(
                sessionToken,
                SystemPermission.ManageProfile);

            string errorKey = authorization.ErrorKey;

            if (errorKey == null)
            {
                PlayerProfileLookup profileLookup =
                    _playerProfileRepository.FindProfileByUserId(
                        authorization.UserId);

                if (profileLookup == null)
                {
                    _log.Warn("La consulta de perfil no devolvió resultado.");
                    errorKey = ErrorKeys.Profile.DataModel;
                }
                else
                {
                    errorKey = ResolveLookupErrorKey(profileLookup.Status);

                    if (profileLookup.Status == PlayerProfileLookupStatus.Found)
                    {
                        profileResult.IsSuccess = true;
                        profileResult.Nickname = profileLookup.Nickname;
                        profileResult.Email = profileLookup.Email;
                        profileResult.AvatarKey =
                            AvatarCatalog.NormalizeKey(profileLookup.AvatarKey);
                        profileResult.PasswordChangedAtUtc =
                            profileLookup.PasswordChangedAtUtc;
                    }
                }
            }

            profileResult.ErrorKey = errorKey;

            return profileResult;
        }

        /// <summary>
        /// Updates the profile of a player with the required permissions.
        /// </summary>
        public OperationResult UpdateProfile(
            OperationUpdatePlayerProfileRequest updateProfileRequest)
        {
            OperationResult updateResult = new OperationResult();

            PlayerProfileUpdate profileUpdate =
                CreateProfileUpdate(updateProfileRequest);

            string errorKey =
                PlayerProfileValidator.ValidateProfileUpdate(profileUpdate);

            if (errorKey == string.Empty)
            {
                errorKey = SaveProfileUpdate(
                    updateProfileRequest.SessionToken,
                    profileUpdate);
            }

            updateResult.IsSuccess = errorKey == string.Empty;
            updateResult.ErrorKey = errorKey;

            return updateResult;
        }

        private static PlayerProfileUpdate CreateProfileUpdate(
            OperationUpdatePlayerProfileRequest updateProfileRequest)
        {
            PlayerProfileUpdate profileUpdate = null;

            if (updateProfileRequest != null)
            {
                profileUpdate = new PlayerProfileUpdate
                {
                    Nickname = updateProfileRequest.Nickname,
                    AvatarKey = updateProfileRequest.AvatarKey
                };
            }

            return profileUpdate;
        }

        private string SaveProfileUpdate(
            string sessionToken,
            PlayerProfileUpdate profileUpdate)
        {
            SessionAuthorization authorization = _sessionAuthorizer.Authorize(
                sessionToken,
                SystemPermission.ManageProfile);

            string errorKey = authorization.ErrorKey;

            if (errorKey == null)
            {
                errorKey = FindNicknameTakenErrorKey(
                    authorization.UserId,
                    profileUpdate.Nickname);

                if (errorKey == string.Empty)
                {
                    errorKey = PersistProfile(
                        sessionToken,
                        authorization.UserId,
                        profileUpdate);
                }
            }

            return errorKey;
        }

        private string FindNicknameTakenErrorKey(
            int userId,
            string nickname)
        {
            string errorKey = ErrorKeys.Common.Unexpected;

            PlayerExistenceLookupStatus existenceStatus =
                _playerProfileRepository.FindNicknameExistenceStatusForOtherUsers(
                    userId,
                    nickname);

            if (existenceStatus == PlayerExistenceLookupStatus.NotFound)
            {
                errorKey = string.Empty;
            }
            else if (existenceStatus == PlayerExistenceLookupStatus.Exists)
            {
                errorKey = ErrorKeys.Profile.NicknameTaken;
            }
            else if (existenceStatus == PlayerExistenceLookupStatus.DataFailure)
            {
                errorKey = ErrorKeys.Profile.DataModel;
            }

            return errorKey;
        }

        private string PersistProfile(
            string sessionToken,
            int userId,
            PlayerProfileUpdate profileUpdate)
        {
            PlayerProfileUpdateStatus updateStatus =
                _playerProfileRepository.UpdateProfile(userId, profileUpdate);

            string errorKey = ResolveUpdateErrorKey(updateStatus);

            if (errorKey == string.Empty)
            {
                _sessionIdentityProvider.TryUpdateNickname(
                    sessionToken,
                    profileUpdate.Nickname);

                _log.InfoFormat(
                    "El jugador {0} actualizó su perfil.",
                    userId);
            }

            return errorKey;
        }

        private static string ResolveLookupErrorKey(
            PlayerProfileLookupStatus lookupStatus)
        {
            string errorKey;

            switch (lookupStatus)
            {
                case PlayerProfileLookupStatus.Found:
                    errorKey = string.Empty;
                    break;

                case PlayerProfileLookupStatus.NotFound:
                    errorKey = ErrorKeys.Common.InvalidSession;
                    break;

                case PlayerProfileLookupStatus.DataFailure:
                    errorKey = ErrorKeys.Profile.DataModel;
                    break;

                default:
                    _log.ErrorFormat(
                        "Estado de consulta de perfil no reconocido: {0}.",
                        lookupStatus);

                    errorKey = ErrorKeys.Common.Unexpected;
                    break;
            }

            return errorKey;
        }

        private static string ResolveUpdateErrorKey(
            PlayerProfileUpdateStatus updateStatus)
        {
            string errorKey;

            switch (updateStatus)
            {
                case PlayerProfileUpdateStatus.Updated:
                    errorKey = string.Empty;
                    break;

                case PlayerProfileUpdateStatus.NicknameTaken:
                    errorKey = ErrorKeys.Profile.NicknameTaken;
                    break;

                case PlayerProfileUpdateStatus.PlayerNotFound:
                    errorKey = ErrorKeys.Common.InvalidSession;
                    break;

                case PlayerProfileUpdateStatus.DataFailure:
                    errorKey = ErrorKeys.Profile.DataModel;
                    break;

                default:
                    _log.ErrorFormat(
                        "Estado de actualización de perfil no reconocido: {0}.",
                        updateStatus);

                    errorKey = ErrorKeys.Common.Unexpected;
                    break;
            }

            return errorKey;
        }
    }
}
