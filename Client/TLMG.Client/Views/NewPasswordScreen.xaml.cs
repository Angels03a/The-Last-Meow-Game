using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TLMG.Client.ViewModels;

namespace TLMG.Client.Views
{
    public partial class NewPasswordScreen : LocalizableScreen
    {
        private const int FirstIndex = 0;
        private const string EmptyBarBrushKey = "Brush_Border";

        private readonly NewPasswordViewModel _viewModel;
        private readonly Border[] _strengthBars;

        public NewPasswordScreen(NewPasswordViewModel newPasswordViewModel)
        {
            InitializeComponent();

            _strengthBars = new Border[]
            {
                brdStrength1, brdStrength2, brdStrength3, brdStrength4, brdStrength5
            };

            _viewModel = newPasswordViewModel;
            DataContext = newPasswordViewModel;

            Loaded += OnScreenLoaded;
            Unloaded += OnScreenUnloaded;
        }

        protected override void UpdateDynamicLabels()
        {
            UpdatePasswordStrengthDisplay();
        }

        private void OnScreenLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdatePasswordStrengthDisplay();
        }

        private void OnScreenUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NewPasswordViewModel.PasswordStrengthScore))
            {
                UpdatePasswordStrengthDisplay();
            }
        }

        private void UpdatePasswordStrengthDisplay()
        {
            string brushKey = _viewModel.StrengthBrushKey;
            bool hasStrength = brushKey != null;

            Brush emptyBarBrush = (Brush)FindResource(EmptyBarBrushKey);
            Brush indicatorBrush = emptyBarBrush;

            if (hasStrength)
            {
                indicatorBrush = (Brush)FindResource(brushKey);
            }

            for (int barIndex = FirstIndex; barIndex < _strengthBars.Length; barIndex++)
            {
                if (barIndex < _viewModel.PasswordStrengthScore)
                {
                    _strengthBars[barIndex].Background = indicatorBrush;
                }
                else
                {
                    _strengthBars[barIndex].Background = emptyBarBrush;
                }
            }

            if (hasStrength)
            {
                lblStrengthLabel.Text = FindMessage(_viewModel.StrengthLabelKey);
                lblStrengthLabel.Foreground = indicatorBrush;
                lblStrengthLabel.Visibility = Visibility.Visible;
            }
            else
            {
                lblStrengthLabel.Visibility = Visibility.Collapsed;
            }
        }

        private string FindMessage(string resourceKey)
        {
            string localizedMessage = TryFindResource(resourceKey) as string ?? resourceKey;
            return localizedMessage;
        }
    }
}