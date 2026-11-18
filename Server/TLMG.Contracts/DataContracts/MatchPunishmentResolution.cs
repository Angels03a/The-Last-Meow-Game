using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Cómo se resolvió el castigo de un reto fallido.</summary>
    [DataContract]
    public enum MatchPunishmentResolution
    {
        /// <summary>El reto no falló, así que no hay castigo.</summary>
        [EnumMember]
        None,

        /// <summary>El castigo sigue pendiente: quien debe elegir todavía no elige.</summary>
        [EnumMember]
        Pending,

        /// <summary>Quien debía elegir eligió la carta que se eliminó.</summary>
        [EnumMember]
        ChosenByChooser,

        /// <summary>El sistema eligió al azar, porque venció el plazo o salió quien debía elegir.</summary>
        [EnumMember]
        ChosenAtRandom,

        /// <summary>El castigado tenía una sola vida: se eliminó sin elegir.</summary>
        [EnumMember]
        SingleLifeNoChoice,

        /// <summary>El castigo se canceló porque el castigado salió de la partida.</summary>
        [EnumMember]
        Cancelled
    }
}
