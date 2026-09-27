using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Data.Entity.Core;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TLMG.Models;
using TLMG.DatabaseModel;

namespace TLMG.Views
{
    /// <summary>
    /// Pantalla que muestra el podio y la tabla de posiciones de los jugadores (leaderboard).
    /// </summary>
    public partial class LeaderboardScreen : UserControl
    {
        private const string DefaultPointsFormat = "{0} pts";
        private const string PointsSuffixResourceKey = "Leaderboard_PointsSuffix";
        private const string DefaultProfileImage = "pack://application:,,,/TLMG;component/img/default.jpg";
        private const string DefaultPlayerName = "---";
        private const int ZeroGamesPlayed = 0;
        private const int ZeroWinRate = 0;
        private const int PercentageMultiplier = 100;
        private const int DefaultEmptyScore = 0;
        private const int DefaultEmptyWins = 0;

        // FIXME: Estos valores deben venir del jugador actualmente logueado en la sesión.
        private const int CurrentUserStandingRank = 38;
        private const int CurrentUserStandingPoints = 1840;

        private PlayerRankEntry _firstPlacePlayer;
        private PlayerRankEntry _secondPlacePlayer;
        private PlayerRankEntry _thirdPlacePlayer;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LeaderboardScreen"/>.
        /// </summary>
        public LeaderboardScreen()
        {
            InitializeComponent();
            Loaded += LeaderboardScreen_Loaded;
            Unloaded += LeaderboardScreen_Unloaded;

            LoadLeaderboardData();
            UpdateDynamicLabels();
        }

        private void LeaderboardScreen_Loaded(object eventSender, RoutedEventArgs eventArguments)
        {
            LanguageManager.LanguageChanged += LanguageManager_LanguageChanged;
            UpdateDynamicLabels();
        }

        private void LeaderboardScreen_Unloaded(object eventSender, RoutedEventArgs eventArguments)
        {
            LanguageManager.LanguageChanged -= LanguageManager_LanguageChanged;
        }

        private void LanguageManager_LanguageChanged(object eventSender, EventArgs eventArguments)
        {
            UpdateDynamicLabels();
        }

