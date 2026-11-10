namespace TLMG.Client.Navigation
{
    /// <summary>
    /// Navegación entre pantallas pedida por los ViewModels, sin conocer las vistas.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>Muestra la pantalla y guarda la actual en el historial.</summary>
        /// <param name="screenId">Destino.</param>
        void NavigateTo(ScreenId screenId);

        /// <summary>Muestra la pantalla con parámetro y guarda la actual en el historial.</summary>
        /// <param name="screenId">Destino.</param>
        /// <param name="parameter">Parámetro de navegación; puede ser null.</param>
        void NavigateTo(ScreenId screenId, object parameter);

        /// <summary>Muestra la pantalla sin guardar la actual en el historial.</summary>
        /// <param name="screenId">Destino.</param>
        void NavigateToWithoutHistory(ScreenId screenId);

        /// <summary>Muestra la pantalla con parámetro sin guardar la actual en el historial.</summary>
        /// <param name="screenId">Destino.</param>
        /// <param name="parameter">Parámetro de navegación; puede ser null.</param>
        void NavigateToWithoutHistory(ScreenId screenId, object parameter);

        /// <summary>Regresa a la pantalla anterior, o al menú principal si el historial está vacío.</summary>
        void GoBack();

        /// <summary>Vacía el historial de pantallas.</summary>
        void ClearHistory();
    }
}