using TLMG.Client.UsersManagerReference;

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
        /// Inicializa una nueva instancia para el flujo de registro de cuenta.
        /// </summary>
        /// <param name="verificationFlow">Flujo que originó la verificación.</param>
        /// <param name="pendingRegistration">Datos capturados pendientes de confirmación por correo.</param>
        public VerificationNavigationParameter(
            VerificationFlow verificationFlow,
            PlayerAccountRegistration pendingRegistration)
        {
            VerificationFlow = verificationFlow;
            PendingRegistration = pendingRegistration;
            TargetEmail = pendingRegistration != null ? pendingRegistration.Email : null;
        }

        /// <summary>
        /// Flujo que originó la verificación.
        /// </summary>
        public VerificationFlow VerificationFlow { get; }

        /// <summary>
        /// Datos de registro pendientes de persistir una vez verificado el correo.
        /// </summary>
        public PlayerAccountRegistration PendingRegistration { get; }

        /// <summary>
        /// Correo electrónico al que se envió el código de verificación.
        /// </summary>
        public string TargetEmail { get; }
    }
}