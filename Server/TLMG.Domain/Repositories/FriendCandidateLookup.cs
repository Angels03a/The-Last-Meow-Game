namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Resultado de buscar por nickname al jugador al que se quiere solicitar amistad.
    /// </summary>
    public class FriendCandidateLookup
    {
        /// <summary>Si se encontró, no existe o la base de datos falló.</summary>
        public FriendLookupStatus Status { get; set; }

        /// <summary>Id de la cuenta encontrada; 0 si no se encontró.</summary>
        public int UserId { get; set; }

        /// <summary>Nickname tal como está guardado; null si no se encontró.</summary>
        public string Nickname { get; set; }
    }
}
