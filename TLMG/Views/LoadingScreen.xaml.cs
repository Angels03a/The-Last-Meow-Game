using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace TLMG.Views
{
    /// <summary>
    /// Pantalla de bienvenida y carga inicial con barra animada de progreso de recursos.
    /// </summary>
    public partial class LoadingScreen : UserControl
    {
        private const int LoadingAnimationDurationSeconds = 3;
        private const double InitialLoadingBarWidthPixels = 0.0;
        private const double TargetLoadingBarWidthPixels = 410.0;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LoadingScreen"/> y asocia los eventos de carga.
        /// </summary>
        public LoadingScreen()
        {
            InitializeComponent();
            Loaded += LoadingScreen_Loaded;
        }

        /// <summary>
        /// Se produce cuando la animación de la barra de carga finaliza su recorrido completo.
        /// </summary>
        public event EventHandler LoadingCompleted;

        private void LoadingScreen_Loaded(object sender, RoutedEventArgs e)
        {
            var loadingAnimation = new DoubleAnimation
            {
                From = InitialLoadingBarWidthPixels,
                To = TargetLoadingBarWidthPixels,
                Duration = new Duration(TimeSpan.FromSeconds(LoadingAnimationDurationSeconds)),
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseInOut
                }
            };

            loadingAnimation.Completed += OnLoadingAnimationCompleted;

            rectLoadingBar.BeginAnimation(
                System.Windows.Shapes.Rectangle.WidthProperty,
                loadingAnimation);
        }

        private void OnLoadingAnimationCompleted(object sender, EventArgs e)
        {
            LoadingCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}