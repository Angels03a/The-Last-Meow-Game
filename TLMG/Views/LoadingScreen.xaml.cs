using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace TLMG.Views
{
    public partial class LoadingScreen : UserControl
    {
        public event EventHandler CargaCompletada;

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
                To = 600,
                Duration = new Duration(TimeSpan.FromSeconds(4.5)),
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseInOut
                }
            };

            animacion.Completed += (s, args) => CargaCompletada?.Invoke(this, EventArgs.Empty);

            rectBarraCarga.BeginAnimation(WidthProperty, animacion);
        }
    }
}