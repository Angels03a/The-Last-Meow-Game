using System.Globalization;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Navigation;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// ViewModel de la pantalla de instrucciones: lleva el paso actual (de 1 a 5) y regresa a la pantalla anterior.
    /// No usa el servidor. Cómo se dibuja cada paso (contenido visible, puntos, textos) lo decide la vista.
    /// </summary>
    public class HowToPlayViewModel : ViewModelBase
    {
        private const int FirstStepNumber = 1;
        private const int LastStepNumber = 5;

        private readonly INavigationManager _navigationManager;
        private readonly RelayCommand _nextCommand;
        private readonly RelayCommand _previousCommand;

        private int _currentStep = FirstStepNumber;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="HowToPlayViewModel"/>.
        /// </summary>
        /// <param name="navigationManager">Administrador de navegación.</param>
        public HowToPlayViewModel(INavigationManager navigationManager)
        {
            _navigationManager = navigationManager;

            BackCommand = new RelayCommand(ExecuteBack);
            _nextCommand = new RelayCommand(ExecuteNext);
            _previousCommand = new RelayCommand(ExecutePrevious, CanExecutePrevious);
            GoToStepCommand = new RelayCommand<string>(ExecuteGoToStep);
        }

        /// <summary>Comando para regresar a la pantalla anterior.</summary>
        public ICommand BackCommand { get; }

        /// <summary>Comando para pasar al paso siguiente; en el último paso termina y regresa.</summary>
        public ICommand NextCommand => _nextCommand;

        /// <summary>Comando para volver al paso anterior.</summary>
        public ICommand PreviousCommand => _previousCommand;

        /// <summary>Comando para ir a un paso; el parámetro es su número (de 1 a 5) como texto.</summary>
        public ICommand GoToStepCommand { get; }

        /// <summary>Número del paso que se muestra, de 1 a 5.</summary>
        public int CurrentStep
        {
            get { return _currentStep; }
            private set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    OnPropertyChanged(nameof(IsFirstStep));
                    OnPropertyChanged(nameof(IsLastStep));
                    _previousCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>Total de pasos del instructivo.</summary>
        public int StepCount
        {
            get { return LastStepNumber; }
        }

        /// <summary>Indica si se muestra el primer paso.</summary>
        public bool IsFirstStep
        {
            get { return _currentStep == FirstStepNumber; }
        }

        /// <summary>Indica si se muestra el último paso, donde «Siguiente» pasa a ser «Finalizar».</summary>
        public bool IsLastStep
        {
            get { return _currentStep == LastStepNumber; }
        }

        private void ExecuteBack()
        {
            _navigationManager.GoBack();
        }

        private void ExecuteNext()
        {
            if (IsLastStep)
            {
                _navigationManager.GoBack();
            }
            else
            {
                CurrentStep = CurrentStep + 1;
            }
        }

        private void ExecutePrevious()
        {
            CurrentStep = CurrentStep - 1;
        }

        private bool CanExecutePrevious()
        {
            bool canGoToPrevious = !IsFirstStep;
            return canGoToPrevious;
        }

        private void ExecuteGoToStep(string stepText)
        {
            int stepNumber;
            bool isNumber = int.TryParse(stepText, NumberStyles.None, CultureInfo.InvariantCulture, out stepNumber);

            if (isNumber && stepNumber >= FirstStepNumber && stepNumber <= LastStepNumber)
            {
                CurrentStep = stepNumber;
            }
        }
    }
}
