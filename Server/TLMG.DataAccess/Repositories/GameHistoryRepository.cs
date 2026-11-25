using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

using log4net;

using TLMG.DataAccess.DatabaseModel;
using TLMG.Domain.Constants;
using TLMG.Domain.Repositories;

namespace TLMG.DataAccess.Repositories
{
    public sealed class GameHistoryRepository : IGameHistoryRepository
    {
        private const int NoGameId = 0;
        private const int MinimumRegisteredUserId = 1;
        private const string InProgressGameState = "GAME_IN_PROGRESS";
        private const string AbandonedGameState = "GAME_ABANDONED";
        private const string FinishedGameState = "GAME_FINISHED";

        private static readonly ILog _log = LogManager.GetLogger(typeof(GameHistoryRepository));

        public GameRegistrationResult RegisterStartedMatch(GameRegistration gameRegistration)
        {
            GameRegistrationResult gameRegistrationResult = new GameRegistrationResult
            {
                GameRegistrationStatus = GameRegistrationStatus.DataFailure,
                GameId = NoGameId
            };

            string gameModeText;

            if (IsValidRegistration(gameRegistration)
                && TryConvertToModeText(gameRegistration.CardsPerPlayerCount, out gameModeText))
            {
                try
                {
                    gameRegistrationResult = RegisterMatchInTransaction(gameRegistration, gameModeText);
                }
                catch (SqlException sqlException)
                {
                    gameRegistrationResult = new GameRegistrationResult
                    {
                        GameRegistrationStatus = GameRegistrationStatus.DataFailure,
                        GameId = NoGameId
                    };
                    _log.Error("Match registration failed.", sqlException);
                }
                catch (DataException dataException)
                {
                    gameRegistrationResult = new GameRegistrationResult
                    {
                        GameRegistrationStatus = GameRegistrationStatus.DataFailure,
                        GameId = NoGameId
                    };
                    _log.Error("Match registration failed.", dataException);
                }
            }
            else
            {
                gameRegistrationResult.GameRegistrationStatus = GameRegistrationStatus.InvalidRegistration;
            }

            return gameRegistrationResult;
        }

        public bool AbandonUnpublishedMatch(Guid matchId)
        {
            bool wasMatchCancellationReconciled = false;

            if (matchId != Guid.Empty)
            {
                try
                {
                    using (TLMG_DBEntities context = new TLMG_DBEntities())
                    using (DbContextTransaction transaction =
                        context.Database.BeginTransaction(IsolationLevel.Serializable))
                    {
                        Game existingGame = (from game in context.Game.Include(nameof(Game.Game_Player))
                                             where game.match_id == matchId
                                             select game).FirstOrDefault();

                        if (existingGame == null || existingGame.state == AbandonedGameState)
                        {
                            transaction.Commit();
                            wasMatchCancellationReconciled = true;
                        }
                        else if (existingGame.state == InProgressGameState)
                        {
                            CloseGameWithoutPoints(existingGame, DateTime.UtcNow);
                            context.SaveChanges();
                            transaction.Commit();
                            wasMatchCancellationReconciled = true;
                        }
                        else
                        {
                            _log.WarnFormat("Cannot abandon match {0} in state {1}.",
                                matchId, existingGame.state);
                        }
                    }
                }
                catch (SqlException sqlException)
                {
                    wasMatchCancellationReconciled = false;
                    _log.Error("Match cancellation failed.", sqlException);
                }
                catch (DataException dataException)
                {
                    wasMatchCancellationReconciled = false;
                    _log.Error("Match cancellation failed.", dataException);
                }
            }

            return wasMatchCancellationReconciled;
        }

        public bool RecoverInterruptedMatches()
        {
            bool wasRecoverySuccessful = false;

            try
            {
                using (TLMG_DBEntities context = new TLMG_DBEntities())
                using (DbContextTransaction transaction =
                    context.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    List<Game> interruptedGames = (from game in context.Game.Include(nameof(Game.Game_Player))
                                                   where game.match_id.HasValue
                                                       && game.state == InProgressGameState
                                                   select game).ToList();

                    DateTime recoveryTimeUtc = DateTime.UtcNow;

                    foreach (Game interruptedGame in interruptedGames)
                    {
                        CloseGameWithoutPoints(interruptedGame, recoveryTimeUtc);
                    }

                    context.SaveChanges();
                    transaction.Commit();
                }

                wasRecoverySuccessful = true;
            }
            catch (SqlException sqlException)
            {
                wasRecoverySuccessful = false;
                _log.Error("Interrupted match recovery failed.", sqlException);
            }
            catch (DataException dataException)
            {
                wasRecoverySuccessful = false;
                _log.Error("Interrupted match recovery failed.", dataException);
            }

            return wasRecoverySuccessful;
        }

