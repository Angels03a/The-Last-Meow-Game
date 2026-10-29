using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Resultado de salir de una sala: éxito o clave de error para que el cliente la traduzca.
    /// </summary>
    [DataContract]
    public class OperationLeaveLobbyResult
    {
        /// <summary>Indica si el jugador quedó fuera de toda sala.</summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>Clave del texto de error; null si el jugador quedó fuera de la sala.</summary>
        [DataMember]
        public string ErrorKey { get; set; }
    }
}