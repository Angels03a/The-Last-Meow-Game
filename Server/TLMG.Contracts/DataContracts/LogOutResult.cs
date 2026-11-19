using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>Confirma si el servidor revocó la sesión o devuelve una clave de error.</summary>
    [DataContract]
    public class LogOutResult : OperationResult
    {
    }
}
