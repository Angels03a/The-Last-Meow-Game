using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using log4net;

using TLMG.DatabaseModel;
using TLMG.Models;

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
        private const int FirstPlaceIndex = 0;
        private const int SecondPlaceIndex = 1;
        private const int ThirdPlaceIndex = 2;
        private const int FirstPlaceRank = 1;
        private const int SecondPlaceRank = 2;
        private const int ThirdPlaceRank = 3;

        // FIXME: Estos valores deben venir del jugador actualmente logueado en la sesión.
        private const int CurrentUserStandingRank = 38;
        private const int CurrentUserStandingPoints = 1840;

        private static readonly ILog _log = LogManager.GetLogger(typeof(LeaderboardScreen));

        private PlayerRankEntry _firstPlacePlayer;
        private PlayerRankEntry _secondPlacePlayer;
        private PlayerRankEntry _thirdPlacePlayer;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LeaderboardScreen"/>.
        /// </summary>
        public LeaderboardScreen()
        {
            InitializeComponent();

            LoadLeaderboardData();
            UpdateDynamicLabels();
        }

        /// <inheritdoc />
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
                                              // FIXME: Filtrar por estado de usuario activo cuando se implemente.
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

                    List<PlayerRankEntry> rankEntries = rankedPlayersQuery.Select((data, index) => new PlayerRankEntry
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
            catch (SqlException ex)
            {
                MessageBox.Show(
                    TryFindResource("Leaderboard_ConnectionErrorMessage") as string,
                    TryFindResource("Leaderboard_ErrorTitle") as string,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                _log.Error("Fallo en la conexión de la base de datos al cargar el leaderboard.", ex);
                LoadEmptyLeaderboard();
            }
            catch (EntityException ex)
            {
                MessageBox.Show(
                    TryFindResource("Leaderboard_DataModelErrorMessage") as string,
                    TryFindResource("Leaderboard_ErrorTitle") as string,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                _log.Error("Fallo al interactuar con el modelo de datos al cargar el leaderboard.", ex);
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
            List<PlayerRankEntry> remainingPlayers = allPlayers.Skip(PodiumCapacity).ToList();
            lstRankedPlayers.ItemsSource = new ObservableCollection<PlayerRankEntry>(remainingPlayers);
        }

        private void LoadEmptyLeaderboard()
        {
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
                // FIXME: Cambiar imagen a una por defecto si no se puede cargar la imagen del jugador.
                // HACK: De momento se queda el gato de Stella por defecto.
                _log.Warn(
                    string.Format(
                        "Error de E/S al cargar avatar para {0}. Ruta: {1}.",
                        player.PlayerName,
                        player.AvatarImagePath),
                    ex);
                views.AvatarImage.Source = new BitmapImage(new Uri(DefaultProfileImage, UriKind.RelativeOrAbsolute));
            }
            catch (UriFormatException ex)
            {
                _log.Warn(
                    string.Format(
                        "Formato de ruta inválido en base de datos para {0}. Ruta: {1}.",
                        player.PlayerName,
                        player.AvatarImagePath),
                    ex);
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

        /// <summary>
        /// Agrupa las referencias de los controles de interfaz para una posición del podio.
        /// </summary>
        private sealed class PodiumSlotViews
        {
            /// <summary>
            /// Inicializa una nueva instancia de la clase <see cref="PodiumSlotViews"/>.
            /// </summary>
            /// <param name="avatarImage">Control de imagen del avatar del jugador.</param>
            /// <param name="nameLabel">Etiqueta de texto con el nombre del jugador.</param>
            /// <param name="pointsLabel">Etiqueta de texto con los puntos del jugador.</param>
            public PodiumSlotViews(Image avatarImage, TextBlock nameLabel, TextBlock pointsLabel)
            {
                AvatarImage = avatarImage;
                NameLabel = nameLabel;
                PointsLabel = pointsLabel;
            }

            /// <summary>
            /// Obtiene el control de imagen asignado al avatar.
            /// </summary>
            public Image AvatarImage { get; }

            /// <summary>
            /// Obtiene la etiqueta de texto asignada al nombre.
            /// </summary>
            public TextBlock NameLabel { get; }

            /// <summary>
            /// Obtiene la etiqueta de texto asignada a los puntos.
            /// </summary>
            public TextBlock PointsLabel { get; }
        }
    }
}