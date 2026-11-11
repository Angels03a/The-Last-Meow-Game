namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Resultado de buscar un jugador o una amistad en el repositorio.
    /// </summary>
    public enum FriendLookupStatus
    {
        /// <summary>La búsqueda encontró lo pedido.</summary>
        Found,

        /// <summary>La consulta funcionó, pero no existe lo pedido.</summary>
        NotFound,

        /// <summary>La base de datos falló; el detalle ya quedó en el log.</summary>
        DataFailure
    }
}
