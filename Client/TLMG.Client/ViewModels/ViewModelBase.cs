using System;

using CommunityToolkit.Mvvm.ComponentModel;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Clase base de los ViewModels. Notifica cambios a la vista (ObservableObject) y libera recursos al descartarse.
    /// </summary>
    public abstract class ViewModelBase : ObservableObject, IDisposable
    {
        /// <summary>
        /// Libera las suscripciones y temporizadores del ViewModel.
        /// </summary>
        public void Dispose()
        {
            OnDisposed();
        }

        /// <summary>
        /// Los ViewModels sobrescriben este método para desuscribirse de eventos y detener temporizadores.
        /// </summary>
        protected virtual void OnDisposed()
        {

        }
    }
}