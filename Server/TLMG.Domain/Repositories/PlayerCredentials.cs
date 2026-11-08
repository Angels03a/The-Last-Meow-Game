namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Datos mínimos de una cuenta para validar un inicio de sesión; no expone la entidad de Entity Framework.
    /// </summary>
    public class PlayerCredentials
    {
        /// <summary>Indica si la cuenta se encontró, no existe o la consulta falló.</summary>
        public CredentialsLookupStatus Status { get; set; }

        /// <summary>Identificador de la cuenta; solo tiene valor cuando Status es Found.</summary>
        public int UserId { get; set; }

        /// <summary>Nickname de la cuenta; solo tiene valor cuando Status es Found.</summary>
        public string Nickname { get; set; }

        /// <summary>Hash guardado de la contraseña; solo tiene valor cuando Status es Found.</summary>
        public string PasswordHash { get; set; }

        /// <summary>Estado de la cuenta; solo tiene valor cuando Status es Found.</summary>
        public string State { get; set; }
    }
}