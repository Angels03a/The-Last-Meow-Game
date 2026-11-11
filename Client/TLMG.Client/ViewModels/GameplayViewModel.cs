using System;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Estado y comandos de la pantalla de partida.
    /// </summary>
    public class GameplayViewModel : ViewModelBase
    {
        private const int CurrentRoundNumberValue = 3;
        private const int InitialBidAmount = 4;
        private const int MinimumBidAmount = 1;
        private const int MaximumBidAmount = 12;
        private const int BidStep = 1;

        private const string PhaseBiddingKey = "Gameplay_PhaseBiddingLabel";

        private readonly RelayCommand _decreaseBidCommand;
        private readonly RelayCommand _increaseBidCommand;
        private int _bidAmount;
        private int _currentBidAmount;
        private string _selectedPlayerName;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="GameplayViewModel"/>.
        /// </summary>
        public GameplayViewModel()
        {
            _bidAmount = InitialBidAmount;
            _currentBidAmount = InitialBidAmount;
            _selectedPlayerName = string.Empty;

            _decreaseBidCommand = new RelayCommand(ExecuteDecreaseBid, CanExecuteDecreaseBid);
            _increaseBidCommand = new RelayCommand(ExecuteIncreaseBid, CanExecuteIncreaseBid);
            ExitCommand = new RelayCommand(ExecuteExit);
            ReportPlayerCommand = new RelayCommand(ExecuteReportPlayer);
            KickPlayerCommand = new RelayCommand(ExecuteKickPlayer);
            BlockPlayerCommand = new RelayCommand(ExecuteBlockPlayer);
            DemoRevealCardsCommand = new RelayCommand(ExecuteDemoRevealCards);
            DemoGameOverCommand = new RelayCommand(ExecuteDemoGameOver);
            DemoInactivityCommand = new RelayCommand(ExecuteDemoInactivity);
        }

        /// <summary>
        /// Se produce cuando el jugador solicita salir de la partida.
        /// </summary>
        public event EventHandler ExitRequested;

        /// <summary>
        /// Se produce cuando se solicita reportar al jugador seleccionado.
        /// </summary>
        public event EventHandler ReportRequested;

        /// <summary>
        /// Se produce cuando se solicita bloquear al jugador seleccionado.
        /// </summary>
        public event EventHandler BlockRequested;

        /// <summary>
        /// Se produce cuando se solicita iniciar la votación de expulsión del jugador seleccionado.
        /// </summary>
        public event EventHandler KickVoteRequested;

        /// <summary>
        /// Se produce cuando se solicita la pantalla demo de resultado de ronda.
        /// </summary>
        public event EventHandler DemoRoundResultRequested;

        /// <summary>
        /// Se produce cuando se solicita la pantalla demo de fin de partida.
        /// </summary>
        public event EventHandler DemoGameOverRequested;

        /// <summary>
        /// Se produce cuando se solicita la pantalla demo de partida cerrada.
        /// </summary>
        public event EventHandler DemoInactivityRequested;

        /// <summary>Comando que reduce la apuesta seleccionada.</summary>
        public ICommand DecreaseBidCommand => _decreaseBidCommand;

        /// <summary>Comando que aumenta la apuesta seleccionada.</summary>
        public ICommand IncreaseBidCommand => _increaseBidCommand;

        /// <summary>Comando que solicita salir de la partida.</summary>
        public ICommand ExitCommand { get; }

        /// <summary>Comando del menú contextual para reportar.</summary>
        public ICommand ReportPlayerCommand { get; }

        /// <summary>Comando del menú contextual para votar la expulsión.</summary>
        public ICommand KickPlayerCommand { get; }

        /// <summary>Comando del menú contextual para bloquear.</summary>
        public ICommand BlockPlayerCommand { get; }

        /// <summary>Comando demo que abre el resultado de ronda.</summary>
        public ICommand DemoRevealCardsCommand { get; }

        /// <summary>Comando demo que abre el fin de partida.</summary>
        public ICommand DemoGameOverCommand { get; }

        /// <summary>Comando demo que abre la partida cerrada por inactividad.</summary>
        public ICommand DemoInactivityCommand { get; }

        /// <summary>
        /// Número de la ronda en curso.
        /// </summary>
        public int CurrentRoundNumber
        {
            get { return CurrentRoundNumberValue; }
        }

        /// <summary>
        /// Clave de recurso del nombre de la fase actual.
        /// </summary>
        public string PhaseLabelKey
        {
            get { return PhaseBiddingKey; }
        }

        /// <summary>
        /// Apuesta que el jugador local tiene seleccionada.
        /// </summary>
        public int BidAmount
        {
            get { return _bidAmount; }
            private set
            {
                if (SetProperty(ref _bidAmount, value))
                {
                    _decreaseBidCommand.NotifyCanExecuteChanged();
                    _increaseBidCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Apuesta vigente en la mesa.
        /// </summary>
        public int CurrentBidAmount
        {
            get { return _currentBidAmount; }
            private set { SetProperty(ref _currentBidAmount, value); }
        }

        /// <summary>
        /// Nombre del jugador sobre el que se abrió el menú contextual.
        /// </summary>
        public string SelectedPlayerName
        {
            get { return _selectedPlayerName; }
            set
            {
                if (SetProperty(ref _selectedPlayerName, value))
                {
                    OnPropertyChanged(nameof(ContextMenuTitle));
                }
            }
        }

        /// <summary>
        /// Título del menú contextual, con el nombre del jugador en mayúsculas.
        /// </summary>
        public string ContextMenuTitle
        {
            get { return SelectedPlayerName.ToUpperInvariant(); }
        }

        private void ExecuteDecreaseBid()
        {
            BidAmount -= BidStep;
        }

        private bool CanExecuteDecreaseBid()
        {
            bool canDecrease = BidAmount > MinimumBidAmount;
            return canDecrease;
        }

        private void ExecuteIncreaseBid()
        {
            BidAmount += BidStep;
        }

        private bool CanExecuteIncreaseBid()
        {
            bool canIncrease = BidAmount < MaximumBidAmount;
            return canIncrease;
        }

        private void ExecuteExit()
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteReportPlayer()
        {
            ReportRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteKickPlayer()
        {
            KickVoteRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteBlockPlayer()
        {
            BlockRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteDemoRevealCards()
        {
            DemoRoundResultRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteDemoGameOver()
        {
            DemoGameOverRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteDemoInactivity()
        {
            DemoInactivityRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}