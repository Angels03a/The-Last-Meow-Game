using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Estado de una sala: su identificador y los jugadores que están dentro.
    /// </summary>
    [DataContract]
    public class LobbyRoom
    {
        /// <summary>Identificador de la sala.</summary>
        [DataMember]
        public int RoomId { get; set; }

        /// <summary>Jugadores dentro de la sala, en el orden en que entraron.</summary>
        [DataMember]
        public List<LobbyPlayer> Players { get; set; }

        /// <summary>Versión creciente del estado; permite descartar avisos atrasados de esta sala.</summary>
        [DataMember]
        public long Version { get; set; }

        /// <summary>Última generación creada; solo está activa si CountdownEndsAtUtc tiene valor.</summary>
        [DataMember]
        public long CountdownId { get; set; }

        /// <summary>Hora límite UTC del servidor; null cuando no hay cuenta activa.</summary>
        [DataMember]
        public DateTime? CountdownEndsAtUtc { get; set; }

        /// <summary>Hora UTC de la copia; el cliente no decide el vencimiento de la cuenta.</summary>
        [DataMember]
        public DateTime ServerTimeUtc { get; set; }

        /// <summary>Indica que el servidor autorizó la entrada a la partida.</summary>
        [DataMember]
        public bool InGame { get; set; }

        /// <summary>Identificador estable de la partida autorizada; null antes del inicio.</summary>
        [DataMember]
        public Guid? MatchId { get; set; }

        /// <summary>Hora UTC de autorización; null antes del inicio.</summary>
        [DataMember]
        public DateTime? MatchStartedAtUtc { get; set; }

        /// <summary>Cantidad de cartas por jugador de la partida autorizada o por iniciar.</summary>
        [DataMember]
        public int CardsPerPlayerCount { get; set; }
    }
}