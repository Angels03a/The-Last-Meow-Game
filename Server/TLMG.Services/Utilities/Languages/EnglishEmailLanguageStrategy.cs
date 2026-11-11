namespace TLMG.Services.Utilities.Languages
{
    public class EnglishEmailLanguageStrategy : IEmailLanguageStrategy
    {
        public string CulturePrefix
        {
            get
            {
                return "en";
            }
        }

        public string LanguageSuffix
        {
            get
            {
                return "English";
            }
        }

        public string GetAccountVerificationSubject()
        {
            string subject = "Identity Verification - The Last Meow";
            return subject;
        }

        public string GetPasswordResetSubject()
        {
            string subject = "Password Reset Code - The Last Meow";
            return subject;
        }

        public string GetAccountVerificationFallback(string code)
        {
            string fallbackMessage = "Your verification code is: " + code;
            return fallbackMessage;
        }

        public string GetPasswordResetFallback(string code)
        {
            string fallbackMessage = "Your password reset code is: " + code;
            return fallbackMessage;
        }
    }
}