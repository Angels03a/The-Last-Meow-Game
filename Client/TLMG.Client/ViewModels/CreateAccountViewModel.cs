using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Estado y comandos de la pantalla de registro de cuentas.
    /// </summary>
    public class CreateAccountViewModel : ObservableObject
    {
        private string _nickname;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private bool _hasAcceptedTerms;
        private bool _isBusy;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="CreateAccountViewModel"/>.
        /// </summary>
        public CreateAccountViewModel()
        {
            CreateAccountCommand = new RelayCommand(ExecuteCreateAccount, CanExecuteCreateAccount);
            BackCommand = new RelayCommand(ExecuteBack);
        }

        /// <summary>
        /// Se produce cuando el jugador solicita crear la cuenta.
        /// </summary>
        public event EventHandler RegistrationRequested;

        /// <summary>
        /// Se produce cuando el jugador solicita volver al inicio de sesión.
        /// </summary>
        public event EventHandler BackRequested;

        public RelayCommand CreateAccountCommand { get; }

        public RelayCommand BackCommand { get; }

        public string Nickname
        {
            get { return _nickname; }
            set { SetProperty(ref _nickname, value); }
        }

        public string Email
        {
            get { return _email; }
            set { SetProperty(ref _email, value); }
        }

        public string Password
        {
            get { return _password; }
            set { SetProperty(ref _password, value); }
        }

        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set { SetProperty(ref _confirmPassword, value); }
        }

        public bool HasAcceptedTerms
        {
            get { return _hasAcceptedTerms; }
            set { SetProperty(ref _hasAcceptedTerms, value); }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    CreateAccountCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private void ExecuteCreateAccount()
        {
            RegistrationRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool CanExecuteCreateAccount()
        {
            bool canCreate = !IsBusy;
            return canCreate;
        }

        private void ExecuteBack()
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}