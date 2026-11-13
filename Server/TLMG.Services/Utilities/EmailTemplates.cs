using log4net;
using System;
using System.IO;
using TLMG.Services.Utilities.Languages;

namespace TLMG.Services.Utilities
{
    public static class EmailTemplates
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EmailTemplates));
        private static readonly EmailLanguageStrategyFactory StrategyFactory = new EmailLanguageStrategyFactory();

        private const string CodePlaceholder = "{{CODE}}";

        private static string GetTemplateContent(string templateBaseName, IEmailLanguageStrategy languageStrategy, 
            string fallbackContent)
        {
            const string htmlExtension = ".html";
            string baseDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");

            string content = fallbackContent;
            string fileName = templateBaseName + languageStrategy.LanguageSuffix + htmlExtension;
            string templatePath = Path.Combine(baseDirectoryPath, fileName);

            try
            {
                content = File.ReadAllText(templatePath);
            }
            catch (FileNotFoundException exception)
            {
                Logger.Error($"Template file not found: {templatePath}", exception);
            }
            catch (DirectoryNotFoundException exception)
            {
                Logger.Error($"Templates directory not found: {baseDirectoryPath}", exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                Logger.Error($"Read permission denied for template: {templatePath}", exception);
            }
            catch (IOException exception)
            {
                Logger.Error($"I/O error reading template file: {templatePath}", exception);
            }

            return content;
        }

        public static string GetAccountVerificationBody(string code, string cultureCode)
        {
            IEmailLanguageStrategy strategy = StrategyFactory.GetStrategy(cultureCode);
            string fallbackContent = strategy.GetAccountVerificationFallback(code);
            string htmlContent = GetTemplateContent("AccountVerification", strategy, fallbackContent);

            string formattedBody = htmlContent.Replace(CodePlaceholder, code);
            return formattedBody;
        }

        public static string GetPasswordResetBody(string code, string cultureCode)
        {
            IEmailLanguageStrategy strategy = StrategyFactory.GetStrategy(cultureCode);
            string fallbackContent = strategy.GetPasswordResetFallback(code);
            string htmlContent = GetTemplateContent("PasswordReset", strategy, fallbackContent);

            string formattedBody = htmlContent.Replace(CodePlaceholder, code);
            return formattedBody;
        }

        public static string GetAccountVerificationSubject(string cultureCode)
        {
            IEmailLanguageStrategy strategy = StrategyFactory.GetStrategy(cultureCode);
            string subject = strategy.GetAccountVerificationSubject();
            return subject;
        }

        public static string GetPasswordResetSubject(string cultureCode)
        {
            IEmailLanguageStrategy strategy = StrategyFactory.GetStrategy(cultureCode);
            string subject = strategy.GetPasswordResetSubject();
            return subject;
        }
    }
}