using System.ServiceModel;
using TLMG.Contracts.DataContracts;

namespace TLMG.Contracts
{
    [ServiceContract]
    public interface IForgotPasswordManager
    {
        [OperationContract]
        OperationResult RequestPasswordResetCode(string email, string cultureCode);

        [OperationContract]
        OperationResult VerifyPasswordResetCode(string email, string code);

        [OperationContract]
        OperationResult ResetPassword(string email, string newPassword);
    }
}