using System;
using System.Collections.Generic;

namespace TLMG.Client.Navigation
{
    /// <summary>
    /// Administra el historial de pantallas y libera los ViewModels que se descartan.
    /// </summary>
    public class NavigationService : INavigationService
    {
        private const int EmptyHistoryCount = 0;

        private readonly INavigationHost _host;
        private readonly Stack<ScreenEntry> _history;
        private ScreenEntry _currentEntry;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="NavigationService"/>.
        /// </summary>
        /// <param name="host">Contenedor que muestra la pantalla actual.</param>
        public NavigationService(INavigationHost host)
        {
            _host = host;
            _history = new Stack<ScreenEntry>();
        }

        /// <summary>
        /// Fábrica que crea las pantallas; se asigna al componer la aplicación.
        /// </summary>
        public IScreenFactory ScreenFactory { get; set; }

        public void NavigateTo(ScreenId screenId)
        {
            NavigateTo(screenId, null);
        }

        public void NavigateTo(ScreenId screenId, object parameter)
        {
            Show(ScreenFactory.Create(screenId, parameter), true);
        }

        public void NavigateToWithoutHistory(ScreenId screenId)
        {
            NavigateToWithoutHistory(screenId, null);
        }

        public void NavigateToWithoutHistory(ScreenId screenId, object parameter)
        {
            Show(ScreenFactory.Create(screenId, parameter), false);
        }

        public void GoBack()
        {
            if (_history.Count > EmptyHistoryCount)
            {
                ScreenEntry previousEntry = _history.Pop();
                DisposeEntry(_currentEntry);
                _currentEntry = previousEntry;
                _host.CurrentScreen = previousEntry.View;
            }
            else
            {
                NavigateToWithoutHistory(ScreenId.MainMenu);
            }
        }

        public void ClearHistory()
        {
            foreach (ScreenEntry historyEntry in _history)
            {
                DisposeEntry(historyEntry);
            }

            _history.Clear();
        }

        /// <summary>
        /// Muestra una vista ya creada. Solo para pantallas aún no migradas; se elimina al terminar la migración.
        /// </summary>
        /// <param name="view">Vista a mostrar.</param>
        /// <param name="saveInHistory">Indica si la pantalla actual se guarda en el historial.</param>
        public void ShowExistingScreen(object view, bool saveInHistory)
        {
            Show(new ScreenEntry(view, null), saveInHistory);
        }

        private void Show(ScreenEntry newEntry, bool saveInHistory)
        {
            if (newEntry.View != null)
            {
                if (_currentEntry != null)
                {
                    if (saveInHistory)
                    {
                        _history.Push(_currentEntry);
                    }
                    else
                    {
                        DisposeEntry(_currentEntry);
                    }
                }

                _currentEntry = newEntry;
                _host.CurrentScreen = newEntry.View;
            }
        }

        private void DisposeEntry(ScreenEntry screenEntry)
        {
            if (screenEntry != null && screenEntry.ViewModel is IDisposable disposableViewModel)
            {
                disposableViewModel.Dispose();
            }
        }
    }
}