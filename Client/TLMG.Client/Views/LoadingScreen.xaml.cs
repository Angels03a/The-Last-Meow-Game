using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Pantalla de bienvenida con una barra animada que señala el fin de la presentación inicial.
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
            Loaded += OnScreenLoaded;
        }

        /// <summary>
        /// Se produce cuando la animación de la barra de carga finaliza su recorrido completo.
        /// </summary>
        public event EventHandler LoadingCompleted;

        private void OnScreenLoaded(object sender, RoutedEventArgs e)
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