        public GameFinalizationStatus FinalizeMatch(GameFinalization gameFinalization)
        {
            GameFinalizationStatus finalizationStatus = GameFinalizationStatus.InvalidFinalization;

            if (IsValidFinalization(gameFinalization))
            {
                finalizationStatus = GameFinalizationStatus.DataFailure;

                try
                {
                    using (TLMG_DBEntities context = new TLMG_DBEntities())
                    using (DbContextTransaction transaction =
                        context.Database.BeginTransaction(IsolationLevel.Serializable))
                    {
                        Guid matchId = gameFinalization.MatchId;
                        Game existingGame = (from game in context.Game
                                             where game.match_id == matchId
                                             select game).FirstOrDefault();

                        if (existingGame == null)
                        {
                            finalizationStatus = GameFinalizationStatus.FinalizationConflict;
                        }
                        else
                        {
                            List<Game_Player> registeredParticipants =
                                (from participant in context.Game_Player.Include(nameof(Game_Player.Player))
                                 where participant.id_game == existingGame.id_game && participant.id_player.HasValue
                                 select participant).ToList();

                            GameFinalizationStatus transactionStatus = ApplyMatchFinalization(
                                existingGame, registeredParticipants, gameFinalization);

                            if (transactionStatus == GameFinalizationStatus.Saved)
                            {
                                context.SaveChanges();
                            }

                            if (transactionStatus == GameFinalizationStatus.Saved
                                || transactionStatus == GameFinalizationStatus.AlreadySaved)
                            {
                                transaction.Commit();
                            }

                            finalizationStatus = transactionStatus;
                        }
                    }
                }
                catch (SqlException sqlException)
                {
                    finalizationStatus = GameFinalizationStatus.DataFailure;
                    _log.Error("Match finalization failed.", sqlException);
                }
                catch (DataException dataException)
                {
                    finalizationStatus = GameFinalizationStatus.DataFailure;
                    _log.Error("Match finalization failed.", dataException);
                }
            }

            return finalizationStatus;
        }

        private static GameFinalizationStatus ApplyMatchFinalization(Game game,
            List<Game_Player> registeredParticipants, GameFinalization gameFinalization)
        {
            GameFinalizationStatus finalizationStatus = GameFinalizationStatus.FinalizationConflict;
            Dictionary<int, GameParticipantRecord> participantRecordsByUserId =
                new Dictionary<int, GameParticipantRecord>();

            foreach (GameParticipantRecord participantRecord in gameFinalization.Participants)
            {
                participantRecordsByUserId.Add(participantRecord.UserId, participantRecord);
            }
            DateTime endedAtUtc = TruncateToDatabasePrecision(gameFinalization.EndedAtUtc);
            string finalGameState = gameFinalization.IsAbandoned ? AbandonedGameState : FinishedGameState;

            bool hasMatchingParticipants = HasMatchingFinalizationParticipants(
                registeredParticipants, participantRecordsByUserId, endedAtUtc);
            bool hasValidGameDates = game.game_start_date.HasValue && game.game_start_date.Value <= endedAtUtc;

            if (hasMatchingParticipants && hasValidGameDates)
            {
                bool hasSavedGame = game.state == finalGameState && game.game_ending_date == endedAtUtc;
                if (hasSavedGame && HasSavedParticipantRecords(registeredParticipants, participantRecordsByUserId))
                {
                    finalizationStatus = GameFinalizationStatus.AlreadySaved;
                }
                else if (CanFinalizeGame(game, registeredParticipants))
                {
                    SaveParticipantRecords(registeredParticipants, participantRecordsByUserId, gameFinalization.IsAbandoned);
                    game.state = finalGameState;
                    game.game_ending_date = endedAtUtc;
                    finalizationStatus = GameFinalizationStatus.Saved;
                }
            }

            return finalizationStatus;
        }

        private static bool CanFinalizeGame(Game game, List<Game_Player> registeredParticipants)
        {
            bool canFinalizeGame = game.state == InProgressGameState && !game.game_ending_date.HasValue
                && registeredParticipants.All(HasInitialParticipantState);

            return canFinalizeGame;
        }

