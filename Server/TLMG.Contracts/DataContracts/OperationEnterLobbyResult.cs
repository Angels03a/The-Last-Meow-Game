using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Resultado de entrar a una sala: la sala o la clave de error para que el cliente la traduzca.
    /// </summary>
    [DataContract]
    public class OperationEnterLobbyResult
    {
        /// <summary>Indica si el jugador entró a la sala.</summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>Clave del texto de error; null si el jugador entró.</summary>
        [DataMember]
        public string ErrorKey { get; set; }

        /// <summary>La sala con el jugador ya dentro; null si la operación falló.</summary>
        [DataMember]
        public LobbyRoom Room { get; set; }
    }
}