using System;
using System.Windows.Input;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Comando que ejecuta un método con nombre, con una condición opcional de habilitación.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// Inicializa un comando que siempre está habilitado.
        /// </summary>
        /// <param name="execute">Método que ejecuta el comando.</param>
        public RelayCommand(Action execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Inicializa un comando con una condición de habilitación.
        /// </summary>
        /// <param name="execute">Método que ejecuta el comando.</param>
        /// <param name="canExecute">Método que indica si el comando está habilitado; puede ser null.</param>
        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// Se produce cuando cambia si el comando puede ejecutarse.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            bool hasExecuteMethod = _execute != null;
            bool hasNoCondition = _canExecute == null;
            bool canExecute = hasExecuteMethod && (hasNoCondition || _canExecute());

            return canExecute;
        }

        public void Execute(object parameter)
        {
            if (_execute != null)
            {
                _execute();
            }
        }

        /// <summary>
        /// Pide a la vista que vuelva a evaluar si el comando está habilitado.
        /// </summary>
        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}