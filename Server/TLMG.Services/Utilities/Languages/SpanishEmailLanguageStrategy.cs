namespace TLMG.Services.Utilities.Languages
{
    public class SpanishEmailLanguageStrategy : IEmailLanguageStrategy
    {
        public string CulturePrefix
        {
            get
            {
                return "es";
            }
        }

        public string LanguageSuffix
        {
            get
            {
                return "Spanish";
            }
        }

        public string GetAccountVerificationSubject()
        {
            string subject = "Verificación de Identidad - The Last Meow";
            return subject;
        }

        public string GetPasswordResetSubject()
        {
            string subject = "Código de Restablecimiento - The Last Meow";
            return subject;
        }

        public string GetAccountVerificationFallback(string code)
        {
            string fallbackMessage = "Tu código de verificación es: " + code;
            return fallbackMessage;
        }

        public string GetPasswordResetFallback(string code)
        {
            string fallbackMessage = "Tu código de recuperación es: " + code;
            return fallbackMessage;
        }
    }
}