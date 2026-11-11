using System.Windows;

namespace TLMG.Client.Navigation
{
    /// <summary>
    /// Muestra los diálogos con MessageBox y traduce las claves con LanguageManager. Es el único lugar del lado de
    /// los ViewModels que usa MessageBox.
    /// </summary>
    public class DialogManager : IDialogManager
    {
        private const string ErrorTitleKey = "Common_ErrorTitle";

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