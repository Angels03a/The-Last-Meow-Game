using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Resultado de consultar la tabla de posiciones: jugadores ordenados o clave de error.
    /// </summary>
    [DataContract]
    public class OperationGetLeaderboardResult : OperationResult
    {
        /// <summary>Jugadores ordenados por puntos, de mayor a menor.</summary>
        [DataMember]
        public List<LeaderboardPlayer> Players { get; set; }
    }
}