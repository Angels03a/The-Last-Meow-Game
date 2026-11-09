using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Clase base de los ViewModels. Notifica a la vista cuando cambia el valor de una propiedad.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Se produce cuando cambia el valor de una propiedad.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Asigna el valor a un campo y notifica a la vista solo si el valor cambió.
        /// </summary>
        /// <typeparam name="T">Tipo del campo.</typeparam>
        /// <param name="field">Campo que respalda la propiedad.</param>
        /// <param name="newValue">Nuevo valor.</param>
        /// <param name="propertyName">Nombre de la propiedad; el compilador lo completa.</param>
        /// <returns><c>true</c> si el valor cambió; en caso contrario, <c>false</c>.</returns>
        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            bool hasChanged = !EqualityComparer<T>.Default.Equals(field, newValue);

            if (hasChanged)
            {
                field = newValue;
                OnPropertyChanged(propertyName);
            }

            return hasChanged;
        }

        /// <summary>
        /// Notifica a la vista que una propiedad cambió.
        /// </summary>
        /// <param name="propertyName">Nombre de la propiedad que cambió.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}