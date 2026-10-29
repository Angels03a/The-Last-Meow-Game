using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Resultado de crear una sala: la sala o la clave de error para que el cliente la traduzca.
    /// </summary>
    [DataContract]
    public class OperationCreateRoomResult
    {
        /// <summary>Indica si la sala se creó y el jugador quedó dentro.</summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>Clave del texto de error; null si la sala se creó.</summary>
        [DataMember]
        public string ErrorKey { get; set; }

        /// <summary>La sala creada; null si la operación falló.</summary>
        [DataMember]
        public LobbyRoom Room { get; set; }
    }
}