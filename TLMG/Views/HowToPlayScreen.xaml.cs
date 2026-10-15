using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace TLMG.Views
{
    /// <summary>
    /// Pantalla informativa con las instrucciones y reglas del juego distribuidas por pasos.
    /// </summary>
    public partial class HowToPlayScreen : LocalizableScreen
    {
        private const int MinStep = 1;
        private const int MaxStep = 5;
        private const int StepNumberOffset = 1;

        private int _currentStep;
        private readonly StepData[] _steps;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="HowToPlayScreen"/>.
        /// </summary>
        public HowToPlayScreen()
        {
            InitializeComponent();

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

            _currentStep = MinStep;
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
                lblStepIndicator.Text = string.Format(format, _currentStep, MaxStep);
            }
        }

        private void UpdateStepVisibility()
        {
            for (int i = 0; i < _steps.Length; i++)
            {
                StepData step = _steps[i];
                bool isActive = (i + StepNumberOffset) == _currentStep;

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

            btnPrevious.Visibility = 
                _currentStep > MinStep ? Visibility.Visible : Visibility.Hidden;
            btnNext.SetResourceReference(ContentControl.ContentProperty, 
                _currentStep == MaxStep ? "HowToPlay_BtnFinish" : "HowToPlay_BtnNext");
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep < MaxStep)
            {
                _currentStep++;
                UpdateStepVisibility();
            }
            else
            {
                ReturnToMainMenu();
            }
        }

        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep > MinStep)
            {
                _currentStep--;
                UpdateStepVisibility();
            }
        }

        private void BtnSidebarStep_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                for (int i = 0; i < _steps.Length; i++)
                {
                    if (_steps[i].SidebarButton == clickedButton)
                    {
                        _currentStep = i + StepNumberOffset;
                        break;
                    }
                }
                UpdateStepVisibility();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            ReturnToMainMenu();
        }

        private void ReturnToMainMenu()
        {
            var applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new MainMenuScreen());
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