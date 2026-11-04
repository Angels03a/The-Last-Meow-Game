using System.ServiceModel;

using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    /// <summary>Canal dúplex propio para suscribirse al chat y enviar mensajes de sala.</summary>
    [ServiceContract(CallbackContract = typeof(IChatManagerCallback))]
    public interface IChatManager
    {
        /// <summary>Suscribe el canal después de entrar a una sala; la última suscripción gana.</summary>
        /// <param name="sessionToken">Token vigente del jugador registrado.</param>
        [OperationContract(IsOneWay = true)]
        void SubscribeToChat(string sessionToken);

        /// <summary>Solicita un envío; el resultado solo informa rechazos y el eco confirma la aceptación.</summary>
        /// <param name="sessionToken">Token vigente del jugador registrado.</param>
        /// <param name="request">Sala, identificador de correlación y texto.</param>
        [OperationContract(IsOneWay = true)]
        void SendChatMessage(string sessionToken, OperationSendChatMessageRequest request);
    }
}
