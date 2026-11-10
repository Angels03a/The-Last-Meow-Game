namespace TLMG.Client.Navigation
{
    /// <summary>
    /// Una pantalla creada: su vista y su ViewModel.
    /// </summary>
    public class ScreenEntry
    {
        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ScreenEntry"/>.
        /// </summary>
        /// <param name="view">Vista.</param>
        /// <param name="viewModel">ViewModel; null en pantallas aún no migradas.</param>
        public ScreenEntry(object view, object viewModel)
        {
            View = view;
            ViewModel = viewModel;
        }

        /// <summary>
        /// Vista que se muestra.
        /// </summary>
        public object View { get; }

        /// <summary>
        /// ViewModel de la vista; null en pantallas aún no migradas.
        /// </summary>
        public object ViewModel { get; }
    }
}