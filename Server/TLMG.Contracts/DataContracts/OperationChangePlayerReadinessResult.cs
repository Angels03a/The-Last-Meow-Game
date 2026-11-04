using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Resultado de cambiar el estado Listo del jugador.</summary>
    [DataContract]
    public class OperationChangePlayerReadinessResult
    {
        /// <summary>Indica que se atendió la petición; el inicio se confirma únicamente con Room.InGame.</summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>Clave de error para traducir en el cliente; null si se atendió la petición.</summary>
        [DataMember]
        public string ErrorKey { get; set; }

        /// <summary>Estado actual autorizado de la sala; null si se rechazó la petición.</summary>
        [DataMember]
        public LobbyRoom Room { get; set; }
    }
}
