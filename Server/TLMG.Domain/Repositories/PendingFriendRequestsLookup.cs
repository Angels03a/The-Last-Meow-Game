using System.Collections.Generic;

namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Resultado de consultar las solicitudes de amistad pendientes de un jugador.
    /// </summary>
    public class PendingFriendRequestsLookup
    {
        /// <summary>Indica si la consulta funcionó; false si la base de datos falló.</summary>
        public bool WasLookupSuccessful { get; set; }

        /// <summary>Solicitudes pendientes recibidas y enviadas; vacía si la consulta falló.</summary>
        public List<PendingFriendRequest> Requests { get; set; } = new List<PendingFriendRequest>();
    }
}
