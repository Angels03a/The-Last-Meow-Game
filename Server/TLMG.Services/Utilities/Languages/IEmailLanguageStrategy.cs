namespace TLMG.Services.Utilities.Languages
{
    public interface IEmailLanguageStrategy
    {
        string CulturePrefix { get; }
        string LanguageSuffix { get; }

        string GetAccountVerificationSubject();
        string GetPasswordResetSubject();
        string GetAccountVerificationFallback(string code);
        string GetPasswordResetFallback(string code);
    }
}