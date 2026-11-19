using System;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Referencias de la partida a la que aún tiene acceso el dueño del token.</summary>
    [DataContract]
    public class OperationRecoveryContextResult : OperationResult
    {
        /// <summary>Sala autorizada, o null si no existe una partida recuperable.</summary>
        [DataMember]
        public int? RoomId { get; set; }

        /// <summary>Partida autorizada, o null si no existe una partida recuperable.</summary>
        [DataMember]
        public Guid? MatchId { get; set; }
    }
}
