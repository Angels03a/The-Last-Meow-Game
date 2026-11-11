using System;
using System.Windows;
using System.Windows.Controls;

namespace TLMG.Client.Behaviors
{
    /// <summary>
    /// Comportamiento adjunto que permite enlazar a datos la contraseña de un <see cref="PasswordBox"/>,
    /// que WPF no expone como propiedad de dependencia.
    /// </summary>
    public static class PasswordBindingBehavior
    {
        private const string BindPasswordPropertyName = "BindPassword";
        private const string BoundPasswordPropertyName = "BoundPassword";
        private const string IsUpdatingPropertyName = "IsUpdating";

        /// <summary>
        /// Propiedad adjunta que activa la sincronización entre el <see cref="PasswordBox"/> y
        /// <see cref="BoundPasswordProperty"/>.
        /// </summary>
        public static readonly DependencyProperty BindPasswordProperty = DependencyProperty.RegisterAttached(
            BindPasswordPropertyName,
            typeof(bool),
            typeof(PasswordBindingBehavior),
            new PropertyMetadata(false, OnBindPasswordChanged));

        /// <summary>
        /// Propiedad adjunta con la contraseña enlazada; es bidireccional por defecto.
        /// </summary>
        public static readonly DependencyProperty BoundPasswordProperty = DependencyProperty.RegisterAttached(
            BoundPasswordPropertyName,
            typeof(string),
            typeof(PasswordBindingBehavior),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnBoundPasswordChanged));

        private static readonly DependencyProperty _isUpdatingProperty = DependencyProperty.RegisterAttached(
            IsUpdatingPropertyName,
            typeof(bool),
            typeof(PasswordBindingBehavior),
            new PropertyMetadata(false));

        /// <summary>
        /// Obtiene si el <see cref="PasswordBox"/> sincroniza su contraseña con la propiedad enlazada.
        /// </summary>
        /// <param name="element">Elemento que lleva la propiedad adjunta.</param>
        /// <returns><c>true</c> si la sincronización está activa.</returns>
        public static bool GetBindPassword(DependencyObject element)
        {
            ValidateElement(element);

            bool bindPassword = (bool)element.GetValue(BindPasswordProperty);
            return bindPassword;
        }

        /// <summary>
        /// Activa o desactiva la sincronización de la contraseña.
        /// </summary>
        /// <param name="element">Elemento que lleva la propiedad adjunta.</param>
        /// <param name="bindPassword"><c>true</c> para activar la sincronización.</param>
        public static void SetBindPassword(DependencyObject element, bool bindPassword)
        {
            ValidateElement(element);

            element.SetValue(BindPasswordProperty, bindPassword);
        }

        /// <summary>
        /// Obtiene la contraseña enlazada.
        /// </summary>
        /// <param name="element">Elemento que lleva la propiedad adjunta.</param>
        /// <returns>Contraseña enlazada.</returns>
        public static string GetBoundPassword(DependencyObject element)
        {
            ValidateElement(element);

            string boundPassword = (string)element.GetValue(BoundPasswordProperty);
            return boundPassword;
        }

        /// <summary>
        /// Establece la contraseña enlazada.
        /// </summary>
        /// <param name="element">Elemento que lleva la propiedad adjunta.</param>
        /// <param name="boundPassword">Contraseña que se enlaza.</param>
        public static void SetBoundPassword(DependencyObject element, string boundPassword)
        {
            ValidateElement(element);

            element.SetValue(BoundPasswordProperty, boundPassword);
        }

        private static bool GetIsUpdating(DependencyObject element)
        {
            bool isUpdating = (bool)element.GetValue(_isUpdatingProperty);
            return isUpdating;
        }

        private static void SetIsUpdating(DependencyObject element, bool isUpdating)
        {
            element.SetValue(_isUpdatingProperty, isUpdating);
        }

        private static void ValidateElement(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
        }

        private static void OnBindPasswordChanged(
            DependencyObject element,
            DependencyPropertyChangedEventArgs eventArgs)
        {
            PasswordBox passwordBox = element as PasswordBox;

            if (passwordBox != null)
            {
                passwordBox.PasswordChanged -= OnPasswordBoxPasswordChanged;

                if ((bool)eventArgs.NewValue)
                {
                    passwordBox.PasswordChanged += OnPasswordBoxPasswordChanged;
                    CopyBoundPasswordToPasswordBox(passwordBox);
                }
            }
        }

        private static void OnBoundPasswordChanged(
            DependencyObject element,
            DependencyPropertyChangedEventArgs eventArgs)
        {
            PasswordBox passwordBox = element as PasswordBox;

            if (passwordBox != null && GetBindPassword(passwordBox) && !GetIsUpdating(passwordBox))
            {
                CopyBoundPasswordToPasswordBox(passwordBox);
            }
        }

        private static void CopyBoundPasswordToPasswordBox(PasswordBox passwordBox)
        {
            string boundPassword = GetBoundPassword(passwordBox) ?? string.Empty;

            if (passwordBox.Password != boundPassword)
            {
                SetIsUpdating(passwordBox, true);

                try
                {
                    passwordBox.Password = boundPassword;
                }
                finally
                {
                    SetIsUpdating(passwordBox, false);
                }
            }
        }

        private static void OnPasswordBoxPasswordChanged(object sender, RoutedEventArgs eventArgs)
        {
            PasswordBox passwordBox = sender as PasswordBox;

            if (passwordBox != null && !GetIsUpdating(passwordBox))
            {
                SetIsUpdating(passwordBox, true);

                try
                {
                    SetBoundPassword(passwordBox, passwordBox.Password);
                }
                finally
                {
                    SetIsUpdating(passwordBox, false);
                }
            }
        }
    }
}
