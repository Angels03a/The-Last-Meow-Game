using log4net;

using TLMG.Contracts.DataContracts;
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

        public PlayerProfileService(
            IPlayerProfileRepository playerProfileRepository,
            ISessionIdentityProvider sessionIdentityProvider)
        {
            _playerProfileRepository = playerProfileRepository;
            _sessionIdentityProvider = sessionIdentityProvider;
        }
        public OperationPlayerProfileResult GetProfile(string sessionToken)
        {
            OperationPlayerProfileResult profileResult = new OperationPlayerProfileResult();
            string errorKey = ErrorKeys.Common.InvalidSession;

            if (_sessionIdentityProvider.TryGetUserId(sessionToken, out int userId))
            {
                PlayerProfileLookup profileLookup = _playerProfileRepository.FindProfileByUserId(userId);
                errorKey = ResolveLookupErrorKey(profileLookup.Status);

                if (errorKey == null)
                {
                    profileResult.Nickname = profileLookup.Nickname;
                    profileResult.Email = profileLookup.Email;
                    profileResult.AvatarKey = AvatarCatalog.NormalizeKey(profileLookup.AvatarKey);
                    profileResult.PasswordChangedAtUtc = profileLookup.PasswordChangedAtUtc;
                }
            }

            profileResult.IsSuccess = errorKey == null;
            profileResult.ErrorKey = errorKey;

            return profileResult;
        }

        public OperationResult UpdateProfile(OperationUpdatePlayerProfileRequest updateProfileRequest)
        {
            OperationResult updateResult = new OperationResult();
            string errorKey = ValidateUpdateRequest(updateProfileRequest, out PlayerProfileUpdate profileUpdate);

            if (errorKey == string.Empty)
            {
                errorKey = SaveProfileUpdate(updateProfileRequest.SessionToken, profileUpdate);
            }

            updateResult.IsSuccess = errorKey == string.Empty;
            updateResult.ErrorKey = errorKey;

            return updateResult;
        }

        private static string ValidateUpdateRequest(
            OperationUpdatePlayerProfileRequest updateProfileRequest,
            out PlayerProfileUpdate profileUpdate)
        {
            profileUpdate = null;

            if (updateProfileRequest != null)
            {
                profileUpdate = new PlayerProfileUpdate
                {
                    Nickname = updateProfileRequest.Nickname,
                    AvatarKey = updateProfileRequest.AvatarKey
                };
            }

            string errorKey = PlayerProfileValidator.ValidateProfileUpdate(profileUpdate);

            return errorKey;
        }

        private string SaveProfileUpdate(string sessionToken, PlayerProfileUpdate profileUpdate)
        {
            string errorKey = ErrorKeys.Common.InvalidSession;

            if (_sessionIdentityProvider.TryGetUserId(sessionToken, out int userId))
            {
                errorKey = FindNicknameTakenErrorKey(userId, profileUpdate.Nickname);

                if (errorKey == string.Empty)
                {
                    errorKey = PersistProfile(sessionToken, userId, profileUpdate);
                }
            }

            return errorKey;
        }

        private string FindNicknameTakenErrorKey(int userId, string nickname)
        {
            string errorKey = ErrorKeys.Common.Unexpected;
            PlayerExistenceLookupStatus existenceStatus =
                _playerProfileRepository.FindNicknameExistenceStatusForOtherUsers(userId, nickname);

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

        private string PersistProfile(string sessionToken, int userId, PlayerProfileUpdate profileUpdate)
        {
            PlayerProfileUpdateStatus updateStatus = _playerProfileRepository.UpdateProfile(userId, profileUpdate);
            string errorKey = ResolveUpdateErrorKey(updateStatus);

            if (errorKey == string.Empty)
            {
                _sessionIdentityProvider.TryUpdateNickname(sessionToken, profileUpdate.Nickname);
                _log.InfoFormat("El jugador {0} actualizó su perfil.", userId);
            }

            return errorKey;
        }

        private static string ResolveLookupErrorKey(PlayerProfileLookupStatus lookupStatus)
        {
            string errorKey;

            switch (lookupStatus)
            {
                case PlayerProfileLookupStatus.Found:
                    errorKey = null;
                    break;
                case PlayerProfileLookupStatus.NotFound:
                    errorKey = ErrorKeys.Common.InvalidSession;
                    break;
                case PlayerProfileLookupStatus.DataFailure:
                    errorKey = ErrorKeys.Profile.DataModel;
                    break;
                default:
                    _log.ErrorFormat("Estado de consulta de perfil no reconocido: {0}.", lookupStatus);
                    errorKey = ErrorKeys.Common.Unexpected;
                    break;
            }

            return errorKey;
        }

        private static string ResolveUpdateErrorKey(PlayerProfileUpdateStatus updateStatus)
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
                    _log.ErrorFormat("Estado de actualización de perfil no reconocido: {0}.", updateStatus);
                    errorKey = ErrorKeys.Common.Unexpected;
                    break;
            }

            return errorKey;
        }
    }
}