        private void LoadLeaderboardData()
        {
            try
            {
                using (var database = new TLMG_DBEntities())
                {
                    var rankedPlayersQuery = (from player in database.Player
                                              join user in database.User on player.id_user equals user.id_user
                                              where user.state == "USER_ACTIVE"
                                              orderby player.total_points descending
                                              select new
                                              {
                                                  user.nickname,
                                                  user.profile_image,
                                                  player.total_points,
                                                  player.total_wins,
                                                  player.total_games_played
                                              }).ToList();

                    var rankEntries = rankedPlayersQuery.Select((data, index) => new PlayerRankEntry
                    {
                        Rank = index + 1,
                        AvatarImagePath = string.IsNullOrWhiteSpace(data.profile_image)
                             ? DefaultProfileImage
                             : (data.profile_image.StartsWith("/") ? "pack://application:,,," + data.profile_image : data.profile_image),
                        PlayerName = data.nickname,
                        Score = data.total_points,
                        Wins = data.total_wins,
                        WinRatePercentage = CalculateWinRate(data.total_wins, data.total_games_played)
                    }).ToList();

                    AssignPodiumPlayers(rankEntries);
                    AssignRankedList(rankEntries);
                }
            }
            catch (SqlException)
            {
                MessageBox.Show("Error de conexión al cargar la tabla de posiciones.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadEmptyLeaderboard();
            }
            catch (EntityException)
            {
                MessageBox.Show("Error interno en el modelo de datos.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadEmptyLeaderboard();
            }
        }

        private int CalculateWinRate(int wins, int gamesPlayed)
        {
            return gamesPlayed == ZeroGamesPlayed
                ? ZeroWinRate
                : (int)Math.Round((double)wins / gamesPlayed * PercentageMultiplier);
        }

        private void AssignPodiumPlayers(System.Collections.Generic.List<PlayerRankEntry> allPlayers)
        {
            _firstPlacePlayer = allPlayers.ElementAtOrDefault(0) ?? CreateEmptyEntry(1);
            _secondPlacePlayer = allPlayers.ElementAtOrDefault(1) ?? CreateEmptyEntry(2);
            _thirdPlacePlayer = allPlayers.ElementAtOrDefault(2) ?? CreateEmptyEntry(3);

            SetPodiumSlot(new PodiumSlotViews(imgPodiumFirstAvatar, lblPodiumFirstName, lblPodiumFirstPoints), _firstPlacePlayer);
            SetPodiumSlot(new PodiumSlotViews(imgPodiumSecondAvatar, lblPodiumSecondName, lblPodiumSecondPoints), _secondPlacePlayer);
            SetPodiumSlot(new PodiumSlotViews(imgPodiumThirdAvatar, lblPodiumThirdName, lblPodiumThirdPoints), _thirdPlacePlayer);
        }

        private void AssignRankedList(System.Collections.Generic.List<PlayerRankEntry> allPlayers)
        {
            var remainingPlayers = allPlayers.Skip(3).ToList();
            lstRankedPlayers.ItemsSource = new ObservableCollection<PlayerRankEntry>(remainingPlayers);
        }

        private void LoadEmptyLeaderboard()
        {
            _firstPlacePlayer = CreateEmptyEntry(1);
            _secondPlacePlayer = CreateEmptyEntry(2);
            _thirdPlacePlayer = CreateEmptyEntry(3);

            SetPodiumSlot(new PodiumSlotViews(imgPodiumFirstAvatar, lblPodiumFirstName, lblPodiumFirstPoints), _firstPlacePlayer);
            SetPodiumSlot(new PodiumSlotViews(imgPodiumSecondAvatar, lblPodiumSecondName, lblPodiumSecondPoints), _secondPlacePlayer);
            SetPodiumSlot(new PodiumSlotViews(imgPodiumThirdAvatar, lblPodiumThirdName, lblPodiumThirdPoints), _thirdPlacePlayer);

            lstRankedPlayers.ItemsSource = new ObservableCollection<PlayerRankEntry>();
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

        private void SetPodiumSlot(PodiumSlotViews views, PlayerRankEntry player)
        {
            try
            {
                views.AvatarImage.Source = new BitmapImage(new Uri(player.AvatarImagePath, UriKind.RelativeOrAbsolute));
            }
            catch (System.IO.IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error de I/O al cargar avatar para {player.PlayerName}. Ruta: {player.AvatarImagePath}. Detalle: {ex.Message}");
                views.AvatarImage.Source = new BitmapImage(new Uri(DefaultProfileImage, UriKind.RelativeOrAbsolute));
            }
            catch (UriFormatException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Formato de ruta inválido en BD para {player.PlayerName}. Ruta: {player.AvatarImagePath}. Detalle: {ex.Message}");
                views.AvatarImage.Source = new BitmapImage(new Uri(DefaultProfileImage, UriKind.RelativeOrAbsolute));
            }

            views.NameLabel.Text = player.PlayerName;
            views.PointsLabel.Text = FormatPointsLabel(player.Score);
        }

        private void UpdateDynamicLabels()
        {
            lblPodiumFirstPoints.Text = FormatPointsLabel(_firstPlacePlayer.Score);
            lblPodiumSecondPoints.Text = FormatPointsLabel(_secondPlacePlayer.Score);
            lblPodiumThirdPoints.Text = FormatPointsLabel(_thirdPlacePlayer.Score);

            lblUserStandingRank.Text = $"#{CurrentUserStandingRank}";
            lblUserStandingPoints.Text = FormatPointsLabel(CurrentUserStandingPoints);
        }

        private string FormatPointsLabel(int points)
        {
            string format = TryFindResource(PointsSuffixResourceKey) as string ?? DefaultPointsFormat;
            return string.Format(format, points.ToString("N0"));
        }

        private void BtnBack_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new MainMenuScreen());
        }

        private void BtnFilterAllTime_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            LoadLeaderboardData();
        }

        /// <summary>
        /// Agrupa las tres etiquetas visuales de un puesto del podio, para evitar pasar
        /// controles individuales como parámetros sueltos a <see cref="SetPodiumSlot"/>.
        /// </summary>
        private sealed class PodiumSlotViews
        {
            public PodiumSlotViews(Image avatarImage, TextBlock nameLabel, TextBlock pointsLabel)
            {
                AvatarImage = avatarImage;
                NameLabel = nameLabel;
                PointsLabel = pointsLabel;
            }

            public Image AvatarImage { get; }
            public TextBlock NameLabel { get; }
            public TextBlock PointsLabel { get; }
        }
    }
}