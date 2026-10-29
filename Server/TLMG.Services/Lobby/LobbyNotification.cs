using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Lobby
{
    /// <summary>
    /// Aviso de que una sala cambió, listo para entregarse a un jugador fuera de cualquier candado.
    /// </summary>
    public sealed class LobbyNotification
    {
        /// <summary>Lista vacía de avisos, para las operaciones que no avisan a nadie.</summary>
        public static readonly IReadOnlyList<LobbyNotification> None = new List<LobbyNotification>().AsReadOnly();

        /// <summary>Crea un aviso de cambio de sala.</summary>
        /// <param name="callback">Callback del canal de lobby del jugador que recibe el aviso.</param>
        /// <param name="room">La sala con la lista actualizada de jugadores.</param>
        public LobbyNotification(ILobbyManagerCallback callback, LobbyRoom room)
        {
            Callback = callback;
            Room = room;
        }

        /// <summary>Obtiene el callback del jugador que recibe el aviso.</summary>
        public ILobbyManagerCallback Callback { get; }

        /// <summary>Obtiene la sala con la lista actualizada de jugadores.</summary>
        public LobbyRoom Room { get; }
    }
}