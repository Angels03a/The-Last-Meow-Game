using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace TLMG.Views
{
    public partial class LoadingScreen : UserControl
    {
        public event EventHandler LoadingCompleted;

        public LoadingScreen()
        {
            InitializeComponent();
            Loaded += LoadingScreen_Loaded;
        }

        private void LoadingScreen_Loaded(object eventSender, RoutedEventArgs eventArguments)
        {
            var loadingAnimation = new DoubleAnimation
            {
                From = 0,
                To = 400,
                Duration = new Duration(TimeSpan.FromSeconds(3)),
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseInOut
                }
            };

            loadingAnimation.Completed += (animationSender, animationArguments) =>
            {
                LoadingCompleted?.Invoke(this, EventArgs.Empty);
            };

            rectLoadingBar.BeginAnimation(System.Windows.Shapes.Rectangle.WidthProperty, loadingAnimation);
        }
    }
}