using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Fase de la ronda que viaja por la red; Services la traduce desde el motor.</summary>
    [DataContract]
    public enum MatchRoundPhase
    {
        /// <summary>Los jugadores colocan cartas por turnos.</summary>
        [EnumMember]
        PlacingCards,

        /// <summary>Hay una apuesta en curso.</summary>
        [EnumMember]
        Betting,

        /// <summary>Queda un solo apostador y el reto está listo.</summary>
        [EnumMember]
        ChallengeReady,

        /// <summary>Hay un castigo pendiente tras un reto fallido.</summary>
        [EnumMember]
        PunishmentPending,

        /// <summary>La partida terminó.</summary>
        [EnumMember]
        MatchFinished
    }
}
