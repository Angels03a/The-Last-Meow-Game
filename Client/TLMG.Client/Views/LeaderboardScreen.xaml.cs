using System;
using System.ServiceModel;
using System.Windows;
using log4net;
using TLMG.Client.UsersManagerReference;
using TLMG.Client.ViewModels;
using TLMG.Client.Navigation;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Pantalla que muestra el podio y la tabla de posiciones de los jugadores (leaderboard).
    /// </summary>
    [CallbackBehavior(UseSynchronizationContext = false)]
    public partial class LeaderboardScreen : LocalizableScreen, ILeaderboardManagerCallback
    {
        private const string PointsFormatResourceKey = "Leaderboard_PointsFormat";
        private const string ConnectionErrorKey = "Leaderboard_ConnectionErrorMessage";
        private const string ErrorTitleKey = "Leaderboard_ErrorTitle";

        private static readonly ILog _log = LogManager.GetLogger(typeof(LeaderboardScreen));

        private readonly LeaderboardViewModel _viewModel;
        private LeaderboardManagerClient _leaderboardClient;
        private OperationGetLeaderboardResult _pendingLeaderboardResult;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LeaderboardScreen"/>.
        /// </summary>
        public LeaderboardScreen()
        {
            InitializeComponent();

            _viewModel = new LeaderboardViewModel();
            DataContext = _viewModel;

            _viewModel.RefreshRequested += OnRefreshRequested;
            _viewModel.BackRequested += OnBackRequested;

            Unloaded += OnScreenUnloaded;

            RequestLeaderboard();
        }

        /// <summary>
        /// Recibe del servidor la tabla de posiciones y la traslada al hilo de la interfaz sin utilizar lambdas.
        /// </summary>
        /// <param name="operationGetLeaderboardResult">Resultado con los jugadores o la clave de error.</param>
        public void GetLeaderboardResult(OperationGetLeaderboardResult operationGetLeaderboardResult)
        {
            _pendingLeaderboardResult = operationGetLeaderboardResult;
            Dispatcher.BeginInvoke(new Action(ProcessPendingLeaderboardResult));
        }

        protected override void UpdateDynamicLabels()
        {
            string pointsFormat = TryFindResource(PointsFormatResourceKey) as string;
            _viewModel.UpdateLocalizedFormats(pointsFormat);
        }

        private void ProcessPendingLeaderboardResult()
        {
            OperationGetLeaderboardResult operationGetLeaderboardResult = _pendingLeaderboardResult;

            if (operationGetLeaderboardResult != null)
            {
                ShowLeaderboardResult(operationGetLeaderboardResult);
            }
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            CloseLeaderboardClient();
        }

        private void OnRefreshRequested(object sender, EventArgs e)
        {
            RequestLeaderboard();
        }

        private void OnBackRequested(object sender, EventArgs e)
        {
            ((App)Application.Current).Navigation.NavigateTo(ScreenId.MainMenu);
        }

        private void RequestLeaderboard()
        {
            CloseLeaderboardClient();

            try
            {
                _leaderboardClient = new LeaderboardManagerClient(new InstanceContext(this));
                _leaderboardClient.GetLeaderboard();
            }
            catch (EndpointNotFoundException endpointNotFoundException)
            {
                _log.Error("No se encontró el endpoint del servidor al consultar la tabla de posiciones.", 
                    endpointNotFoundException);
                ShowConnectionError();
            }
            catch (TimeoutException timeoutException)
            {
                _log.Error("El servidor tardó demasiado en recibir la consulta de la tabla de posiciones.", 
                    timeoutException);
                ShowConnectionError();
            }
            catch (CommunicationException communicationException)
            {
                _log.Error("Error de comunicación al consultar la tabla de posiciones.", 
                    communicationException);
                ShowConnectionError();
            }
        }

        private void ShowLeaderboardResult(OperationGetLeaderboardResult operationGetLeaderboardResult)
        {
            CloseLeaderboardClient();

            if (operationGetLeaderboardResult.IsSuccess)
            {
                string pointsFormat = TryFindResource(PointsFormatResourceKey) as string;
                _viewModel.ProcessLeaderboardData(operationGetLeaderboardResult.Players, pointsFormat);
            }
            else
            {
                ShowLeaderboardError(operationGetLeaderboardResult.ErrorKey ?? ConnectionErrorKey);
            }
        }

        private void ShowConnectionError()
        {
            CloseLeaderboardClient();
            ShowLeaderboardError(ConnectionErrorKey);
        }

        private void ShowLeaderboardError(string errorKey)
        {
            MessageBox.Show(
                TryFindResource(errorKey) as string,
                TryFindResource(ErrorTitleKey) as string,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            _viewModel.LoadEmptyLeaderboard();
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
                catch (CommunicationException communicationException)
                {
                    _log.Warn("No se pudo cerrar limpiamente el canal de la tabla de posiciones.", communicationException);
                    _leaderboardClient.Abort();
                }

                _leaderboardClient = null;
            }
        }
    }
}