using System;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Estado, validación y comandos de la pantalla de nueva contraseña.
    /// </summary>
    public class NewPasswordViewModel : ViewModelBase
    {
        private const int VeryWeakScore = 1;
        private const int WeakScore = 2;
        private const int FairScore = 3;
        private const int GoodScore = 4;
        private const int StrongScore = 5;

        private const string WeakBrushKey = "Brush_StrengthWeak";
        private const string FairBrushKey = "Brush_StrengthFair";
        private const string StrongBrushKey = "Brush_StrengthStrong";

        private const string VeryWeakLabelKey = "NewPassword_StrengthVeryWeak";
        private const string WeakLabelKey = "NewPassword_StrengthWeak";
        private const string FairLabelKey = "NewPassword_StrengthFair";
        private const string GoodLabelKey = "NewPassword_StrengthGood";
        private const string StrongLabelKey = "NewPassword_StrengthStrong";

        private const string IncompleteFieldsErrorKey = "NewPassword_IncompleteFieldsError";
        private const string PolicyErrorKey = "NewPassword_PolicyError";
        private const string MismatchErrorKey = "NewPassword_MismatchError";

        private string _newPassword;
        private string _confirmPassword;
        private int _passwordStrengthScore;
        private string _strengthLabelKey;
        private string _strengthBrushKey;
        private string _passwordErrorKey;
        private string _confirmPasswordErrorKey;
        private string _formErrorKey;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="NewPasswordViewModel"/>.
        /// </summary>
        public NewPasswordViewModel()
        {
            SetPasswordCommand = new RelayCommand(ExecuteSetPassword);
            BackCommand = new RelayCommand(ExecuteBack);
        }

        /// <summary>
        /// Se produce cuando la nueva contraseña es válida y la vista debe confirmar y redirigir.
        /// </summary>
        public event EventHandler PasswordAccepted;

        /// <summary>
        /// Se produce cuando el jugador solicita volver a la pantalla de contraseña olvidada.
        /// </summary>
        public event EventHandler BackRequested;

        /// <summary>
        /// Se produce cuando hay claves de error que la vista debe mostrar.
        /// </summary>
        public event EventHandler ValidationFailed;

        /// <summary>
        /// Se produce cuando cambia la fuerza de la contraseña y la vista debe redibujar el indicador.
        /// </summary>
        public event EventHandler PasswordStrengthChanged;

        /// <summary>
        /// Comando que valida las contraseñas y solicita guardar la nueva.
        /// </summary>
        public RelayCommand SetPasswordCommand { get; }

        /// <summary>
        /// Comando que solicita volver a la pantalla de contraseña olvidada.
        /// </summary>
        public RelayCommand BackCommand { get; }

        /// <summary>
        /// Nueva contraseña copiada desde el PasswordBox de la vista.
        /// </summary>
        public string NewPassword
        {
            get { return _newPassword; }
            set
            {
                if (SetProperty(ref _newPassword, value))
                {
                    UpdatePasswordStrength();
                }
            }
        }

        /// <summary>
        /// Confirmación de la contraseña copiada desde el PasswordBox de la vista.
        /// </summary>
        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set { SetProperty(ref _confirmPassword, value); }
        }

        /// <summary>
        /// Fuerza de la contraseña actual, de 0 a 5.
        /// </summary>
        public int PasswordStrengthScore
        {
            get { return _passwordStrengthScore; }
            private set { SetProperty(ref _passwordStrengthScore, value); }
        }

        /// <summary>
        /// Clave de recurso de la etiqueta de fuerza; null si la contraseña está vacía.
        /// </summary>
        public string StrengthLabelKey
        {
            get { return _strengthLabelKey; }
            private set { SetProperty(ref _strengthLabelKey, value); }
        }

        /// <summary>
        /// Clave de recurso del pincel del indicador de fuerza; null si la contraseña está vacía.
        /// </summary>
        public string StrengthBrushKey
        {
            get { return _strengthBrushKey; }
            private set { SetProperty(ref _strengthBrushKey, value); }
        }

        /// <summary>
        /// Clave de error de la política de contraseña; null si no hay error.
        /// </summary>
        public string PasswordErrorKey
        {
            get { return _passwordErrorKey; }
            private set { SetProperty(ref _passwordErrorKey, value); }
        }

        /// <summary>
        /// Clave de error de la coincidencia de contraseñas; null si no hay error.
        /// </summary>
        public string ConfirmPasswordErrorKey
        {
            get { return _confirmPasswordErrorKey; }
            private set { SetProperty(ref _confirmPasswordErrorKey, value); }
        }

        /// <summary>
        /// Clave de error general del formulario; null si no hay error.
        /// </summary>
        public string FormErrorKey
        {
            get { return _formErrorKey; }
            private set { SetProperty(ref _formErrorKey, value); }
        }

        private void ExecuteSetPassword()
        {
            ClearErrors();

            bool isFormValid = ValidateForm();

            if (isFormValid)
            {
                PasswordAccepted?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ValidationFailed?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ExecuteBack()
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool ValidateForm()
        {
            bool hasNewPassword = !string.IsNullOrWhiteSpace(NewPassword);
            bool hasConfirmation = !string.IsNullOrWhiteSpace(ConfirmPassword);
            bool areFieldsFilled = hasNewPassword && hasConfirmation;
            bool isPasswordStrong = PasswordStrengthScore >= ValidationHelper.RequiredPasswordStrengthScore;
            bool doPasswordsMatch = NewPassword == ConfirmPassword;

            if (!areFieldsFilled)
            {
                FormErrorKey = IncompleteFieldsErrorKey;
            }
            else if (!isPasswordStrong)
            {
                PasswordErrorKey = PolicyErrorKey;
            }
            else if (!doPasswordsMatch)
            {
                ConfirmPasswordErrorKey = MismatchErrorKey;
            }

            bool isFormValid = !HasErrors();
            return isFormValid;
        }

        private void UpdatePasswordStrength()
        {
            PasswordStrengthScore = ValidationHelper.CalculatePasswordStrength(NewPassword);
            StrengthLabelKey = GetStrengthLabelKey(PasswordStrengthScore);
            StrengthBrushKey = GetStrengthBrushKey(PasswordStrengthScore);

            PasswordStrengthChanged?.Invoke(this, EventArgs.Empty);
        }

        private string GetStrengthLabelKey(int strengthScore)
        {
            string labelKey;

            switch (strengthScore)
            {
                case VeryWeakScore:
                    labelKey = VeryWeakLabelKey;
                    break;
                case WeakScore:
                    labelKey = WeakLabelKey;
                    break;
                case FairScore:
                    labelKey = FairLabelKey;
                    break;
                case GoodScore:
                    labelKey = GoodLabelKey;
                    break;
                case StrongScore:
                    labelKey = StrongLabelKey;
                    break;
                default:
                    labelKey = null;
                    break;
            }

            return labelKey;
        }

        private string GetStrengthBrushKey(int strengthScore)
        {
            bool hasStrength = strengthScore > 0;
            bool isWeak = strengthScore <= WeakScore;
            bool isFair = strengthScore == FairScore;
            string brushKey = null;

            if (hasStrength && isWeak)
            {
                brushKey = WeakBrushKey;
            }
            else if (hasStrength && isFair)
            {
                brushKey = FairBrushKey;
            }
            else if (hasStrength)
            {
                brushKey = StrongBrushKey;
            }

            return brushKey;
        }

        private bool HasErrors()
        {
            bool hasFieldErrors = PasswordErrorKey != null || ConfirmPasswordErrorKey != null;
            bool hasErrors = hasFieldErrors || FormErrorKey != null;

            return hasErrors;
        }

        private void ClearErrors()
        {
            PasswordErrorKey = null;
            ConfirmPasswordErrorKey = null;
            FormErrorKey = null;
        }
    }
}