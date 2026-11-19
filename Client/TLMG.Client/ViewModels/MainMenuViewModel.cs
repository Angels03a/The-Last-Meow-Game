using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Communication;
using TLMG.Client.Navigation;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// ViewModel del menú principal: muestra el nickname del jugador en sesión y navega a las demás pantallas con
    /// el administrador de navegación. Cerrar sesión libera la conexión con el servidor y regresa al inicio de
    /// sesión.
    /// </summary>
    public class MainMenuViewModel : ViewModelBase
    {
        private readonly IAccountsConnection _accountsConnection;
        private readonly INavigationManager _navigationManager;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="MainMenuViewModel"/>.
        /// </summary>
        /// <param name="accountsConnection">Área de cuentas de la conexión con el servidor.</param>
        /// <param name="navigationManager">Administrador de navegación.</param>
        public MainMenuViewModel(IAccountsConnection accountsConnection, INavigationManager navigationManager)
        {
            _accountsConnection = accountsConnection;
            _navigationManager = navigationManager;

            // TODO: Mostrar lo que se decida para el invitado, que no tiene nickname; hoy queda vacío. (BL-089).
            PlayerNickname = _accountsConnection.PlayerNickname ?? string.Empty;

            NavigateToLobbyCommand = new RelayCommand(ExecuteNavigateToLobby);
            NavigateToFriendsCommand = new RelayCommand(ExecuteNavigateToFriends);
            NavigateToProfileCommand = new RelayCommand(ExecuteNavigateToProfile);
            NavigateToHowToPlayCommand = new RelayCommand(ExecuteNavigateToHowToPlay);
            NavigateToLeaderboardCommand = new RelayCommand(ExecuteNavigateToLeaderboard);
            NavigateToOptionsCommand = new RelayCommand(ExecuteNavigateToOptions);
            LogOutCommand = new RelayCommand(ExecuteLogOut);
        }

        /// <summary>Comando para navegar al Lobby.</summary>
        public ICommand NavigateToLobbyCommand { get; }

        /// <summary>Comando para navegar a la lista de amigos.</summary>
        public ICommand NavigateToFriendsCommand { get; }

        /// <summary>Comando para navegar al perfil.</summary>
        public ICommand NavigateToProfileCommand { get; }

        /// <summary>Comando para navegar a las instrucciones.</summary>
        public ICommand NavigateToHowToPlayCommand { get; }

        /// <summary>Comando para navegar al Leaderboard.</summary>
        public ICommand NavigateToLeaderboardCommand { get; }

        /// <summary>Comando para navegar a las opciones.</summary>
        public ICommand NavigateToOptionsCommand { get; }

        /// <summary>Comando para cerrar sesión.</summary>
        public ICommand LogOutCommand { get; }

        /// <summary>Nickname del jugador en sesión; vacío para un invitado.</summary>
        public string PlayerNickname { get; }

        private void ExecuteNavigateToLobby()
        {
            _navigationManager.NavigateTo(ScreenId.Lobby);
        }

        private void ExecuteNavigateToFriends()
        {
            _navigationManager.NavigateTo(ScreenId.Friends);
        }

        private void ExecuteNavigateToProfile()
        {
            _navigationManager.NavigateTo(ScreenId.Profile);
        }

        private void ExecuteNavigateToHowToPlay()
        {
            _navigationManager.NavigateTo(ScreenId.HowToPlay);
        }

        private void ExecuteNavigateToLeaderboard()
        {
            _navigationManager.NavigateTo(ScreenId.Leaderboard);
        }

        private void ExecuteNavigateToOptions()
        {
            _navigationManager.NavigateTo(ScreenId.Options);
        }

        private void ExecuteLogOut()
        {
            _accountsConnection.LogOut();
            _navigationManager.NavigateToWithoutHistory(ScreenId.Login);
            _navigationManager.ClearHistory();
        }
    }
}
