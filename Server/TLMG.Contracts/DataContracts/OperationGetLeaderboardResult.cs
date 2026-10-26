using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Resultado de consultar la tabla de posiciones: jugadores ordenados o clave de error.
    /// </summary>
    [DataContract]
    public class OperationGetLeaderboardResult
    {
        /// <summary>Indica si la consulta se resolvió.</summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>Clave del texto de error; null si la consulta se resolvió.</summary>
        [DataMember]
        public string ErrorKey { get; set; }

        /// <summary>Jugadores ordenados por puntos, de mayor a menor.</summary>
        [DataMember]
        public List<LeaderboardPlayer> Players { get; set; }
    }
}