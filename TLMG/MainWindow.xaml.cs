using System.Windows;
using TLMG.Views;

namespace TLMG
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var pantallaCarga = new LoadingScreen();
            pantallaCarga.CargaCompletada += (s, e) =>
            {
                grdContenedorPrincipal.Children.Clear();
                grdContenedorPrincipal.Children.Add(new LoginScreen());
            };

            grdContenedorPrincipal.Children.Add(pantallaCarga);
        }
    }
}