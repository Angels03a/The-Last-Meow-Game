using System;
using System.Windows;
using System.Windows.Controls;

namespace TLMG.Views
{
    /// <summary>
    /// Representa la pantalla del menú principal de navegación de la aplicación.
    /// </summary>
    public partial class MainMenuScreen : UserControl
    {
        private const int DefaultWinStreakCount = 3;
        private const int DefaultPointsToNextRank = 120;
        private const string WinStreakFormatResourceKey = "MainMenu_WinStreakValue";
        private const string NextRankFormatResourceKey = "MainMenu_NextRankValue";

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="MainMenuScreen"/>.
        /// </summary>
        public MainMenuScreen()
        {
            InitializeComponent();
            Loaded += MainMenuScreen_Loaded;
            Unloaded += MainMenuScreen_Unloaded;
            UpdateDynamicLabels();
        }

        private void MainMenuScreen_Loaded(object eventSender, RoutedEventArgs eventArguments)
        {
            LanguageManager.LanguageChanged += LanguageManager_LanguageChanged;
            UpdateDynamicLabels();
        }

        private void MainMenuScreen_Unloaded(object eventSender, RoutedEventArgs eventArguments)
        {
            LanguageManager.LanguageChanged -= LanguageManager_LanguageChanged;
        }

        private void LanguageManager_LanguageChanged(object eventSender, EventArgs eventArguments)
        {
            UpdateDynamicLabels();
        }

        private void UpdateDynamicLabels()
        {
            string winStreakFormat = TryFindResource(WinStreakFormatResourceKey) as string ?? "{0}";
            lblWinStreakValue.Text = string.Format(winStreakFormat, DefaultWinStreakCount);

            string nextRankFormat = TryFindResource(NextRankFormatResourceKey) as string ?? "{0}";
            lblNextRankValue.Text = string.Format(nextRankFormat, DefaultPointsToNextRank);
        }

        private void BtnNav_Click(object eventSender, RoutedEventArgs eventArguments)
        {
            // TODO: navegar a las pantallas de Lobby, Cómo Jugar, Clasificación, Amigos, Perfil
            // y Partida cuando existan.
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