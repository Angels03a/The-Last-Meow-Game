using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>Resultados y mensajes recibidos exclusivamente por el canal de chat.</summary>
    [ServiceContract]
    public interface IChatManagerCallback
    {
        /// <summary>Entrega el resultado de la suscripción.</summary>
        /// <param name="operationSubscribeToChatResult">Éxito o clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void SubscribeToChatResult(OperationSubscribeToChatResult operationSubscribeToChatResult);

        /// <summary>Entrega únicamente el rechazo de un envío.</summary>
        /// <param name="operationSendChatMessageResult">Correlación y clave de error.</param>
        [OperationContract(IsOneWay = true)]
        void SendChatMessageResult(OperationSendChatMessageResult operationSendChatMessageResult);

        /// <summary>Entrega un mensaje aceptado de la propia sala, incluido el eco al emisor.</summary>
        /// <param name="chatMessage">Mensaje aceptado por el servidor.</param>
        [OperationContract(IsOneWay = true)]
        void OnChatMessageReceived(ChatMessage chatMessage);
    }
}
