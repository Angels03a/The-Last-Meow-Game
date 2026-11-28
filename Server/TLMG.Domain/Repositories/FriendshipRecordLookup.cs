namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Resultado de buscar una fila de Friendship, por pareja o por id.
    /// </summary>
    public class FriendshipRecordLookup
    {
        /// <summary>Si se encontró, no existe o la base de datos falló.</summary>
        public FriendLookupStatus Status { get; set; } = FriendLookupStatus.DataFailure;

        /// <summary>La fila encontrada; null si no se encontró.</summary>
        public FriendshipRecord Record { get; set; }
    }
}
