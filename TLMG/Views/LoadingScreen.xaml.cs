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

        public LoadingScreen()
        {
            InitializeComponent();
            Loaded += LoadingScreen_Loaded;
        }

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

            loadingAnimation.Completed += (animationSender, animationArguments) =>
            {
                LoadingCompleted?.Invoke(this, EventArgs.Empty);
            };

            rectLoadingBar.BeginAnimation(
                System.Windows.Shapes.Rectangle.WidthProperty,
                loadingAnimation);
        }
    }
}