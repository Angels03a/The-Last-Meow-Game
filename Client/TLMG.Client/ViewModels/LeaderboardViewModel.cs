using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Models;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Gestiona el estado de la tabla de posiciones, el podio y los comandos de navegación/filtrado.
    /// </summary>
    public class LeaderboardViewModel : ViewModelBase
    {
        private const string DefaultProfileImage = "pack://application:,,,/TLMG.Client;component/img/default.jpg";
        private const string DefaultPlayerName = "---";
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
        private const string DefaultPointsFormat = "{0} pts";

        private PlayerRankEntry _firstPlace;
        private PlayerRankEntry _secondPlace;
        private PlayerRankEntry _thirdPlace;
        private string _firstPlaceScoreLabel;
        private string _secondPlaceScoreLabel;
        private string _thirdPlaceScoreLabel;
        private string _userStandingRankLabel;
        private string _userStandingPointsLabel;
        private string _currentPointsFormat = DefaultPointsFormat;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LeaderboardViewModel"/>.
        /// </summary>
        public LeaderboardViewModel()
        {
            RankedPlayers = new ObservableCollection<PlayerRankEntry>();
            FilterAllTimeCommand = new RelayCommand(ExecuteFilterAllTime);
            BackCommand = new RelayCommand(ExecuteBack);
            LoadEmptyLeaderboard();
        }

        /// <summary>
        /// Se produce cuando se solicita actualizar los datos del servidor.
        /// </summary>
        public event EventHandler RefreshRequested;

        /// <summary>
        /// Se produce cuando el usuario solicita regresar a la pantalla anterior.
        /// </summary>
        public event EventHandler BackRequested;

        /// <summary>Comando para consultar nuevamente la tabla de posiciones histórica.</summary>
        public ICommand FilterAllTimeCommand { get; }

        /// <summary>Comando para regresar al menú principal.</summary>
        public ICommand BackCommand { get; }

        /// <summary>
        /// Colección de jugadores a partir del cuarto lugar.
        /// </summary>
        public ObservableCollection<PlayerRankEntry> RankedPlayers { get; }

        public PlayerRankEntry FirstPlace
        {
            get { return _firstPlace; }
            private set { SetProperty(ref _firstPlace, value); }
        }

        public PlayerRankEntry SecondPlace
        {
            get { return _secondPlace; }
            private set { SetProperty(ref _secondPlace, value); }
        }

        public PlayerRankEntry ThirdPlace
        {
            get { return _thirdPlace; }
            private set { SetProperty(ref _thirdPlace, value); }
        }

        public string FirstPlaceScoreLabel
        {
            get { return _firstPlaceScoreLabel; }
            private set { SetProperty(ref _firstPlaceScoreLabel, value); }
        }

        public string SecondPlaceScoreLabel
        {
            get { return _secondPlaceScoreLabel; }
            private set { SetProperty(ref _secondPlaceScoreLabel, value); }
        }

        public string ThirdPlaceScoreLabel
        {
            get { return _thirdPlaceScoreLabel; }
            private set { SetProperty(ref _thirdPlaceScoreLabel, value); }
        }

        public string UserStandingRankLabel
        {
            get { return _userStandingRankLabel; }
            private set { SetProperty(ref _userStandingRankLabel, value); }
        }

        public string UserStandingPointsLabel
        {
            get { return _userStandingPointsLabel; }
            private set { SetProperty(ref _userStandingPointsLabel, value); }
        }

        /// <summary>
        /// Procesa los datos recibidos del servidor, calcula el WinRate y los distribuye entre el podio y la lista.
        /// </summary>
        public void ProcessLeaderboardData(LeaderboardPlayer[] players, string pointsFormat)
        {
            _currentPointsFormat = pointsFormat ?? DefaultPointsFormat;
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

        /// <summary>
        /// Restablece el podio y la tabla a valores predeterminados vacíos.
        /// </summary>
        public void LoadEmptyLeaderboard()
        {
            FirstPlace = CreateEmptyEntry(FirstPlaceRank);
            SecondPlace = CreateEmptyEntry(SecondPlaceRank);
            ThirdPlace = CreateEmptyEntry(ThirdPlaceRank);

            UpdatePodiumScoreLabels();
            RankedPlayers.Clear();
            UpdateUserStanding();
        }

        /// <summary>
        /// Actualiza el formato de los textos localizados en tiempo de ejecución.
        /// </summary>
        public void UpdateLocalizedFormats(string pointsFormat)
        {
            _currentPointsFormat = pointsFormat ?? DefaultPointsFormat;
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
            FirstPlaceScoreLabel = string.Format(_currentPointsFormat, FirstPlace.Score.ToString("N0"));
            SecondPlaceScoreLabel = string.Format(_currentPointsFormat, SecondPlace.Score.ToString("N0"));
            ThirdPlaceScoreLabel = string.Format(_currentPointsFormat, ThirdPlace.Score.ToString("N0"));
        }

        private void UpdateUserStanding()
        {
            UserStandingRankLabel = $"#{CurrentUserStandingRank}";
            UserStandingPointsLabel = string.Format(_currentPointsFormat, CurrentUserStandingPoints.ToString("N0"));
        }

        private PlayerRankEntry CreateEntry(int rank, LeaderboardPlayer player)
        {
            return new PlayerRankEntry
            {
                Rank = rank,
                AvatarImagePath = ResolveAvatarImagePath(player.ProfileImage),
                PlayerName = player.Nickname,
                Score = player.TotalPoints,
                Wins = player.TotalWins,
                WinRatePercentage = CalculateWinRate(player.TotalWins, player.TotalGamesPlayed)
            };
        }

        private PlayerRankEntry CreateEmptyEntry(int rank)
        {
            return new PlayerRankEntry
            {
                Rank = rank,
                AvatarImagePath = DefaultProfileImage,
                PlayerName = DefaultPlayerName,
                Score = DefaultEmptyScore,
                Wins = DefaultEmptyWins,
                WinRatePercentage = ZeroWinRate
            };
        }

        private string ResolveAvatarImagePath(string profileImage)
        {
            string resolvedPath;

            if (string.IsNullOrWhiteSpace(profileImage))
            {
                resolvedPath = DefaultProfileImage;
            }
            else if (profileImage.StartsWith("/"))
            {
                resolvedPath = "pack://application:,,," + profileImage;
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
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteBack()
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}