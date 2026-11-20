using System.Windows;
using System.Windows.Controls;

namespace TLMG.Client.Behaviors
{
    public static class PasswordBindingBehavior
    {
        private const string BindPasswordPropertyName = "BindPassword";
        private const string BoundPasswordPropertyName = "BoundPassword";
        private const string IsUpdatingPropertyName = "IsUpdating";

        public static readonly DependencyProperty BindPasswordProperty = DependencyProperty.RegisterAttached(
            BindPasswordPropertyName,
            typeof(bool),
            typeof(PasswordBindingBehavior),
            new PropertyMetadata(false, OnBindPasswordChanged));

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

        public static bool GetBindPassword(DependencyObject element)
        {
            bool bindPassword = false;

            if (element != null)
            {
                bindPassword = (bool)element.GetValue(BindPasswordProperty);
            }

            return bindPassword;
        }

        public static void SetBindPassword(DependencyObject element, bool bindPassword)
        {
            if (element != null)
            {
                element.SetValue(BindPasswordProperty, bindPassword);
            }
        }

        public static string GetBoundPassword(DependencyObject element)
        {
            string boundPassword = string.Empty;

            if (element != null)
            {
                boundPassword = (string)element.GetValue(BoundPasswordProperty);
            }

            return boundPassword;
        }

        public static void SetBoundPassword(DependencyObject element, string boundPassword)
        {
            if (element != null)
            {
                element.SetValue(BoundPasswordProperty, boundPassword);
            }
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
                passwordBox.Password = boundPassword;
                SetIsUpdating(passwordBox, false);
            }
        }

        private static void OnPasswordBoxPasswordChanged(object sender, RoutedEventArgs eventArgs)
        {
            PasswordBox passwordBox = sender as PasswordBox;

            if (passwordBox != null && !GetIsUpdating(passwordBox))
            {
                SetIsUpdating(passwordBox, true);
                SetBoundPassword(passwordBox, passwordBox.Password);
                SetIsUpdating(passwordBox, false);
            }
        }
    }
}