        private static bool HasMatchingFinalizationParticipants(List<Game_Player> registeredParticipants,
            Dictionary<int, GameParticipantRecord> participantRecordsByUserId, DateTime endedAtUtc)
        {
            bool hasMatchingParticipants = registeredParticipants.Count == participantRecordsByUserId.Count;
            HashSet<int> matchedUserIds = new HashSet<int>();

            if (hasMatchingParticipants)
            {
                foreach (Game_Player participant in registeredParticipants)
                {
                    GameParticipantRecord participantRecord;
                    if (participant.Player == null
                        || !matchedUserIds.Add(participant.Player.id_user)
                        || !participantRecordsByUserId.TryGetValue(participant.Player.id_user, out participantRecord)
                        || !HasValidParticipantDates(participant, participantRecord, endedAtUtc))
                    {
                        hasMatchingParticipants = false;
                        break;
                    }
                }
            }

            return hasMatchingParticipants;
        }

        private static bool HasValidParticipantDates(Game_Player participant,
            GameParticipantRecord participantRecord, DateTime endedAtUtc)
        {
            DateTime? leftAtUtc = NormalizeExitDate(participantRecord.LeftAtUtc);
            bool hasValidDates = participant.joined_at <= endedAtUtc
                && (!leftAtUtc.HasValue || leftAtUtc.Value >= participant.joined_at);

            return hasValidDates;
        }

        private static bool HasInitialParticipantState(Game_Player participant)
        {
            bool hasInitialState = !participant.final_position.HasValue
                && participant.outcome == null
                && participant.points_earned == ValidationRules.InitialStatisticValue
                && !participant.left_at.HasValue;

            return hasInitialState;
        }

        private static bool HasSavedParticipantRecords(List<Game_Player> registeredParticipants,
            Dictionary<int, GameParticipantRecord> participantRecordsByUserId)
        {
            bool hasSavedRecords = true;

            foreach (Game_Player participant in registeredParticipants)
            {
                GameParticipantRecord participantRecord = participantRecordsByUserId[participant.Player.id_user];
                if (participant.final_position != participantRecord.Position
                    || participant.points_earned != participantRecord.Points
                    || participant.outcome != ConvertFinalStatusToDatabaseValue(participantRecord.FinalStatus)
                    || participant.left_at != NormalizeExitDate(participantRecord.LeftAtUtc))
                {
                    hasSavedRecords = false;
                    break;
                }
            }

            return hasSavedRecords;
        }

        private static void SaveParticipantRecords(List<Game_Player> registeredParticipants,
            Dictionary<int, GameParticipantRecord> participantRecordsByUserId, bool isAbandoned)
        {
            DateTime pointsReachedAtUtc = DateTime.UtcNow;

            foreach (Game_Player participant in registeredParticipants)
            {
                GameParticipantRecord participantRecord = participantRecordsByUserId[participant.Player.id_user];
                participant.final_position = participantRecord.Position;
                participant.points_earned = participantRecord.Points;
                participant.outcome = ConvertFinalStatusToDatabaseValue(participantRecord.FinalStatus);
                participant.left_at = NormalizeExitDate(participantRecord.LeftAtUtc);

                if (!isAbandoned)
                {
                    UpdatePlayerStatistics(participant.Player, participantRecord, pointsReachedAtUtc);
                }
            }
        }

        private static void UpdatePlayerStatistics(Player player, GameParticipantRecord participantRecord,
            DateTime pointsReachedAtUtc)
        {
            const int statisticIncrement = 1;
            player.total_points += participantRecord.Points;
            player.total_games_played += statisticIncrement;

            if (participantRecord.Points > ValidationRules.InitialStatisticValue)
            {
                player.points_reached_at_utc = pointsReachedAtUtc;
            }

            if (participantRecord.FinalStatus == GameParticipantFinalStatus.Winner)
            {
                player.total_wins += statisticIncrement;
            }
        }

        private static DateTime? NormalizeExitDate(DateTime? leftAtUtc)
        {
            DateTime? normalizedDate = leftAtUtc.HasValue
                ? (DateTime?)TruncateToDatabasePrecision(leftAtUtc.Value) : null;

            return normalizedDate;
        }

