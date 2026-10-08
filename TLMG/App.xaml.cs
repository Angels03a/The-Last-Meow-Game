using System.Windows;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace TLMG
{
    public partial class App : Application
    {
        public App()
        {
            LanguageManager.InitializeLanguage();
        }
    }
}