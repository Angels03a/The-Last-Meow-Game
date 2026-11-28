using System.Collections.Generic;

namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Resultado de consultar la lista de amigos aceptados: los amigos o un fallo de datos.
    /// </summary>
    public class FriendListLookup
    {
        /// <summary>Indica si la consulta se resolvió; false si falló por un problema de datos.</summary>
        public bool WasFriendListLookupSuccessful { get; set; }

        /// <summary>Amigos aceptados ordenados por nickname; vacía si no tiene o si la consulta falló.</summary>
        public List<AcceptedFriend> Friends { get; set; } = new List<AcceptedFriend>();
    }
}