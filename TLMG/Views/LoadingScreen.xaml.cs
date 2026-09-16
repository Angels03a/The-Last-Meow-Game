using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace TLMG.Views
{
    public partial class LoadingScreen : UserControl
    {
        public LoadingScreen()
        {
            InitializeComponent();
            Loaded += LoadingScreen_Loaded;
        }

        private void LoadingScreen_Loaded(object sender, RoutedEventArgs e)
        {
            var animacion = new DoubleAnimation
            {
                From = 0,
                To = 400,
                Duration = new Duration(TimeSpan.FromSeconds(3.5)),
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseInOut
                }
            };

            rectBarraCarga.BeginAnimation(WidthProperty, animacion);
        }
    }
}