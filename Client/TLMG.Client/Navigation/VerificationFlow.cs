namespace TLMG.Client.Navigation
{
    /// <summary>
    /// Flujo que originó la pantalla de verificación; decide a dónde se navega al terminar o al volver.
    /// </summary>
    public enum VerificationFlow
    {
        AccountRegistration,
        PasswordRecovery
    }
}