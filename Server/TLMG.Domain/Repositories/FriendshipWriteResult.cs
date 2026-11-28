namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Resultado de crear una solicitud de amistad: el estado de la escritura y el id de la fila creada.
    /// </summary>
    public class FriendshipWriteResult
    {
        /// <summary>Estado de la escritura.</summary>
        public FriendshipWriteStatus Status { get; set; } = FriendshipWriteStatus.DataFailure;

        /// <summary>Id de la fila de Friendship creada; 0 si no se creó.</summary>
        public int FriendshipId { get; set; }
    }
}
