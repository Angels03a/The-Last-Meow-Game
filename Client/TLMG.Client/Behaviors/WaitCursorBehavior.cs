using System.Windows;
using System.Windows.Input;

namespace TLMG.Client.Behaviors
{
    public static class WaitCursorBehavior
    {
        private const string IsBusyPropertyName = "IsBusy";

        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.RegisterAttached(
            IsBusyPropertyName,
            typeof(bool),
            typeof(WaitCursorBehavior),
            new PropertyMetadata(false, OnIsBusyChanged));

        public static bool GetIsBusy(DependencyObject element)
        {
            bool isBusy = element != null && (bool)element.GetValue(IsBusyProperty);

            return isBusy;
        }

        public static void SetIsBusy(DependencyObject element, bool isBusy)
        {
            if (element != null)
            {
                element.SetValue(IsBusyProperty, isBusy);
            }
        }

        private static void OnIsBusyChanged(DependencyObject element, DependencyPropertyChangedEventArgs eventArgs)
        {
            bool isBusy = (bool)eventArgs.NewValue;
            FrameworkElement frameworkElement = element as FrameworkElement;

            if (frameworkElement != null)
            {
                frameworkElement.Unloaded -= OnElementUnloaded;
            }

            Mouse.OverrideCursor = isBusy ? Cursors.Wait : null;

            if (isBusy && frameworkElement != null)
            {
                frameworkElement.Unloaded += OnElementUnloaded;
            }
        }

        private static void OnElementUnloaded(object sender, RoutedEventArgs eventArgs)
        {
            DependencyObject element = sender as DependencyObject;

            if (GetIsBusy(element))
            {
                Mouse.OverrideCursor = null;
            }
        }
    }
}
