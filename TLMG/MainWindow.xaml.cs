using System.Windows;
using System.Windows.Controls;
using TLMG.Views;

namespace TLMG
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var loadingScreen = new LoadingScreen();
            loadingScreen.LoadingCompleted += (s, e) => ShowScreen(new LoginScreen());
            ShowScreen(loadingScreen);
        }

        public void ShowScreen(UserControl screen)
        {
            grdMainContainer.Children.Clear();
            grdMainContainer.Children.Add(screen);
        }
    }
}