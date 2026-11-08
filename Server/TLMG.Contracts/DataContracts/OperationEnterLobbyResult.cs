using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Resultado de entrar a una sala: la sala o la clave de error para que el cliente la traduzca.
    /// </summary>
    [DataContract]
    public class OperationEnterLobbyResult : OperationResult
    {
        /// <summary>La sala con el jugador ya dentro; null si la operación falló.</summary>
        [DataMember]
        public LobbyRoom Room { get; set; }
    }
}