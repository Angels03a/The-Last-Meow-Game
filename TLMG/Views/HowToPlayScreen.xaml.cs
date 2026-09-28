using System.Windows;
using System.Windows.Controls;

namespace TLMG.Views
{
    public partial class HowToPlayScreen : LocalizableScreen
    {
        private const int Step1 = 1;
        private const int Step2 = 2;
        private const int Step3 = 3;
        private const int Step4 = 4;
        private const int Step5 = 5;
        private const int MinStep = Step1;
        private const int MaxStep = Step5;

        private int _currentStep;

        public HowToPlayScreen()
        {
            InitializeComponent();
            _currentStep = MinStep;
            UpdateStepVisibility();
        }

        protected override void UpdateDynamicLabels()
        {
            RefreshStepIndicatorText();
        }

        private void RefreshStepIndicatorText()
        {
            string format = TryFindResource("HowToPlay_StepIndicatorFormat") as string ?? "STEP {0} OF {1}";
            lblStepIndicator.Text = string.Format(format, _currentStep, MaxStep);
        }

        private void UpdateStepVisibility()
        {
            grdStep1.Visibility = Visibility.Collapsed;
            grdStep2.Visibility = Visibility.Collapsed;
            grdStep3.Visibility = Visibility.Collapsed;
            grdStep4.Visibility = Visibility.Collapsed;
            grdStep5.Visibility = Visibility.Collapsed;

            btnStep1.IsDefault = false;
            btnStep2.IsDefault = false;
            btnStep3.IsDefault = false;
            btnStep4.IsDefault = false;
            btnStep5.IsDefault = false;

            switch (_currentStep)
            {
                case Step1:
                    grdStep1.Visibility = Visibility.Visible;
                    btnStep1.IsDefault = true;
                    UpdateHeaderContent("🃏", "HowToPlay_MenuStep1");
                    break;
                case Step2:
                    grdStep2.Visibility = Visibility.Visible;
                    btnStep2.IsDefault = true;
                    UpdateHeaderContent("🐾", "HowToPlay_MenuStep2");
                    break;
                case Step3:
                    grdStep3.Visibility = Visibility.Visible;
                    btnStep3.IsDefault = true;
                    UpdateHeaderContent("💬", "HowToPlay_MenuStep3");
                    break;
                case Step4:
                    grdStep4.Visibility = Visibility.Visible;
                    btnStep4.IsDefault = true;
                    UpdateHeaderContent("🎯", "HowToPlay_MenuStep4");
                    break;
                case Step5:
                    grdStep5.Visibility = Visibility.Visible;
                    btnStep5.IsDefault = true;
                    UpdateHeaderContent("⚙️", "HowToPlay_MenuStep5");
                    break;
            }

            UpdatePaginationControls();
        }

        private void UpdateHeaderContent(string emoji, string titleResourceKey)
        {
            lblHeaderEmoji.Text = emoji;
            RefreshStepIndicatorText();
            lblStepTitle.SetResourceReference(TextBlock.TextProperty, titleResourceKey);
        }

        private void UpdatePaginationControls()
        {
            shpDot1.Tag = _currentStep == Step1 ? "Active" : string.Empty;
            shpDot2.Tag = _currentStep == Step2 ? "Active" : string.Empty;
            shpDot3.Tag = _currentStep == Step3 ? "Active" : string.Empty;
            shpDot4.Tag = _currentStep == Step4 ? "Active" : string.Empty;
            shpDot5.Tag = _currentStep == Step5 ? "Active" : string.Empty;

            btnPrevious.Visibility = _currentStep > MinStep ? Visibility.Visible : Visibility.Hidden;

            if (_currentStep == MaxStep)
            {
                btnNext.SetResourceReference(ContentControl.ContentProperty, "HowToPlay_BtnFinish");
            }
            else
            {
                btnNext.SetResourceReference(ContentControl.ContentProperty, "HowToPlay_BtnNext");
            }
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
            Button clickedButton = sender as Button;

            if (clickedButton == btnStep1)
            {
                _currentStep = Step1;
            }
            else if (clickedButton == btnStep2)
            {
                _currentStep = Step2;
            }
            else if (clickedButton == btnStep3)
            {
                _currentStep = Step3;
            }
            else if (clickedButton == btnStep4)
            {
                _currentStep = Step4;
            }
            else if (clickedButton == btnStep5)
            {
                _currentStep = Step5;
            }

            UpdateStepVisibility();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            ReturnToMainMenu();
        }

        private void ReturnToMainMenu()
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new MainMenuScreen());
        }
    }
}