using System.Windows;
using System.Windows.Controls;

namespace TLMG.Views
{
    public partial class MainMenuScreen : UserControl
    {
        public MainMenuScreen()
        {
            InitializeComponent();
        }

        private void BtnNav_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            // TODO: navegar a las pantallas de Lobby, Cómo Jugar, Clasificación, Amigos, Perfil y Partida cuando existan.
        }

        private void BtnOptions_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new OptionsScreen());
        }

        private void BtnLogOut_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            ((MainWindow)Application.Current.MainWindow).ShowScreen(new LoginScreen());
        }
    }
}