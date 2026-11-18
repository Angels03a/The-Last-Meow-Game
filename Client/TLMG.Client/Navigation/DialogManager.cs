using System.Windows;
using TLMG.Client.Models;
using TLMG.Client.Views;

namespace TLMG.Client.Navigation
{
    /// <summary>
    /// Muestra los diálogos con MessageBox y traduce las claves con LanguageManager. Es el único lugar del lado de
    /// los ViewModels que usa MessageBox.
    /// </summary>
    public class DialogManager : IDialogManager
    {
        private const string ErrorTitleKey = "Common_ErrorTitle";
        private const string MessageTitleKey = "MainWindow_Title";

        /// <inheritdoc />
        public void ShowError(string errorKey)
        {
            MessageBox.Show(
                LanguageManager.FindText(errorKey),
                LanguageManager.FindText(ErrorTitleKey),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        /// <inheritdoc />
        public void ShowMessage(string messageKey)
        {
            MessageBox.Show(
                LanguageManager.FindText(messageKey),
                LanguageManager.FindText(MessageTitleKey),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <inheritdoc />
        public DialogResult ConfirmWithResult(string messageKey, string titleKey)
        {
            GameDialogWindow window = new GameDialogWindow(GameDialogFactory.CreateConfirmation(messageKey, titleKey));
            if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                window.Owner = Application.Current.MainWindow;
            }

            window.ShowDialog();
            return window.Result;
        }

        /// <inheritdoc />
        public bool Confirm(string messageKey, string titleKey)
        {
            MessageBoxResult answer = MessageBox.Show(
                LanguageManager.FindText(messageKey),
                LanguageManager.FindText(titleKey),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            bool hasConfirmed = answer == MessageBoxResult.Yes;

            return hasConfirmed;
        }
    }
}