        private static bool IsValidFinalization(GameFinalization gameFinalization)
        {
            bool isValidFinalization = gameFinalization != null
                && gameFinalization.MatchId != Guid.Empty
                && gameFinalization.EndedAtUtc.Kind == DateTimeKind.Utc
                && gameFinalization.Participants != null
                && gameFinalization.Participants.Count <= ValidationRules.MaximumPlayersPerMatchCount;

            if (isValidFinalization)
            {
                isValidFinalization = HasValidParticipantRecords(gameFinalization);
            }

            return isValidFinalization;
        }

        private static bool HasValidParticipantRecords(GameFinalization gameFinalization)
        {
            bool hasValidRecords = true;
            HashSet<int> registeredUserIds = new HashSet<int>();
            HashSet<int> finalPositions = new HashSet<int>();

            foreach (GameParticipantRecord participantRecord in gameFinalization.Participants)
            {
                if (!IsValidParticipantRecord(participantRecord, gameFinalization)
                    || !registeredUserIds.Add(participantRecord.UserId))
                {
                    hasValidRecords = false;
                    break;
                }

                if (participantRecord.Position.HasValue && !finalPositions.Add(participantRecord.Position.Value))
                {
                    hasValidRecords = false;
                    break;
                }
            }

            return hasValidRecords;
        }

        private static bool IsValidParticipantRecord(GameParticipantRecord participantRecord,
            GameFinalization gameFinalization)
        {
            bool isValidRecord = participantRecord != null
                && participantRecord.UserId >= MinimumRegisteredUserId
                && participantRecord.Points >= ValidationRules.InitialStatisticValue
                && Enum.IsDefined(typeof(GameParticipantFinalStatus), participantRecord.FinalStatus);

            if (isValidRecord)
            {
                isValidRecord = HasValidParticipantExit(participantRecord, gameFinalization.EndedAtUtc)
                    && HasValidFinalPosition(participantRecord, gameFinalization.IsAbandoned);
            }

            return isValidRecord;
        }

        private static bool HasValidParticipantExit(GameParticipantRecord participantRecord, DateTime endedAtUtc)
        {
            bool hasLeft = participantRecord.FinalStatus == GameParticipantFinalStatus.Abandoned
                || participantRecord.FinalStatus == GameParticipantFinalStatus.Kicked;
            bool hasValidExit = hasLeft == participantRecord.LeftAtUtc.HasValue;

            if (hasValidExit && hasLeft)
            {
                hasValidExit = participantRecord.LeftAtUtc.Value.Kind == DateTimeKind.Utc
                    && participantRecord.LeftAtUtc.Value <= endedAtUtc
                    && participantRecord.Points == ValidationRules.InitialStatisticValue;
            }

            return hasValidExit;
        }

        private static bool HasValidFinalPosition(GameParticipantRecord participantRecord, bool isAbandoned)
        {
            bool hasValidPosition;

            if (isAbandoned)
            {
                hasValidPosition = !participantRecord.Position.HasValue
                    && participantRecord.Points == ValidationRules.InitialStatisticValue
                    && (participantRecord.FinalStatus == GameParticipantFinalStatus.Unclassified
                        || participantRecord.FinalStatus == GameParticipantFinalStatus.Abandoned
                        || participantRecord.FinalStatus == GameParticipantFinalStatus.Kicked);
            }
            else
            {
                hasValidPosition = HasRankedPosition(participantRecord);
            }

            return hasValidPosition;
        }

        private static bool HasRankedPosition(GameParticipantRecord participantRecord)
        {
            const int firstPosition = 1;
            bool isWinner = participantRecord.FinalStatus == GameParticipantFinalStatus.Winner;
            bool hasRankedPosition = participantRecord.Position.HasValue
                && participantRecord.Position.Value >= firstPosition
                && participantRecord.Position.Value <= ValidationRules.MaximumPlayersPerMatchCount
                && isWinner == (participantRecord.Position.Value == firstPosition)
                && participantRecord.FinalStatus != GameParticipantFinalStatus.Unclassified;

            return hasRankedPosition;
        }

