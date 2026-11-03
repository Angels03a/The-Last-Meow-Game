using System.ServiceModel;

using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.DataAccess.Repositories;
using TLMG.Services.Leaderboard;

namespace TLMG.Services
{
    public partial class ServiceImplementation : ILeaderboardManager
    {
        // El repositorio se crea aquí y no se comparte con el otro archivo parcial: el orden en que se inicializan
        // los campos estáticos entre archivos parciales no está definido.
        private static readonly LeaderboardService _leaderboardService =
            new LeaderboardService(new PlayerRepository());

        /// <summary>Delega la consulta al servicio de la tabla de posiciones y responde por callback.</summary>
        public void GetLeaderboard()
        {
            ILeaderboardManagerCallback callback =
                OperationContext.Current.GetCallbackChannel<ILeaderboardManagerCallback>();

            OperationGetLeaderboardResult operationGetLeaderboardResult = _leaderboardService.GetLeaderboard();

            SafeNotify(() => callback.GetLeaderboardResult(operationGetLeaderboardResult));
        }
    }
}