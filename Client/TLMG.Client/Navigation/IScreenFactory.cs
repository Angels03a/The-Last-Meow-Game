namespace TLMG.Client.Navigation
{
    /// <summary>
    /// Crea pantallas con su ViewModel ya asignado.
    /// </summary>
    public interface IScreenFactory
    {
        /// <summary>
        /// Crea la pantalla del destino indicado.
        /// </summary>
        /// <param name="screenId">Destino.</param>
        /// <param name="parameter">Parámetro de navegación; puede ser null.</param>
        /// <returns>La vista y su ViewModel.</returns>
        ScreenEntry Create(ScreenId screenId, object parameter);
    }
}