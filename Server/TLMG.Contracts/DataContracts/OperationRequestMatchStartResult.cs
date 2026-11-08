using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Resultado de solicitar el inicio autorizado de una partida.</summary>
    [DataContract]
    public class OperationRequestMatchStartResult : OperationResult
    {
        /// <summary>Estado actual autorizado de la sala; null si se rechazó la petición.</summary>
        [DataMember]
        public LobbyRoom Room { get; set; }

        /// <summary>Segundos antes de volver a comprobar una cuenta activa; cero sin cuenta.</summary>
        [DataMember]
        public int RetryAfterSeconds { get; set; }
    }
}
