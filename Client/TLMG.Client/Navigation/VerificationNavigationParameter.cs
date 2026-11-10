namespace TLMG.Client.Navigation
{
    /// <summary>
    /// Parámetro de navegación hacia la pantalla de verificación.
    /// </summary>
    public class VerificationNavigationParameter
    {
        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="VerificationNavigationParameter"/>.
        /// </summary>
        /// <param name="verificationFlow">Flujo que originó la verificación.</param>
        public VerificationNavigationParameter(VerificationFlow verificationFlow)
        {
            VerificationFlow = verificationFlow;
        }

        /// <summary>
        /// Flujo que originó la verificación.
        /// </summary>
        public VerificationFlow VerificationFlow { get; }
    }
}