namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Resultado de comprobar si ya existe una cuenta; separa "no existe" de un fallo de datos.
    /// </summary>
    public enum PlayerExistenceLookupStatus
    {
        /// <summary>Ya existe una cuenta con el valor buscado.</summary>
        Exists,

        /// <summary>No existe ninguna cuenta con el valor buscado.</summary>
        NotFound,

        /// <summary>La consulta falló por un problema de datos; el detalle está en el log.</summary>
        DataFailure
    }
}
