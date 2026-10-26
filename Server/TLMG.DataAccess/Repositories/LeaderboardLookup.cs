using System.Collections.Generic;

namespace TLMG.DataAccess.Repositories
{
    /// <summary>
    /// Resultado de consultar la tabla de posiciones: los jugadores o un fallo de datos.
    /// </summary>
    public class LeaderboardLookup
    {
        /// <summary>Indica si la consulta se resolvió; false si falló por un problema de datos.</summary>
        public bool WasSuccessful { get; set; }

        /// <summary>Jugadores ordenados por puntos, de mayor a menor; vacía si la consulta falló.</summary>
        public List<RankedPlayer> Players { get; set; }
    }
}