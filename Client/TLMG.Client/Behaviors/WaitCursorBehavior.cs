using System;
using System.Windows;
using System.Windows.Input;

namespace TLMG.Client.Behaviors
{
    /// <summary>
    /// Comportamiento adjunto que muestra el cursor de espera mientras una pantalla está ocupada.
    /// </summary>
    public static class WaitCursorBehavior
    {
        private const string IsBusyPropertyName = "IsBusy";

        /// <summary>
        /// Propiedad adjunta que indica si el elemento está ocupado; en <c>true</c> muestra el cursor de espera.
        /// </summary>
        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.RegisterAttached(
            IsBusyPropertyName,
            typeof(bool),
            typeof(WaitCursorBehavior),
            new PropertyMetadata(false, OnIsBusyChanged));

        /// <summary>
        /// Obtiene si el elemento está marcado como ocupado.
        /// </summary>
        /// <param name="element">Elemento que lleva la propiedad adjunta.</param>
        /// <returns><c>true</c> si el elemento está ocupado.</returns>
        public static bool GetIsBusy(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            bool isBusy = (bool)element.GetValue(IsBusyProperty);
            return isBusy;
        }

        /// <summary>
        /// Marca el elemento como ocupado o libre.
        /// </summary>
        /// <param name="element">Elemento que lleva la propiedad adjunta.</param>
        /// <param name="isBusy"><c>true</c> para mostrar el cursor de espera.</param>
        public static void SetIsBusy(DependencyObject element, bool isBusy)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(IsBusyProperty, isBusy);
        }

        private static void OnIsBusyChanged(DependencyObject element, DependencyPropertyChangedEventArgs eventArgs)
        {
            bool isBusy = (bool)eventArgs.NewValue;
            FrameworkElement frameworkElement = element as FrameworkElement;

            if (frameworkElement != null)
            {
                frameworkElement.Unloaded -= OnElementUnloaded;
            }

            if (isBusy)
            {
                Mouse.OverrideCursor = Cursors.Wait;

                if (frameworkElement != null)
                {
                    frameworkElement.Unloaded += OnElementUnloaded;
                }
            }
            else
            {
                Mouse.OverrideCursor = null;
            }
        }

        private static void OnElementUnloaded(object sender, RoutedEventArgs eventArgs)
        {
            DependencyObject element = sender as DependencyObject;

            if (element != null && GetIsBusy(element))
            {
                Mouse.OverrideCursor = null;
            }
        }
    }
}
