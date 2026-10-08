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
    public partial class LeaderboardScreen : LocalizableScreen
    {
        private const string PointsFormatResourceKey = "Leaderboard_PointsFormat";
        private const string DefaultProfileImage = "pack://application:,,,/TLMG;component/img/default.jpg";
        private const string DefaultPlayerName = "---";
        private const int RankIndexOffset = 1;
        private const int ZeroGamesPlayed = 0;
        private const int ZeroWinRate = 0;
        private const int PercentageMultiplier = 100;
        private const int DefaultEmptyScore = 0;
        private const int DefaultEmptyWins = 0;
        private const int PodiumCapacity = 3;

        // FIXME: Estos valores deben venir del jugador actualmente logueado en la sesión.
        private const int CurrentUserStandingRank = 38;
        private const int CurrentUserStandingPoints = 1840;

        private PlayerRankEntry _firstPlacePlayer;
        private PlayerRankEntry _secondPlacePlayer;
        private PlayerRankEntry _thirdPlacePlayer;

        public LeaderboardScreen()
        {
            InitializeComponent();

            LoadLeaderboardData();
            UpdateDynamicLabels();
        }

        protected override void UpdateDynamicLabels()
        {
            lblPodiumFirstPoints.Text = FormatPointsLabel(_firstPlacePlayer.Score);
            lblPodiumSecondPoints.Text = FormatPointsLabel(_secondPlacePlayer.Score);
            lblPodiumThirdPoints.Text = FormatPointsLabel(_thirdPlacePlayer.Score);

            lblUserStandingRank.Text = $"#{CurrentUserStandingRank}";
            lblUserStandingPoints.Text = FormatPointsLabel(CurrentUserStandingPoints);
        }

        private void LoadLeaderboardData()
        {
            try
            {
                using (var database = new TLMG_DBEntities())
                {
                    var rankedPlayersQuery = (from player in database.Player
                                              join user in database.User on player.id_user equals user.id_user
                                              where user.state == "USER_ACTIVE" // FIXME: Filtrar por estado de usuario activo cuando se implemente.
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
                        Rank = index + RankIndexOffset,
                        AvatarImagePath = string.IsNullOrWhiteSpace(data.profile_image)
                             ? DefaultProfileImage
                             : (data.profile_image.StartsWith("/")
                                ? "pack://application:,,," + data.profile_image
                                : data.profile_image),
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
                MessageBox.Show(
                    TryFindResource("Leaderboard_ConnectionErrorMessage") as string,
                    TryFindResource("Leaderboard_ErrorTitle") as string,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                LoadEmptyLeaderboard();
            }
            catch (EntityException)
            {
                MessageBox.Show(
                    TryFindResource("Leaderboard_DataModelErrorMessage") as string,
                    TryFindResource("Leaderboard_ErrorTitle") as string,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
            const int FirstPlaceIndex = 0;
            const int SecondPlaceIndex = 1;
            const int ThirdPlaceIndex = 2;
            const int FirstPlaceRank = 1;
            const int SecondPlaceRank = 2;
            const int ThirdPlaceRank = 3;

            _firstPlacePlayer = allPlayers.ElementAtOrDefault(FirstPlaceIndex) ?? CreateEmptyEntry(FirstPlaceRank);
            _secondPlacePlayer = allPlayers.ElementAtOrDefault(SecondPlaceIndex) ?? CreateEmptyEntry(SecondPlaceRank);
            _thirdPlacePlayer = allPlayers.ElementAtOrDefault(ThirdPlaceIndex) ?? CreateEmptyEntry(ThirdPlaceRank);

            SetPodiumSlot(
                new PodiumSlotViews(imgPodiumFirstAvatar, lblPodiumFirstName, lblPodiumFirstPoints),
                _firstPlacePlayer);
            SetPodiumSlot(
                new PodiumSlotViews(imgPodiumSecondAvatar, lblPodiumSecondName, lblPodiumSecondPoints),
                _secondPlacePlayer);
            SetPodiumSlot(
                new PodiumSlotViews(imgPodiumThirdAvatar, lblPodiumThirdName, lblPodiumThirdPoints),
                _thirdPlacePlayer);
        }

        private void AssignRankedList(System.Collections.Generic.List<PlayerRankEntry> allPlayers)
        {
            var remainingPlayers = allPlayers.Skip(PodiumCapacity).ToList();
            lstRankedPlayers.ItemsSource = new ObservableCollection<PlayerRankEntry>(remainingPlayers);
        }

        private void LoadEmptyLeaderboard()
        {
            const int FirstPlaceRank = 1;
            const int SecondPlaceRank = 2;
            const int ThirdPlaceRank = 3;

            _firstPlacePlayer = CreateEmptyEntry(FirstPlaceRank);
            _secondPlacePlayer = CreateEmptyEntry(SecondPlaceRank);
            _thirdPlacePlayer = CreateEmptyEntry(ThirdPlaceRank);

            SetPodiumSlot(
                new PodiumSlotViews(imgPodiumFirstAvatar, lblPodiumFirstName, lblPodiumFirstPoints),
                _firstPlacePlayer);
            SetPodiumSlot(
                new PodiumSlotViews(imgPodiumSecondAvatar, lblPodiumSecondName, lblPodiumSecondPoints),
                _secondPlacePlayer);
            SetPodiumSlot(
                new PodiumSlotViews(imgPodiumThirdAvatar, lblPodiumThirdName, lblPodiumThirdPoints),
                _thirdPlacePlayer);

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
                System.Diagnostics.Debug.WriteLine(
                    // FIXME: Cambiar imagen a una por defecto si no se puede cargar la imagen del jugador. 
                    // INFO: De momento se queda el gato de Stella por defecto.
                    $"Error de I/O al cargar avatar para {player.PlayerName}. " +
                    $"Ruta: {player.AvatarImagePath}. Detalle: {ex.Message}");
                views.AvatarImage.Source = new BitmapImage(new Uri(DefaultProfileImage, UriKind.RelativeOrAbsolute));
            }
            catch (UriFormatException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Formato de ruta inválido en BD para {player.PlayerName}. " +
                    $"Ruta: {player.AvatarImagePath}. Detalle: {ex.Message}");
                views.AvatarImage.Source = new BitmapImage(new Uri(DefaultProfileImage, UriKind.RelativeOrAbsolute));
            }

            views.NameLabel.Text = player.PlayerName;
            views.PointsLabel.Text = FormatPointsLabel(player.Score);
        }

        private string FormatPointsLabel(int points)
        {
            const string DefaultPointsFormat = "{0} pts";
            string format = TryFindResource(PointsFormatResourceKey) as string ?? DefaultPointsFormat;
            return string.Format(format, points.ToString("N0"));
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new MainMenuScreen());
        }

        private void BtnFilterAllTime_Click(object sender, RoutedEventArgs e)
        {
            LoadLeaderboardData();
        }

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