namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Resultado de escribir una solicitud de amistad en el repositorio.
    /// </summary>
    public enum FriendshipWriteStatus
    {
        /// <summary>La escritura se aplicó.</summary>
        Applied,

        /// <summary>Ya existe una fila de la pareja: otra solicitud ganó la carrera.</summary>
        DuplicatePair,

        /// <summary>La fila ya no estaba en el estado esperado o no es del receptor; no se cambió nada.</summary>
        StateChanged,

        /// <summary>La base de datos falló; el detalle ya quedó en el log.</summary>
        DataFailure
    }
}
