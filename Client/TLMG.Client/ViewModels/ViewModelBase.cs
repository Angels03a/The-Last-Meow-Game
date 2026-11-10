using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Clase base de los ViewModels. Notifica cambios a la vista y libera recursos al descartarse.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// Se produce cuando cambia el valor de una propiedad.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Libera las suscripciones y temporizadores del ViewModel.
        /// </summary>
        public void Dispose()
        {
            OnDisposed();
        }

        /// <summary>
        /// Asigna el valor a un campo y notifica a la vista solo si el valor cambió.
        /// </summary>
        /// <typeparam name="FieldType">Tipo del campo.</typeparam>
        /// <param name="fieldType">Campo que respalda la propiedad.</param>
        /// <param name="newFieldValue">Nuevo valor.</param>
        /// <param name="propertyName">Nombre de la propiedad; el compilador lo completa.</param>
        /// <returns><c>true</c> si el valor cambió; en caso contrario, <c>false</c>.</returns>
        protected bool SetProperty<FieldType>(ref FieldType fieldType, FieldType newFieldValue, 
            [CallerMemberName] string propertyName = null)
        {
            bool hasFieldChanged = !EqualityComparer<FieldType>.Default.Equals(fieldType, newFieldValue);

            if (hasFieldChanged)
            {
                fieldType = newFieldValue;
                OnPropertyChanged(propertyName);
            }

            return hasFieldChanged;
        }

        /// <summary>
        /// Notifica a la vista que una propiedad cambió.
        /// </summary>
        /// <param name="propertyName">Nombre de la propiedad que cambió.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Los ViewModels sobrescriben este método para desuscribirse de eventos y detener temporizadores.
        /// </summary>
        protected virtual void OnDisposed()
        {

        }
    }
}