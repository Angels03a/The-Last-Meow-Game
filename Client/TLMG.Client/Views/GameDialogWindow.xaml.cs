using System.Windows;
using TLMG.Client.Models;

namespace TLMG.Client.Views
{
    /// <summary>Presenta una confirmación modal sin decidir su efecto.</summary>
    public partial class GameDialogWindow : Window
    {
        public GameDialogWindow(GameDialog dialog)
        {
            InitializeComponent();
            SetResourceReference(TitleProperty, dialog.TitleKey);
            MessageText.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, dialog.MessageKey);
            Result = Models.DialogResult.Cancel;
        }

        public Models.DialogResult Result { get; private set; }

        private void OnYesClick(object sender, RoutedEventArgs eventArgs)
        {
            Result = Models.DialogResult.Yes;
            Close();
        }

        private void OnNoClick(object sender, RoutedEventArgs eventArgs)
        {
            Result = Models.DialogResult.No;
            Close();
        }
    }
}
