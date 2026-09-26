using System.Windows;
using System.Windows.Controls;

using TLMG.Views;

namespace TLMG
{
    /// <summary>
    /// Ventana principal que gestiona la navegación entre pantallas mediante su contenedor central.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="MainWindow"/> y carga la pantalla inicial de carga.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            LoadingScreen loadingScreen = new LoadingScreen();
            loadingScreen.LoadingCompleted += (eventSender, eventArguments) => ShowScreen(new LoginScreen());
            ShowScreen(loadingScreen);
        }

        /// <summary>
        /// Muestra el control de usuario especificado dentro del contenedor principal de la ventana.
        /// </summary>
        /// <param name="screen">Control de usuario correspondiente a la pantalla a desplegar.</param>
        public void ShowScreen(UserControl screen)
        {
            grdMainContainer.Children.Clear();
            grdMainContainer.Children.Add(screen);
        }
    }
}