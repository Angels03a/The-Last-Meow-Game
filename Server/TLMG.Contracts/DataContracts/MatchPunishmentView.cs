using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Castigo pendiente, público para todos: quién elige y a quién castigan. No lleva ninguna carta; el castigado ve
    /// su mano por la vista propia y quien elige a ciegas solo recibe la cantidad de lugares.
    /// </summary>
    [DataContract]
    public class MatchPunishmentView
    {
        /// <summary>Cuenta de quien elige la carta que se pierde.</summary>
        [DataMember]
        public int ChooserUserId { get; set; }

        /// <summary>Cuenta del jugador que pierde la carta.</summary>
        [DataMember]
        public int PunishedUserId { get; set; }

        /// <summary>Indica si se elige a ciegas (Gato ajeno); si no, el castigado elige viendo su mano.</summary>
        [DataMember]
        public bool IsBlindChoice { get; set; }

        /// <summary>Cantidad de cartas entre las que se elige; coincide con las vidas del castigado.</summary>
        [DataMember]
        public int CardSlotCount { get; set; }
    }
}
