using System.Collections.Generic;

namespace TLMG.DataAccess.Repositories
{
    /// <summary>
    /// Resultado de consultar la lista de amigos aceptados: los amigos o un fallo de datos.
    /// </summary>
    public class FriendListLookup
    {
        /// <summary>Indica si la consulta se resolvió; false si falló por un problema de datos.</summary>
        public bool WasSuccessful { get; set; }

        /// <summary>Amigos aceptados ordenados por nickname; vacía si no tiene o si la consulta falló.</summary>
        public List<AcceptedFriend> Friends { get; set; }
    }
}