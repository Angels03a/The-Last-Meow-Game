using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Vista de las instrucciones y reglas del juego distribuidas por pasos. Conserva solo lógica visual: mostrar el
    /// contenido, el emoji, el título, el indicador y los puntos del paso actual. El número de paso y la navegación
    /// viven en <see cref="HowToPlayViewModel"/>.
    /// </summary>
    public partial class HowToPlayScreen : LocalizableScreen
    {
        private const int StepNumberOffset = 1;

        private readonly HowToPlayViewModel _viewModel;
        private readonly StepData[] _steps;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="HowToPlayScreen"/> con su ViewModel.
        /// </summary>
        /// <param name="howToPlayViewModel">ViewModel que la fábrica de pantallas crea e inyecta.</param>
        public HowToPlayScreen(HowToPlayViewModel howToPlayViewModel)
        {
            InitializeComponent();

            _viewModel = howToPlayViewModel;
            DataContext = howToPlayViewModel;

            _steps = new StepData[]
            {
                new StepData
                {
                    ContentGrid = grdStep1,
                    SidebarButton = btnStep1,
                    PaginationDot = ellDot1,
                    Emoji = "🃏",
                    TitleResourceKey = "HowToPlay_MenuStep1"
                },
                new StepData
                {
                    ContentGrid = grdStep2,
                    SidebarButton = btnStep2,
                    PaginationDot = ellDot2,
                    Emoji = "🐾",
                    TitleResourceKey = "HowToPlay_MenuStep2"
                },
                new StepData
                {
                    ContentGrid = grdStep3,
                    SidebarButton = btnStep3,
                    PaginationDot = ellDot3,
                    Emoji = "💬",
                    TitleResourceKey = "HowToPlay_MenuStep3"
                },
                new StepData
                {
                    ContentGrid = grdStep4,
                    SidebarButton = btnStep4,
                    PaginationDot = ellDot4,
                    Emoji = "🎯",
                    TitleResourceKey = "HowToPlay_MenuStep4"
                },
                new StepData
                {
                    ContentGrid = grdStep5,
                    SidebarButton = btnStep5,
                    PaginationDot = ellDot5,
                    Emoji = "⚙️",
                    TitleResourceKey = "HowToPlay_MenuStep5"
                }
            };

            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;

            UpdateStepVisibility();
        }

        /// <inheritdoc />
        protected override void UpdateDynamicLabels()
        {
            RefreshStepIndicatorText();
        }

        private void RefreshStepIndicatorText()
        {
            string format = TryFindResource("HowToPlay_StepIndicatorFormat") as string;
            if (!string.IsNullOrEmpty(format))
            {
                lblStepIndicator.Text = string.Format(format, _viewModel.CurrentStep, _viewModel.StepCount);
            }
        }

        private void UpdateStepVisibility()
        {
            for (int stepIndex = 0; stepIndex < _steps.Length; stepIndex++)
            {
                StepData step = _steps[stepIndex];
                bool isActive = (stepIndex + StepNumberOffset) == _viewModel.CurrentStep;

                step.ContentGrid.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
                step.SidebarButton.IsDefault = isActive;
                step.PaginationDot.Tag = isActive ? "Active" : string.Empty;

                if (isActive)
                {
                    lblHeaderEmoji.Text = step.Emoji;
                    lblStepTitle.SetResourceReference(TextBlock.TextProperty, step.TitleResourceKey);
                }
            }

            RefreshStepIndicatorText();

            btnPrevious.Visibility = _viewModel.IsFirstStep ? Visibility.Hidden : Visibility.Visible;
            string nextButtonResourceKey = _viewModel.IsLastStep ? "HowToPlay_FinishButton" : "HowToPlay_NextButton";
            btnNext.SetResourceReference(ContentControl.ContentProperty, nextButtonResourceKey);
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateStepVisibility();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HowToPlayViewModel.CurrentStep))
            {
                UpdateStepVisibility();
            }
        }

        /// <summary>
        /// Agrupa los controles y recursos correspondientes a un paso del instructivo.
        /// </summary>
        private sealed class StepData
        {
            /// <summary>
            /// Obtiene o establece la cuadrícula de contenido del paso.
            /// </summary>
            public Grid ContentGrid { get; set; }

            /// <summary>
            /// Obtiene o establece el botón de la barra lateral correspondiente al paso.
            /// </summary>
            public Button SidebarButton { get; set; }

            /// <summary>
            /// Obtiene o establece el indicador de paginación asociado al paso.
            /// </summary>
            public Ellipse PaginationDot { get; set; }

            /// <summary>
            /// Obtiene o establece el emoji representativo del paso.
            /// </summary>
            public string Emoji { get; set; }

            /// <summary>
            /// Obtiene o establece la clave de recurso del título del paso.
            /// </summary>
            public string TitleResourceKey { get; set; }
        }
    }
}