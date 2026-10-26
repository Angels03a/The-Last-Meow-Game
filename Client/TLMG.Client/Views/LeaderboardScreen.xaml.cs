using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using log4net;

using TLMG.Client.Models;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Pantalla que muestra el podio y la tabla de posiciones de los jugadores (leaderboard).
    /// </summary>
    [CallbackBehavior(UseSynchronizationContext = false)]
    public partial class LeaderboardScreen : LocalizableScreen, ILeaderboardManagerCallback
    {
        private const string PointsFormatResourceKey = "Leaderboard_PointsFormat";
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
        private const int EmptyPlayerCount = 0;
        private const string ConnectionErrorKey = "Leaderboard_ConnectionErrorMessage";
        private const string ErrorTitleKey = "Leaderboard_ErrorTitle";

        // FIXME: Se reemplaza por los datos del jugador en sesión cuando el login pase al servidor. (BL-030).
        private const int CurrentUserStandingRank = 38;
        private const int CurrentUserStandingPoints = 1840;

        private static readonly ILog _log = LogManager.GetLogger(typeof(LeaderboardScreen));

        private PlayerRankEntry _firstPlacePlayer;
        private PlayerRankEntry _secondPlacePlayer;
        private PlayerRankEntry _thirdPlacePlayer;
        private LeaderboardManagerClient _leaderboardClient;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LeaderboardScreen"/>.
        /// </summary>
        public LeaderboardScreen()
        {
            InitializeComponent();
            Unloaded += OnScreenUnloaded;

            LoadEmptyLeaderboard();
            RequestLeaderboard();
            UpdateDynamicLabels();
        }

        /// <summary>
        /// Recibe del servidor la tabla de posiciones y la traslada al hilo de la interfaz.
        /// </summary>
        /// <param name="operationGetLeaderboardResult">Resultado con los jugadores o la clave de error.</param>
        public void GetLeaderboardResult(OperationGetLeaderboardResult operationGetLeaderboardResult)
        {
            // TODO: Mover el callback y el cierre del canal a la capa Comunicación. (BL-036).
            Dispatcher.BeginInvoke(new Action(() => HandleLeaderboardResult(operationGetLeaderboardResult)));
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

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            CloseLeaderboardClient();
        }

        private void RequestLeaderboard()
        {
            CloseLeaderboardClient();

            try
            {
                _leaderboardClient = new LeaderboardManagerClient(new InstanceContext(this));

                _leaderboardClient.GetLeaderboard();
            }
            catch (EndpointNotFoundException ex)
            {
                _log.Error("No se encontró el endpoint del servidor al consultar la tabla de posiciones.", ex);
                HandleConnectionFailure();
            }
            catch (TimeoutException ex)
            {
                _log.Error("El servidor tardó demasiado en recibir la consulta de la tabla de posiciones.", ex);
                HandleConnectionFailure();
            }
            catch (CommunicationException ex)
            {
                _log.Error("Error de comunicación al consultar la tabla de posiciones.", ex);
                HandleConnectionFailure();
            }
        }

        private void HandleLeaderboardResult(OperationGetLeaderboardResult operationGetLeaderboardResult)
        {
            CloseLeaderboardClient();

            if (operationGetLeaderboardResult.IsSuccess)
            {
                ShowLeaderboard(operationGetLeaderboardResult.Players);
            }
            else
            {
                ShowLeaderboardError(operationGetLeaderboardResult.ErrorKey ?? ConnectionErrorKey);
            }
        }

        private void HandleConnectionFailure()
        {
            CloseLeaderboardClient();
            ShowLeaderboardError(ConnectionErrorKey);
        }

        private void ShowLeaderboard(LeaderboardPlayer[] leaderboardPlayers)
        {
            LeaderboardPlayer[] receivedPlayers = leaderboardPlayers ?? new LeaderboardPlayer[EmptyPlayerCount];

            List<PlayerRankEntry> rankEntries =
                receivedPlayers.Select((leaderboardPlayer, index) => new PlayerRankEntry
                {
                    Rank = index + RankIndexOffset,
                    AvatarImagePath = ResolveAvatarImagePath(leaderboardPlayer.ProfileImage),
                    PlayerName = leaderboardPlayer.Nickname,
                    Score = leaderboardPlayer.TotalPoints,
                    Wins = leaderboardPlayer.TotalWins,
                    WinRatePercentage = CalculateWinRate(
                        leaderboardPlayer.TotalWins,
                        leaderboardPlayer.TotalGamesPlayed)
                }).ToList();

            AssignPodiumPlayers(rankEntries);
            AssignRankedList(rankEntries);
            UpdateDynamicLabels();
        }

        private void ShowLeaderboardError(string errorKey)
        {
            MessageBox.Show(
                TryFindResource(errorKey) as string,
                TryFindResource(ErrorTitleKey) as string,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            LoadEmptyLeaderboard();
        }

        private void CloseLeaderboardClient()
        {
            if (_leaderboardClient != null)
            {
                try
                {
                    if (_leaderboardClient.State == CommunicationState.Faulted)
                    {
                        _leaderboardClient.Abort();
                    }
                    else
                    {
                        _leaderboardClient.Close();
                    }
                }
                catch (CommunicationException ex)
                {
                    _log.Warn("No se pudo cerrar limpiamente el canal de la tabla de posiciones.", ex);
                    _leaderboardClient.Abort();
                }

                _leaderboardClient = null;
            }
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
                // FIXME: Usar imagen por defecto si falla la del jugador; se define con las imágenes. (BL-045).
                // HACK: Se queda el gato de Stella por defecto; se reemplaza al definir las imágenes. (BL-045).
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

            return resolvedPath;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new MainMenuScreen());
        }

        private void BtnFilterAllTime_Click(object sender, RoutedEventArgs e)
        {
            RequestLeaderboard();
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