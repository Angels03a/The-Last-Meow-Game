using System.Collections.Generic;

using TLMG.Domain.Authorization;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Identidad de un jugador con la sesión iniciada; no incluye el token. Solo el registro de sesiones puede
    /// cambiar el nickname, dentro de su candado, para que todos vean el nombre guardado.
    /// </summary>
    public sealed class PlayerSession
    {
        /// <summary>Crea la identidad de una sesión.</summary>
        /// <param name="userId">Identificador de la cuenta (el mismo de Friendship).</param>
        /// <param name="nickname">Nickname de la cuenta.</param>
        public PlayerSession(int userId, string nickname)
        {
            UserId = userId;
            Nickname = nickname;
        }

        /// <summary>Indica si la identidad procede de una cuenta autenticada.</summary>
        internal bool IsRegistered => Role == AccountRole.RegisteredPlayer;

        /// <summary>Rol fijado al iniciar la sesión; el servidor nunca lo toma del cliente.</summary>
        internal AccountRole Role { get; private set; }

        internal System.DateTime LastActivityUtc { get; set; }

        internal Dictionary<HeartbeatChannel, LinkHealth> Links { get; } =
            new Dictionary<HeartbeatChannel, LinkHealth>();

        /// <summary>Identificador aleatorio de diagnóstico para esta sesión.</summary>
        public System.Guid DiagnosticId { get; } = System.Guid.NewGuid();

        /// <summary>Obtiene el identificador de la cuenta.</summary>
        public int UserId { get; }

        /// <summary>Obtiene el nickname de la cuenta.</summary>
        public string Nickname { get; internal set; }

        internal static PlayerSession CreateRegistered(int userId, string nickname)
        {
            return new PlayerSession(userId, nickname) { Role = AccountRole.RegisteredPlayer };
        }

        internal static PlayerSession CreateGuest(int guestUserId, string nickname)
        {
            return new PlayerSession(guestUserId, nickname) { Role = AccountRole.Guest };
        }
    }
}