        private static string ConvertFinalStatusToDatabaseValue(GameParticipantFinalStatus participantFinalStatus)
        {
            const string winnerStatus = "OUTCOME_WINNER";
            const string eliminatedStatus = "OUTCOME_ELIMINATED";
            const string abandonedStatus = "OUTCOME_ABANDONED";
            const string kickedStatus = "OUTCOME_KICKED";
            string databaseFinalStatus = null;

            switch (participantFinalStatus)
            {
                case GameParticipantFinalStatus.Winner:
                    databaseFinalStatus = winnerStatus;
                    break;
                case GameParticipantFinalStatus.Eliminated:
                    databaseFinalStatus = eliminatedStatus;
                    break;
                case GameParticipantFinalStatus.Abandoned:
                    databaseFinalStatus = abandonedStatus;
                    break;
                case GameParticipantFinalStatus.Kicked:
                    databaseFinalStatus = kickedStatus;
                    break;
            }

            return databaseFinalStatus;
        }

        private static GameRegistrationResult RegisterMatchInTransaction(
            GameRegistration gameRegistration, string gameModeText)
        {
            GameRegistrationResult gameRegistrationResult;

            using (TLMG_DBEntities context = new TLMG_DBEntities())
            using (DbContextTransaction transaction =
                context.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                Guid matchId = gameRegistration.MatchId;
                Game existingGame = (from game in context.Game
                                     where game.match_id == matchId
                                     select game).FirstOrDefault();

                if (existingGame == null)
                {
                    gameRegistrationResult = InsertGameWithParticipants(context, gameRegistration, gameModeText);
                }
                else
                {
                    gameRegistrationResult = new GameRegistrationResult
                    {
                        GameRegistrationStatus = GameRegistrationStatus.RegistrationConflict,
                        GameId = NoGameId
                    };

                    if (HasMatchingRegistration(existingGame, gameRegistration, gameModeText)
                        && HasMatchingRegisteredParticipants(context, existingGame.id_game, gameRegistration.RegisteredUserIds))
                    {
                        gameRegistrationResult.GameRegistrationStatus = GameRegistrationStatus.AlreadyRegistered;
                        gameRegistrationResult.GameId = existingGame.id_game;
                    }
                    else
                    {
                        _log.WarnFormat("Registration conflict for match {0}.", matchId);
                    }
                }

                if (gameRegistrationResult.GameRegistrationStatus == GameRegistrationStatus.Created
                    || gameRegistrationResult.GameRegistrationStatus == GameRegistrationStatus.AlreadyRegistered)
                {
                    transaction.Commit();
                }
            }

            return gameRegistrationResult;
        }

        private static bool HasMatchingRegistration(Game existingGame,
            GameRegistration gameRegistration, string gameModeText)
        {
            bool hasMatchingRegistration = existingGame.state == InProgressGameState
                && existingGame.mode == gameModeText
                && existingGame.lobby_open_date == TruncateToDatabasePrecision(gameRegistration.LobbyOpenedAtUtc)
                && existingGame.game_start_date == TruncateToDatabasePrecision(gameRegistration.StartedAtUtc);

            return hasMatchingRegistration;
        }

        private static bool HasMatchingRegisteredParticipants(
            TLMG_DBEntities context, int gameId, IReadOnlyList<int> registeredUserIds)
        {
            List<Game_Player> registeredParticipants =
                (from participant in context.Game_Player.Include(nameof(Game_Player.Player))
                 where participant.id_game == gameId && participant.id_player.HasValue
                 select participant).ToList();

            HashSet<int> persistedUserIds = new HashSet<int>();
            bool hasValidParticipants = true;

            foreach (Game_Player participant in registeredParticipants)
            {
                if (!HasInitialParticipantState(participant)
                    || participant.Player == null || !persistedUserIds.Add(participant.Player.id_user))
                {
                    hasValidParticipants = false;
                    break;
                }
            }

            bool hasMatchingParticipants = hasValidParticipants && persistedUserIds.SetEquals(registeredUserIds);

            return hasMatchingParticipants;
        }

