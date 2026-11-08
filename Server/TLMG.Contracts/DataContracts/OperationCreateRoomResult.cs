using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Resultado de crear una sala: la sala o la clave de error para que el cliente la traduzca.
    /// </summary>
    [DataContract]
    public class OperationCreateRoomResult : OperationResult
    {
        /// <summary>La sala creada; null si la operación falló.</summary>
        [DataMember]
        public LobbyRoom LobbyRoom { get; set; }
    }
}