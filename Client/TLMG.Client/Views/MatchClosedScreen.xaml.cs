using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>Pantalla de cierre de partida.</summary>
    public partial class MatchClosedScreen : LocalizableScreen
    {
        private const string InactivityMessageFormatKey = "MatchClosed_InactivityMessageFormat";
        private const string DefaultFormat = "{0}";
        private readonly MatchClosedViewModel _viewModel;

        /// <summary>Inicializa la vista con su ViewModel.</summary>
        /// <param name="viewModel">Estado y comandos de la pantalla.</param>
        public MatchClosedScreen(MatchClosedViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
        }

        protected override void UpdateDynamicLabels()
        {
            string inactivityMessageFormat = TryFindResource(InactivityMessageFormatKey) as string ?? DefaultFormat;
            lblInactivityMessage.Text = string.Format(inactivityMessageFormat, _viewModel.InactivityTimeoutMinutes);
        }

    }
}
