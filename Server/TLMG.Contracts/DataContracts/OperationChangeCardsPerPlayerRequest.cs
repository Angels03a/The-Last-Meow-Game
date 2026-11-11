using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Petición de la cantidad de cartas por jugador deseada para la sala.</summary>
    [DataContract]
    public class OperationChangeCardsPerPlayerRequest
    {
        /// <summary>Sala a la que pertenece la petición.</summary>
        [DataMember]
        public int RoomId { get; set; }

        /// <summary>Cantidad deseada de cartas por jugador: 4, 6 u 8; repetir la actual no reinicia el Listo.</summary>
        [DataMember]
        public int CardsPerPlayerCount { get; set; }
    }
}
