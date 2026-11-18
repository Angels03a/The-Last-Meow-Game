using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Desenlace del último reto, público para todos los jugadores. Sigue disponible tras terminar el reto, hasta
    /// que se abra la siguiente apuesta o se coloque la siguiente carta; en cualquier otro momento es null.
    /// </summary>
    [DataContract]
    public class MatchChallengeView
    {
        /// <summary>Cuenta del apostador que cumple el reto.</summary>
        [DataMember]
        public int BettorUserId { get; set; }

        /// <summary>Cartas que prometió voltear.</summary>
        [DataMember]
        public int PromisedCount { get; set; }

        /// <summary>Estado o resultado del reto.</summary>
        [DataMember]
        public MatchChallengeOutcome Outcome { get; set; }

        /// <summary>Cartas volteadas en orden; la última es la que detuvo la revelación.</summary>
        [DataMember]
        public List<MatchRevealedCardView> RevealedCards { get; set; }

        /// <summary>Cuenta del jugador castigado; 0 si el reto no falló.</summary>
        [DataMember]
        public int PunishedUserId { get; set; }

        /// <summary>Cuenta de quien eligió o debe elegir la carta que se pierde; 0 si el reto no falló.</summary>
        [DataMember]
        public int PunishmentChooserUserId { get; set; }

        /// <summary>Estado del castigo del reto.</summary>
        [DataMember]
        public MatchPunishmentResolution PunishmentResolution { get; set; }
    }
}
