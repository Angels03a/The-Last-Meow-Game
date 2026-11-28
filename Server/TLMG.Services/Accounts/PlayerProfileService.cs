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

        public OperationPlayerProfileResult GetProfile(string sessionToken)
        {
            OperationPlayerProfileResult profileResult = new OperationPlayerProfileResult
            {
                ErrorKey = ErrorKeys.Common.Unexpected
            };
            SessionAuthorization authorization = _sessionAuthorizer.Authorize(sessionToken, SystemPermission.ManageProfile);

            if (authorization.Status != AuthorizationStatus.Granted)
            {
                profileResult.ErrorKey = authorization.ErrorKey;
            }
            else
            {
                PlayerProfileLookup profileLookup = _playerProfileRepository.FindProfileByUserId(authorization.UserId);

                if (profileLookup == null)
                {
                    _log.Warn("La consulta del perfil no devolvió un resultado.");
                    profileResult.ErrorKey = ErrorKeys.Profile.DataModel;
                }
                else if (profileLookup.Status == PlayerProfileLookupStatus.Found)
                {
                    ApplyProfileLookup(profileLookup, profileResult);
                }
                else if (profileLookup.Status == PlayerProfileLookupStatus.NotFound)
                {
                    profileResult.ErrorKey = ErrorKeys.Common.InvalidSession;
                }
                else if (profileLookup.Status == PlayerProfileLookupStatus.DataFailure)
                {
                    profileResult.ErrorKey = ErrorKeys.Profile.DataModel;
                }
            }

            return profileResult;
        }

        public OperationResult UpdateProfile(OperationUpdatePlayerProfileRequest updateProfileRequest)
        {
            OperationResult updateResult = new OperationResult { ErrorKey = ErrorKeys.Common.Unexpected };
            PlayerProfileUpdate profileUpdate = CreateProfileUpdate(updateProfileRequest);
            string validationErrorKey = PlayerProfileValidator.ValidateProfileUpdate(profileUpdate);

            if (updateProfileRequest != null && validationErrorKey == string.Empty)
            {
                updateResult = SaveProfileUpdate(updateProfileRequest.SessionToken, profileUpdate);
            }
            else if (!string.IsNullOrWhiteSpace(validationErrorKey))
            {
                updateResult.ErrorKey = validationErrorKey;
            }

            return updateResult;
        }

        private static void ApplyProfileLookup(PlayerProfileLookup lookup, OperationPlayerProfileResult result)
        {
            const int MinimumRank = 1;
            int initialStatisticValue = ValidationRules.InitialStatisticValue;
            bool hasValidStatistics = lookup.TotalPoints >= initialStatisticValue
                && lookup.TotalWins >= initialStatisticValue && lookup.TotalGamesPlayed >= lookup.TotalWins
                && lookup.PersonalRank >= MinimumRank;

            if (hasValidStatistics)
            {
                result.Nickname = lookup.Nickname;
                result.Email = lookup.Email;
                result.AvatarKey = AvatarCatalog.NormalizeKey(lookup.AvatarKey);
                result.PasswordChangedAtUtc = lookup.PasswordChangedAtUtc;
                result.TotalPoints = lookup.TotalPoints;
                result.TotalWins = lookup.TotalWins;
                result.TotalGamesPlayed = lookup.TotalGamesPlayed;
                result.PersonalRank = lookup.PersonalRank;
                result.IsSuccess = true;
                result.ErrorKey = string.Empty;
            }
            else
            {
                result.ErrorKey = ErrorKeys.Profile.DataModel;
                _log.Warn("La consulta del perfil devolvió estadísticas inválidas.");
            }
        }

        private static PlayerProfileUpdate CreateProfileUpdate(OperationUpdatePlayerProfileRequest updateProfileRequest)
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

        private OperationResult SaveProfileUpdate(string sessionToken, PlayerProfileUpdate profileUpdate)
        {
            OperationResult updateResult = new OperationResult { ErrorKey = ErrorKeys.Common.Unexpected };
            SessionAuthorization authorization = _sessionAuthorizer.Authorize(sessionToken, SystemPermission.ManageProfile);

            if (authorization.Status != AuthorizationStatus.Granted)
            {
                updateResult.ErrorKey = authorization.ErrorKey;
            }
            else
            {
                PlayerExistenceLookupStatus existenceStatus =
                    _playerProfileRepository.FindNicknameExistenceStatusForOtherUsers(
                        authorization.UserId, profileUpdate.Nickname);

                if (existenceStatus == PlayerExistenceLookupStatus.NotFound)
                {
                    updateResult = PersistProfile(sessionToken, authorization.UserId, profileUpdate);
                }
                else if (existenceStatus == PlayerExistenceLookupStatus.Exists)
                {
                    updateResult.ErrorKey = ErrorKeys.Profile.NicknameTaken;
                }
                else if (existenceStatus == PlayerExistenceLookupStatus.DataFailure)
                {
                    updateResult.ErrorKey = ErrorKeys.Profile.DataModel;
                }
            }

            return updateResult;
        }

        private OperationResult PersistProfile(string sessionToken, int userId, PlayerProfileUpdate profileUpdate)
        {
            PlayerProfileUpdateStatus updateStatus = _playerProfileRepository.UpdateProfile(userId, profileUpdate);
            OperationResult updateResult = new OperationResult { ErrorKey = ErrorKeys.Common.Unexpected };

            if (updateStatus == PlayerProfileUpdateStatus.Updated)
            {
                if (!_sessionIdentityProvider.TryUpdateNickname(sessionToken, profileUpdate.Nickname))
                {
                    _log.Info("El perfil se guardó después de finalizar la sesión.");
                }
                updateResult.IsSuccess = true;
                updateResult.ErrorKey = string.Empty;
                _log.InfoFormat("El jugador {0} actualizó su perfil.", userId);
            }
            else if (updateStatus == PlayerProfileUpdateStatus.NicknameTaken)
            {
                updateResult.ErrorKey = ErrorKeys.Profile.NicknameTaken;
            }
            else if (updateStatus == PlayerProfileUpdateStatus.PlayerNotFound)
            {
                updateResult.ErrorKey = ErrorKeys.Common.InvalidSession;
            }
            else if (updateStatus == PlayerProfileUpdateStatus.DataFailure)
            {
                updateResult.ErrorKey = ErrorKeys.Profile.DataModel;
            }

            return updateResult;
        }
    }
}