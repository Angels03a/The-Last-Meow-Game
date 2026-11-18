using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Carta volteada durante un reto: es información pública, la ven todos los jugadores.</summary>
    [DataContract]
    public class MatchRevealedCardView
    {
        /// <summary>Cuenta del dueño de la pila de la que se volteó la carta.</summary>
        [DataMember]
        public int PileOwnerUserId { get; set; }

        /// <summary>Tipo de la carta volteada.</summary>
        [DataMember]
        public MatchCardKind Card { get; set; }
    }
}
