using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    public partial class CreateAccountScreen : LocalizableScreen
    {
        public CreateAccountScreen(CreateAccountViewModel createAccountViewModel)
        {
            InitializeComponent();
            DataContext = createAccountViewModel;
        }
    }
}