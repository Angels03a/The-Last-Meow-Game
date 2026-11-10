using System;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Gestiona los comandos de navegación y el estado de la pantalla del menú principal.
    /// </summary>
    public class MainMenuViewModel : ViewModelBase
    {
        private string _playerNickname;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="MainMenuViewModel"/>.
        /// </summary>
        public MainMenuViewModel()
        {
            NavigateToLobbyCommand = new RelayCommand(ExecuteNavigateToLobby);
            NavigateToFriendsCommand = new RelayCommand(ExecuteNavigateToFriends);
            NavigateToProfileCommand = new RelayCommand(ExecuteNavigateToProfile);
            NavigateToHowToPlayCommand = new RelayCommand(ExecuteNavigateToHowToPlay);
            NavigateToLeaderboardCommand = new RelayCommand(ExecuteNavigateToLeaderboard);
            NavigateToOptionsCommand = new RelayCommand(ExecuteNavigateToOptions);
            LogOutCommand = new RelayCommand(ExecuteLogOut);
        }

        /// <summary>
        /// Se produce cuando se solicita navegar a la sala (Lobby).
        /// </summary>
        public event EventHandler LobbyRequested;

        /// <summary>
        /// Se produce cuando se solicita navegar a la lista de amigos.
        /// </summary>
        public event EventHandler FriendsRequested;

        /// <summary>
        /// Se produce cuando se solicita navegar al perfil del jugador.
        /// </summary>
        public event EventHandler ProfileRequested;

        /// <summary>
        /// Se produce cuando se solicita navegar a las instrucciones de juego.
        /// </summary>
        public event EventHandler HowToPlayRequested;

        /// <summary>
        /// Se produce cuando se solicita navegar a la tabla de posiciones (Leaderboard).
        /// </summary>
        public event EventHandler LeaderboardRequested;

        /// <summary>
        /// Se produce cuando se solicita navegar a las opciones.
        /// </summary>
        public event EventHandler OptionsRequested;

        /// <summary>
        /// Se produce cuando se solicita cerrar sesión.
        /// </summary>
        public event EventHandler LogOutRequested;

        /// <summary>
        /// Comando para navegar al Lobby.
        /// </summary>
        public RelayCommand NavigateToLobbyCommand { get; }

        /// <summary>
        /// Comando para navegar a la lista de amigos.
        /// </summary>
        public RelayCommand NavigateToFriendsCommand { get; }

        /// <summary>
        /// Comando para navegar al perfil.
        /// </summary>
        public RelayCommand NavigateToProfileCommand { get; }

        /// <summary>
        /// Comando para navegar a las instrucciones.
        /// </summary>
        public RelayCommand NavigateToHowToPlayCommand { get; }

        /// <summary>
        /// Comando para navegar al Leaderboard.
        /// </summary>
        public RelayCommand NavigateToLeaderboardCommand { get; }

        /// <summary>
        /// Comando para navegar a las opciones.
        /// </summary>
        public RelayCommand NavigateToOptionsCommand { get; }

        /// <summary>
        /// Comando para cerrar sesión.
        /// </summary>
        public RelayCommand LogOutCommand { get; }

        /// <summary>
        /// Nickname del jugador actual.
        /// </summary>
        public string PlayerNickname
        {
            get { return _playerNickname; }
            set { SetProperty(ref _playerNickname, value); }
        }

        private void ExecuteNavigateToLobby()
        {
            LobbyRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteNavigateToFriends()
        {
            FriendsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteNavigateToProfile()
        {
            ProfileRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteNavigateToHowToPlay()
        {
            HowToPlayRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteNavigateToLeaderboard()
        {
            LeaderboardRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteNavigateToOptions()
        {
            OptionsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteLogOut()
        {
            LogOutRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}