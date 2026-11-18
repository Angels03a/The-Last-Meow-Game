using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using TLMG.Client.Models;

namespace TLMG.Client.Views
{
    /// <summary>Muestra el último aviso sin bloquear la interfaz.</summary>
    public partial class NotificationPresenter : UserControl
    {
        public static readonly DependencyProperty NotificationProperty = DependencyProperty.Register(
            nameof(Notification), typeof(GameNotification), typeof(NotificationPresenter),
            new PropertyMetadata(null, OnNotificationChanged));

        public NotificationPresenter()
        {
            InitializeComponent();
        }

        public GameNotification Notification
        {
            get { return (GameNotification)GetValue(NotificationProperty); }
            set { SetValue(NotificationProperty, value); }
        }

        private static void OnNotificationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            NotificationPresenter presenter = (NotificationPresenter)sender;
            GameNotification notification = args.NewValue as GameNotification;
            presenter.BeginAnimation(OpacityProperty, null);
            presenter.Opacity = 0;
            if (notification != null)
            {
                presenter.TitleText.SetResourceReference(TextBlock.TextProperty, notification.TitleKey);
                presenter.BodyText.SetResourceReference(Run.TextProperty, notification.BodyKey);
                DoubleAnimation animation = new DoubleAnimation(1, 0, new Duration(notification.Duration));
                presenter.BeginAnimation(OpacityProperty, animation);
            }
        }
    }
}
