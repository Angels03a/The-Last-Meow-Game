using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.Navigation;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Requests the leaderboard, fills the podium and remaining rows, and returns to the previous screen. Listens to
    /// server events only between Activate and Deactivate.
    /// </summary>
    public class LeaderboardViewModel : ViewModelBase
    {
        private const string DefaultPlayerName = "---";
        private const string PointsFormatKey = "Leaderboard_PointsFormat";
        private const string ConnectionErrorKey = "Leaderboard_ConnectionErrorMessage";
        private const string PlainPointsFormat = "{0}";
        private const string PersonalRankFormat = "#{0}";
        private const string ScoreNumberFormat = "N0";
        private const int RankIndexOffset = 1;
        private const int ZeroGamesPlayed = 0;
        private const int ZeroWinRate = 0;
        private const int PercentageMultiplier = 100;
        private const int DefaultEmptyScore = 0;
        private const int DefaultEmptyWins = 0;
        private const int PodiumCapacity = 3;
        private const int FirstPlaceIndex = 0;
        private const int SecondPlaceIndex = 1;
        private const int ThirdPlaceIndex = 2;
        private const int FirstPlaceRank = 1;
        private const int SecondPlaceRank = 2;
        private const int ThirdPlaceRank = 3;


        private readonly ILeaderboardConnection _leaderboardConnection;
        private readonly INavigationManager _navigationManager;
        private readonly IDialogManager _dialogManager;

        private PlayerRankEntry _firstPlace;
        private PlayerRankEntry _secondPlace;
        private PlayerRankEntry _thirdPlace;
        private string _firstPlaceScoreLabel;
        private string _secondPlaceScoreLabel;
        private string _thirdPlaceScoreLabel;
        private string _userStandingRankLabel;
        private string _userStandingPointsLabel;
        private string _currentPointsFormat = PlainPointsFormat;
        private int? _personalRank;
        private int? _personalPoints;

        /// <summary>
        /// Creates a <see cref="LeaderboardViewModel"/>.
        /// </summary>
        /// <param name="leaderboardConnection">Leaderboard connection to the server.</param>
        /// <param name="navigationManager">Navigation manager.</param>
        /// <param name="dialogManager">Dialog manager.</param>
        public LeaderboardViewModel(
            ILeaderboardConnection leaderboardConnection,
            INavigationManager navigationManager,
            IDialogManager dialogManager)
        {
            _leaderboardConnection = leaderboardConnection;
            _navigationManager = navigationManager;
            _dialogManager = dialogManager;

            RankedPlayers = new ObservableCollection<PlayerRankEntry>();
            FilterAllTimeCommand = new RelayCommand(ExecuteFilterAllTime);
            BackCommand = new RelayCommand(ExecuteBack);
            ShowEmptyLeaderboard();
        }

        /// <summary>Requests the historical leaderboard again.</summary>
        public ICommand FilterAllTimeCommand { get; }

        /// <summary>Returns to the previous screen.</summary>
        public ICommand BackCommand { get; }

        /// <summary>Players ranked fourth and below.</summary>
        public ObservableCollection<PlayerRankEntry> RankedPlayers { get; }

        /// <summary>First-place player.</summary>
        public PlayerRankEntry FirstPlace
        {
            get { return _firstPlace; }
            private set { SetProperty(ref _firstPlace, value); }
        }

        /// <summary>Second-place player.</summary>
        public PlayerRankEntry SecondPlace
        {
            get { return _secondPlace; }
            private set { SetProperty(ref _secondPlace, value); }
        }

        /// <summary>Third-place player.</summary>
        public PlayerRankEntry ThirdPlace
        {
            get { return _thirdPlace; }
            private set { SetProperty(ref _thirdPlace, value); }
        }

        /// <summary>First-place points formatted for the current language.</summary>
        public string FirstPlaceScoreLabel
        {
            get { return _firstPlaceScoreLabel; }
            private set { SetProperty(ref _firstPlaceScoreLabel, value); }
        }

        /// <summary>Second-place points formatted for the current language.</summary>
        public string SecondPlaceScoreLabel
        {
            get { return _secondPlaceScoreLabel; }
            private set { SetProperty(ref _secondPlaceScoreLabel, value); }
        }

        /// <summary>Third-place points formatted for the current language.</summary>
        public string ThirdPlaceScoreLabel
        {
            get { return _thirdPlaceScoreLabel; }
            private set { SetProperty(ref _thirdPlaceScoreLabel, value); }
        }

        /// <summary>Current player's rank, or a placeholder when no personal ranking is available.</summary>
        public string UserStandingRankLabel
        {
            get { return _userStandingRankLabel; }
            private set { SetProperty(ref _userStandingRankLabel, value); }
        }

        /// <summary>Current player's points, or a placeholder when no personal ranking is available.</summary>
        public string UserStandingPointsLabel
        {
            get { return _userStandingPointsLabel; }
            private set { SetProperty(ref _userStandingPointsLabel, value); }
        }

        /// <summary>
        /// Listens for server and language changes and requests the leaderboard when the view loads.
        /// </summary>
        public void Activate()
        {
            Deactivate();

            _leaderboardConnection.LeaderboardResultReceived += OnLeaderboardResultReceived;
            LanguageManager.LanguageChanged += OnLanguageChanged;

            UpdateLocalizedFormats();
            RequestLeaderboardData();
        }

        /// <summary>Stops listening for server and language changes and closes the query channel.</summary>
        public void Deactivate()
        {
            _leaderboardConnection.LeaderboardResultReceived -= OnLeaderboardResultReceived;
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            _leaderboardConnection.CloseLeaderboardChannel();
        }

        /// <summary>Releases subscriptions and closes the query channel.</summary>
        protected override void OnDisposed()
        {
            Deactivate();

            base.OnDisposed();
        }

        private void RequestLeaderboardData()
        {
            bool wasRequestSent = _leaderboardConnection.RequestLeaderboard();

            if (!wasRequestSent)
            {
                ShowLeaderboardError(ConnectionErrorKey);
            }
        }

        private void OnLeaderboardResultReceived(
            object sender,
            ServerEventArgs<OperationGetLeaderboardResult> eventArgs)
        {
            OperationGetLeaderboardResult leaderboardResult = eventArgs.Payload;

            if (leaderboardResult != null && leaderboardResult.IsSuccess)
            {
                _personalRank = leaderboardResult.PersonalRank;
                _personalPoints = leaderboardResult.PersonalPoints;
                UpdateLeaderboard(leaderboardResult.Players);
            }
            else
            {
                ShowLeaderboardError(leaderboardResult?.ErrorKey ?? ConnectionErrorKey);
            }
        }

        private void OnLanguageChanged(object sender, EventArgs eventArgs)
        {
            UpdateLocalizedFormats();
        }

        private void ShowLeaderboardError(string errorKey)
        {
            _dialogManager.ShowError(errorKey);
            ShowEmptyLeaderboard();
        }

        private void UpdateLeaderboard(LeaderboardPlayer[] players)
        {
            List<PlayerRankEntry> allEntries = new List<PlayerRankEntry>();

            if (players != null)
            {
                int currentRank = RankIndexOffset;
                foreach (LeaderboardPlayer player in players)
                {
                    allEntries.Add(CreateEntry(currentRank, player));
                    currentRank++;
                }
            }

            AssignPodium(allEntries);
            AssignRankedList(allEntries);
            UpdateUserStanding();
        }

        private void ShowEmptyLeaderboard()
        {
            _personalRank = null;
            _personalPoints = null;
            FirstPlace = CreateEmptyEntry(FirstPlaceRank);
            SecondPlace = CreateEmptyEntry(SecondPlaceRank);
            ThirdPlace = CreateEmptyEntry(ThirdPlaceRank);

            UpdatePodiumScoreLabels();
            RankedPlayers.Clear();
            UpdateUserStanding();
        }

        private void UpdateLocalizedFormats()
        {
            string pointsFormat = LanguageManager.FindText(PointsFormatKey);

            _currentPointsFormat = string.IsNullOrEmpty(pointsFormat) ? PlainPointsFormat : pointsFormat;
            UpdatePodiumScoreLabels();
            UpdateUserStanding();
        }

        private void AssignPodium(List<PlayerRankEntry> allEntries)
        {
            FirstPlace = allEntries.ElementAtOrDefault(FirstPlaceIndex) ?? CreateEmptyEntry(FirstPlaceRank);
            SecondPlace = allEntries.ElementAtOrDefault(SecondPlaceIndex) ?? CreateEmptyEntry(SecondPlaceRank);
            ThirdPlace = allEntries.ElementAtOrDefault(ThirdPlaceIndex) ?? CreateEmptyEntry(ThirdPlaceRank);

            UpdatePodiumScoreLabels();
        }

        private void AssignRankedList(List<PlayerRankEntry> allEntries)
        {
            RankedPlayers.Clear();

            for (int index = PodiumCapacity; index < allEntries.Count; index++)
            {
                RankedPlayers.Add(allEntries[index]);
            }
        }

        private void UpdatePodiumScoreLabels()
        {
            FirstPlaceScoreLabel = FormatPoints(FirstPlace.Score);
            SecondPlaceScoreLabel = FormatPoints(SecondPlace.Score);
            ThirdPlaceScoreLabel = FormatPoints(ThirdPlace.Score);
        }

        private void UpdateUserStanding()
        {
            UserStandingRankLabel = _personalRank.HasValue
                ? string.Format(CultureInfo.CurrentCulture, PersonalRankFormat, _personalRank.Value)
                : DefaultPlayerName;
            UserStandingPointsLabel = _personalPoints.HasValue
                ? FormatPoints(_personalPoints.Value)
                : DefaultPlayerName;
        }

        private string FormatPoints(int points)
        {
            string pointsText = string.Format(
                _currentPointsFormat,
                points.ToString(ScoreNumberFormat, CultureInfo.CurrentCulture));

            return pointsText;
        }

        private PlayerRankEntry CreateEntry(int rank, LeaderboardPlayer player)
        {
            PlayerRankEntry playerRankEntry = new PlayerRankEntry
            {
                Rank = rank,
                AvatarEmoji = AvatarCatalog.FindEmoji(player.ProfileImage),
                PlayerName = player.Nickname,
                Score = player.TotalPoints,
                Wins = player.TotalWins,
                WinRatePercentage = CalculateWinRate(player.TotalWins, player.TotalGamesPlayed)
            };

            return playerRankEntry;
        }

        private PlayerRankEntry CreateEmptyEntry(int rank)
        {
            PlayerRankEntry emptyEntry = new PlayerRankEntry
            {
                Rank = rank,
                AvatarEmoji = AvatarCatalog.FindEmoji(AvatarCatalog.DefaultAvatarKey),
                PlayerName = DefaultPlayerName,
                Score = DefaultEmptyScore,
                Wins = DefaultEmptyWins,
                WinRatePercentage = ZeroWinRate
            };

            return emptyEntry;
        }

        private int CalculateWinRate(int wins, int gamesPlayed)
        {
            int winRate = ZeroWinRate;

            if (gamesPlayed != ZeroGamesPlayed)
            {
                winRate = (int)Math.Round((double)wins / gamesPlayed * PercentageMultiplier);
            }

            return winRate;
        }

        private void ExecuteFilterAllTime()
        {
            RequestLeaderboardData();
        }

        private void ExecuteBack()
        {
            _navigationManager.GoBack();
        }
    }
}
