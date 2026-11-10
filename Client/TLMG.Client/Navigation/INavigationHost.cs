namespace TLMG.Client.Navigation
{
    /// <summary>
    /// Contenedor que muestra la pantalla actual.
    /// </summary>
    public interface INavigationHost
    {
        /// <summary>
        /// Pantalla que se está mostrando.
        /// </summary>
        object CurrentScreen { get; set; }
    }
}