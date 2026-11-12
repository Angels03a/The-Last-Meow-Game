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
    /// ViewModel de la tabla de posiciones: pide el ranking al servidor, reparte el podio y la lista, y regresa a la
    /// pantalla anterior. Escucha al servidor solo entre Activate y Deactivate.
    /// </summary>
    public class LeaderboardViewModel : ViewModelBase
    {
        private const string DefaultProfileImage = "pack://application:,,,/TLMG.Client;component/img/default.jpg";
        private const string PackUriPrefix = "pack://application:,,,";
        private const string AppRelativePathPrefix = "/";
        private const string DefaultPlayerName = "---";
        private const string PointsFormatKey = "Leaderboard_PointsFormat";
        private const string ConnectionErrorKey = "Leaderboard_ConnectionErrorMessage";
        private const string PlainPointsFormat = "{0}";
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

        // FIXME: Mostrar el puesto y los puntos del jugador en sesión cuando el cliente los conozca. (BL-091).
        private const int CurrentUserStandingRank = 38;
        private const int CurrentUserStandingPoints = 1840;

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

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LeaderboardViewModel"/>.
        /// </summary>
        /// <param name="leaderboardConnection">Área de la tabla de posiciones de la conexión con el servidor.</param>
        /// <param name="navigationManager">Administrador de navegación.</param>
        /// <param name="dialogManager">Administrador de diálogos.</param>
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

        /// <summary>Comando para consultar nuevamente la tabla de posiciones histórica.</summary>
        public ICommand FilterAllTimeCommand { get; }

        /// <summary>Comando para regresar a la pantalla anterior.</summary>
        public ICommand BackCommand { get; }

        /// <summary>Colección de jugadores a partir del cuarto lugar.</summary>
        public ObservableCollection<PlayerRankEntry> RankedPlayers { get; }

        /// <summary>Jugador en el primer lugar del podio.</summary>
        public PlayerRankEntry FirstPlace
        {
            get { return _firstPlace; }
            private set { SetProperty(ref _firstPlace, value); }
        }

        /// <summary>Jugador en el segundo lugar del podio.</summary>
        public PlayerRankEntry SecondPlace
        {
            get { return _secondPlace; }
            private set { SetProperty(ref _secondPlace, value); }
        }

        /// <summary>Jugador en el tercer lugar del podio.</summary>
        public PlayerRankEntry ThirdPlace
        {
            get { return _thirdPlace; }
            private set { SetProperty(ref _thirdPlace, value); }
        }

        /// <summary>Puntos del primer lugar con el formato del idioma activo.</summary>
        public string FirstPlaceScoreLabel
        {
            get { return _firstPlaceScoreLabel; }
            private set { SetProperty(ref _firstPlaceScoreLabel, value); }
        }

        /// <summary>Puntos del segundo lugar con el formato del idioma activo.</summary>
        public string SecondPlaceScoreLabel
        {
            get { return _secondPlaceScoreLabel; }
            private set { SetProperty(ref _secondPlaceScoreLabel, value); }
        }

        /// <summary>Puntos del tercer lugar con el formato del idioma activo.</summary>
        public string ThirdPlaceScoreLabel
        {
            get { return _thirdPlaceScoreLabel; }
            private set { SetProperty(ref _thirdPlaceScoreLabel, value); }
        }

        /// <summary>Puesto del jugador en sesión (dato fijo mientras no llegue del servidor).</summary>
        public string UserStandingRankLabel
        {
            get { return _userStandingRankLabel; }
            private set { SetProperty(ref _userStandingRankLabel, value); }
        }

        /// <summary>Puntos del jugador en sesión (dato fijo mientras no lleguen del servidor).</summary>
        public string UserStandingPointsLabel
        {
            get { return _userStandingPointsLabel; }
            private set { SetProperty(ref _userStandingPointsLabel, value); }
        }

        /// <summary>
        /// Empieza a escuchar al servidor y al cambio de idioma, y pide la tabla de posiciones. La vista lo llama al
        /// cargarse.
        /// </summary>
        public void Activate()
        {
            Deactivate();

            _leaderboardConnection.LeaderboardResultReceived += OnLeaderboardResultReceived;
            LanguageManager.LanguageChanged += OnLanguageChanged;

            UpdateLocalizedFormats();
            RequestLeaderboardData();
        }

        /// <summary>Deja de escuchar, cierra el canal de la consulta y suelta el cambio de idioma.</summary>
        public void Deactivate()
        {
            _leaderboardConnection.LeaderboardResultReceived -= OnLeaderboardResultReceived;
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            _leaderboardConnection.CloseLeaderboardChannel();
        }

        /// <summary>Libera las suscripciones y cierra el canal de la consulta.</summary>
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

            if (leaderboardResult.IsSuccess)
            {
                ProcessLeaderboardData(leaderboardResult.Players);
            }
            else
            {
                ShowLeaderboardError(leaderboardResult.ErrorKey ?? ConnectionErrorKey);
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

        private void ProcessLeaderboardData(LeaderboardPlayer[] players)
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
            UserStandingRankLabel = $"#{CurrentUserStandingRank}";
            UserStandingPointsLabel = FormatPoints(CurrentUserStandingPoints);
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
                AvatarImagePath = ResolveAvatarImagePath(player.ProfileImage),
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
                AvatarImagePath = DefaultProfileImage,
                PlayerName = DefaultPlayerName,
                Score = DefaultEmptyScore,
                Wins = DefaultEmptyWins,
                WinRatePercentage = ZeroWinRate
            };

            return emptyEntry;
        }

        // HACK: Tratar como ruta de la aplicación el profile_image que guarda la BD, que no existe. (BL-045).
        private string ResolveAvatarImagePath(string profileImage)
        {
            string resolvedPath;

            if (string.IsNullOrWhiteSpace(profileImage))
            {
                resolvedPath = DefaultProfileImage;
            }
            else if (profileImage.StartsWith(AppRelativePathPrefix, StringComparison.Ordinal))
            {
                resolvedPath = PackUriPrefix + profileImage;
            }
            else
            {
                resolvedPath = profileImage;
            }

            if (!Uri.TryCreate(resolvedPath, UriKind.RelativeOrAbsolute, out _))
            {
                resolvedPath = DefaultProfileImage;
            }

            return resolvedPath;
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
