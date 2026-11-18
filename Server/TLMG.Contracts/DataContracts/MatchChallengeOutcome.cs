using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Resultado de un reto; el cliente solo lo muestra, el servidor lo decide.</summary>
    [DataContract]
    public enum MatchChallengeOutcome
    {
        /// <summary>El apostador todavía está volteando cartas.</summary>
        [EnumMember]
        InProgress,

        /// <summary>El apostador alcanzó el número prometido sin revelar un Gato con Maullido Fatal.</summary>
        [EnumMember]
        Succeeded,

        /// <summary>Se reveló un Gato con Maullido Fatal y el reto falló.</summary>
        [EnumMember]
        Failed
    }
}
