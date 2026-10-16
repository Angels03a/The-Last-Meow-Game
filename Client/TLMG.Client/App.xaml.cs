using System.Windows;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace TLMG.Client
{
    /// <summary>
    /// Punto de entrada y lógica de inicialización general de la aplicación.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Inicializa una nueva instancia de la aplicación y configura el idioma inicial.
        /// </summary>
        public App()
        {
            LanguageManager.InitializeLanguage();
        }
    }
}