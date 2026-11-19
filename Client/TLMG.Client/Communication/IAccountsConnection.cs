using System;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Área de cuentas de la conexión con el servidor: inicio de sesión y registro de jugadores. Los ViewModels de
    /// login y de registro dependen solo de esta interfaz (D-70); la implementa ServerConnectionManager, que
    /// conserva la sesión (D-60). Los resultados llevan la ErrorKey tal como la manda el servidor y nunca textos: la
    /// traducción ocurre en pantalla. Los fallos de red o inesperados llegan como resultados fallidos con su propia
    /// ErrorKey.
    /// </summary>
    public interface IAccountsConnection
    {
        event EventHandler RecoveryStateChanged;
        bool IsMatchRecoveryPending { get; }
        bool IsMatchRecoveryExhausted { get; }
        void RetryMatchRecovery();

        /// <summary>
        /// Obtiene el nickname del jugador en sesión, o null si no hay sesión registrada (por ejemplo, un invitado).
        /// Solo sirve para mostrar.
        /// </summary>
        string PlayerNickname { get; }

        /// <summary>
        /// Inicia sesión como jugador registrado y, si el servidor la acepta, deja la sesión abierta en el manager.
        /// </summary>
        /// <param name="email">Correo capturado por el jugador.</param>
        /// <param name="password">Contraseña capturada por el jugador.</param>
        /// <returns>
        /// El resultado del servidor, o un resultado fallido con ErrorKey si no se pudo conectar o abrir la sesión.
        /// Nunca es null y su SessionToken siempre es null: el token solo lo conserva el manager.
        /// </returns>
        OperationLogInResult LogIn(string email, string password);

        /// <summary>
        /// Solicita al servidor el registro de una cuenta de jugador. No inicia sesión.
        /// </summary>
        /// <param name="playerAccountRegistration">Datos capturados en el formulario de registro.</param>
        /// <returns>
        /// El resultado del servidor, o un resultado fallido con ErrorKey si no se pudo conectar. Nunca es null.
        /// </returns>
        OperationResult RegisterPlayerAccount(
            PlayerAccountRegistration playerAccountRegistration);

        /// <summary>
        /// Cierra la sesión: libera los canales y limpia el estado del jugador. Es seguro llamarlo sin sesión, por
        /// ejemplo con un invitado.
        /// </summary>
        LogOutResult LogOut();

        /// <summary>Consulta la sala y partida recuperables con la sesión actual.</summary>
        OperationRecoveryContextResult GetRecoveryContext();

        OperationResult RequestAccountVerificationCode(
            PlayerAccountRegistration playerAccountRegistration,
            string cultureCode);

        OperationResult CompletePlayerAccountRegistration(
            PlayerAccountRegistration playerAccountRegistration,
            string verificationCode);

        OperationResult ResendAccountVerificationCode(string email, string cultureCode);
    }
}
