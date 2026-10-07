using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using TLMG.Views;

namespace TLMG
{
    public partial class MainWindow : Window
    {
        private readonly Stack<UserControl> _navigationHistory;
        private UserControl _currentScreen;

        public MainWindow()
        {
            InitializeComponent();
            _navigationHistory = new Stack<UserControl>();
            LoadingScreen loadingScreen = new LoadingScreen();
            loadingScreen.LoadingCompleted += (eventSender, eventArguments) => ShowScreen(new LoginScreen(), false);
            ShowScreen(loadingScreen, false);
        }

        public void ShowScreen(UserControl newScreen, bool saveInHistory = true)
        {
            if (saveInHistory && _currentScreen != null)
            {
                _navigationHistory.Push(_currentScreen);
            }

            _currentScreen = newScreen;
            grdMainContainer.Children.Clear();
            grdMainContainer.Children.Add(_currentScreen);
        }

        public void GoBack()
        {
            if (_navigationHistory.Count > 0)
            {
                _currentScreen = _navigationHistory.Pop();
                grdMainContainer.Children.Clear();
                grdMainContainer.Children.Add(_currentScreen);
            }
            else
            {
                ShowScreen(new MainMenuScreen(), false);
            }
        }
        public void ClearHistory()
        {
            _navigationHistory.Clear();
        }
    }
}