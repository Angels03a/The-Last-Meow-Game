using System.ServiceModel;

namespace TLMG.Contracts
{
    /// <summary>
    /// Contrato para crear salas, entrar a ellas y salir; el estado de la sala llega por ILobbyManagerCallback.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(ILobbyManagerCallback))]
    public interface ILobbyManager
    {
        /// <summary>Solicita crear una sala nueva y quedar dentro; la respuesta llega por CreateRoomResult.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        [OperationContract(IsOneWay = true)]
        void CreateRoom(string sessionToken);

        /// <summary>Solicita entrar a una sala con autorización; la respuesta llega por EnterLobbyResult.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        /// <param name="roomId">Identificador de la sala a la que se quiere entrar.</param>
        [OperationContract(IsOneWay = true)]
        void EnterLobby(string sessionToken, int roomId);

        /// <summary>Solicita salir de la sala actual; la respuesta llega por LeaveLobbyResult.</summary>
        /// <param name="sessionToken">Token de la sesión del jugador.</param>
        [OperationContract(IsOneWay = true)]
        void LeaveLobby(string sessionToken);
    }
}