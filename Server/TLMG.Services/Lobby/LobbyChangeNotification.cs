using System.Collections.Generic;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Lobby
{
    /// <summary>
    /// Aviso de que una sala cambió, listo para entregarse a un jugador fuera de cualquier candado.
    /// </summary>
    public sealed class LobbyChangeNotification
    {
        /// <summary>Lista vacía de avisos, para las operaciones que no avisan a nadie.</summary>
        public static readonly IReadOnlyList<LobbyChangeNotification> None = 
            new List<LobbyChangeNotification>().AsReadOnly();

        /// <summary>Crea un aviso de cambio de sala.</summary>
        /// <param name="callback">Callback del canal de lobby del jugador que recibe el aviso.</param>
        /// <param name="lobbyRoom">La sala con la lista actualizada de jugadores.</param>
        public LobbyChangeNotification(ILobbyManagerCallback callback, LobbyRoom lobbyRoom)
        {
            Callback = callback;
            LobbyRoom = lobbyRoom;
        }

        /// <summary>Obtiene el callback del jugador que recibe el aviso.</summary>
        public ILobbyManagerCallback Callback { get; }

        /// <summary>Obtiene la sala con la lista actualizada de jugadores.</summary>
        public LobbyRoom LobbyRoom { get; }
    }
}