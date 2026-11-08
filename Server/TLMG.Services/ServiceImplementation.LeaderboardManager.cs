using System;
using System.ServiceModel;
using TLMG.Contracts;
using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;

namespace TLMG.Services
{
    public partial class ServiceImplementation : ILeaderboardManager
    {
        public void GetLeaderboard()
        {
            try
            {
                ILeaderboardManagerCallback callback =
                    OperationContext.Current.GetCallbackChannel<ILeaderboardManagerCallback>();

                OperationGetLeaderboardResult operationGetLeaderboardResult = _leaderboardService.GetLeaderboard();

                SafeNotify(() => callback.GetLeaderboardResult(operationGetLeaderboardResult));
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                _log.Fatal("Memoria insuficiente durante GetLeaderboard.", outOfMemoryException);
                SystemFault fault = new SystemFault { SystemErrorMessage = ErrorKeys.Common.OutOfMemory };
                throw new FaultException<SystemFault>(fault, new FaultReason("Memoria insuficiente en el servidor."));
            }
        }
    }
}