        private static GameRegistrationResult InsertGameWithParticipants(
            TLMG_DBEntities context, GameRegistration gameRegistration, string gameModeText)
        {
            GameRegistrationResult gameRegistrationResult = new GameRegistrationResult
            {
                GameRegistrationStatus = GameRegistrationStatus.RegisteredPlayerNotFound,
                GameId = NoGameId
            };

            const int emptyParticipantCount = 0;
            List<int> registeredUserIds = new List<int>(gameRegistration.RegisteredUserIds);
            List<int> registeredPlayerIds = new List<int>();

            if (registeredUserIds.Count > emptyParticipantCount)
            {
                registeredPlayerIds = (from player in context.Player
                                       where registeredUserIds.Contains(player.id_user)
                                       select player.id_player).ToList();
            }

            if (registeredPlayerIds.Count == registeredUserIds.Count)
            {
                DateTime startedAtUtc = TruncateToDatabasePrecision(gameRegistration.StartedAtUtc);
                Game newGame = new Game
                {
                    match_id = gameRegistration.MatchId,
                    state = InProgressGameState,
                    mode = gameModeText,
                    lobby_open_date = TruncateToDatabasePrecision(gameRegistration.LobbyOpenedAtUtc),
                    game_start_date = startedAtUtc
                };

                context.Game.Add(newGame);

                foreach (int registeredPlayerId in registeredPlayerIds)
                {
                    Game_Player newParticipant = new Game_Player
                    {
                        Game = newGame,
                        id_player = registeredPlayerId,
                        points_earned = ValidationRules.InitialStatisticValue,
                        joined_at = startedAtUtc
                    };

                    context.Game_Player.Add(newParticipant);
                }

                context.SaveChanges();
                gameRegistrationResult.GameRegistrationStatus = GameRegistrationStatus.Created;
                gameRegistrationResult.GameId = newGame.id_game;
            }

            return gameRegistrationResult;
        }

        private static void CloseGameWithoutPoints(Game game, DateTime closedAtUtc)
        {
            DateTime endingDateUtc = TruncateToDatabasePrecision(closedAtUtc);

            if (game.game_start_date.HasValue && game.game_start_date.Value > endingDateUtc)
            {
                endingDateUtc = game.game_start_date.Value;
            }

            game.state = AbandonedGameState;
            game.game_ending_date = endingDateUtc;

            foreach (Game_Player participant in game.Game_Player)
            {
                if (!participant.left_at.HasValue)
                {
                    participant.left_at = endingDateUtc >= participant.joined_at
                        ? endingDateUtc
                        : participant.joined_at;
                }
            }
        }

        private static DateTime TruncateToDatabasePrecision(DateTime dateTime)
        {
            long preciseTicks = dateTime.Ticks - dateTime.Ticks % TimeSpan.TicksPerMillisecond;
            DateTime preciseDateTime = new DateTime(preciseTicks, dateTime.Kind);

            return preciseDateTime;
        }

        private static bool IsValidRegistration(GameRegistration gameRegistration)
        {
            bool isValidRegistration = gameRegistration != null
                && gameRegistration.MatchId != Guid.Empty
                && gameRegistration.LobbyOpenedAtUtc.Kind == DateTimeKind.Utc
                && gameRegistration.StartedAtUtc.Kind == DateTimeKind.Utc
                && gameRegistration.LobbyOpenedAtUtc <= gameRegistration.StartedAtUtc
                && gameRegistration.RegisteredUserIds != null
                && gameRegistration.RegisteredUserIds.Count <= ValidationRules.MaximumPlayersPerMatchCount;

            if (isValidRegistration)
            {
                isValidRegistration = HasUniqueRegisteredUsers(gameRegistration.RegisteredUserIds);
            }

            return isValidRegistration;
        }

        private static bool HasUniqueRegisteredUsers(IReadOnlyList<int> registeredUserIds)
        {
            bool hasUniqueUsers = true;
            HashSet<int> uniqueUserIds = new HashSet<int>();

            foreach (int userId in registeredUserIds)
            {
                if (userId < MinimumRegisteredUserId || !uniqueUserIds.Add(userId))
                {
                    hasUniqueUsers = false;
                    break;
                }
            }

            return hasUniqueUsers;
        }

        private static bool TryConvertToModeText(int cardsPerPlayerCount, out string gameModeText)
        {
            const string fourCardsMode = "MODE_FOUR_CARDS";
            const string sixCardsMode = "MODE_SIX_CARDS";
            const string eightCardsMode = "MODE_EIGHT_CARDS";

            bool isValidMode = true;
            gameModeText = null;

            switch (cardsPerPlayerCount)
            {
                case ValidationRules.FourCardsPerPlayerCount:
                    gameModeText = fourCardsMode;
                    break;
                case ValidationRules.SixCardsPerPlayerCount:
                    gameModeText = sixCardsMode;
                    break;
                case ValidationRules.EightCardsPerPlayerCount:
                    gameModeText = eightCardsMode;
                    break;
                default:
                    isValidMode = false;
                    break;
            }

            return isValidMode;
        }
    }
}
