namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Resultado de buscar las credenciales de una cuenta.
    /// </summary>
    public enum CredentialsLookupStatus
    {
        /// <summary>La cuenta existe y sus credenciales se obtuvieron.</summary>
        Found,

        /// <summary>No existe una cuenta con ese correo.</summary>
        NotFound,

        /// <summary>La consulta falló por un problema de datos; el detalle está en el log.</summary>
        DataFailure
    }
}