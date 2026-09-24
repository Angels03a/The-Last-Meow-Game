using System.Windows;

namespace TLMG
{
    /// <summary>
    /// Lógica de interacción y ciclo de vida principal para App.xaml.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="App"/> y configura el idioma base.
        /// </summary>
        public App()
        {
            LanguageManager.InitializeLanguage();
        }
    }
}