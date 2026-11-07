using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Resultado de cambiar el estado Listo del jugador.</summary>
    [DataContract]
    public class OperationChangePlayerReadinessResult : OperationResult
    {
        /// <summary>Estado actual autorizado de la sala; null si se rechazó la petición.</summary>
        [DataMember]
        public LobbyRoom Room { get; set; }
    }
}
