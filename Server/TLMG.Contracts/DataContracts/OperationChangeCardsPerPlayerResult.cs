using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Resultado de cambiar la cantidad de cartas por jugador de la sala.</summary>
    [DataContract]
    public class OperationChangeCardsPerPlayerResult : OperationResult
    {
        /// <summary>Estado actual autorizado de la sala; null si se rechazó la petición.</summary>
        [DataMember]
        public LobbyRoom Room { get; set; }
    }
}
