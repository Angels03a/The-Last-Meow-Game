using System;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Gestiona los comandos de navegación de la pantalla de opciones.
    /// </summary>
    public class OptionsViewModel : ViewModelBase
    {
        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="OptionsViewModel"/>.
        /// </summary>
        public OptionsViewModel()
        {
            BackCommand = new RelayCommand(ExecuteBack);
        }

        /// <summary>
        /// Se produce cuando el usuario solicita regresar a la pantalla anterior.
        /// </summary>
        public event EventHandler BackRequested;

        /// <summary>Comando para regresar al menú principal.</summary>
        public ICommand BackCommand { get; }

        private void ExecuteBack()
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}