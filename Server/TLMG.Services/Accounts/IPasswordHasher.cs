namespace TLMG.Services.Accounts
{
    /// <summary>
    /// Calcula y verifica el hash de las contraseñas; solo el servidor lo usa (D-44).
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>Calcula el hash de una contraseña con una sal aleatoria nueva.</summary>
        /// <param name="password">Contraseña en texto plano.</param>
        /// <returns>El valor a guardar, con el formato versionado v1$sal$hash.</returns>
        string HashPassword(string password);

        /// <summary>Comprueba una contraseña contra el valor guardado.</summary>
        /// <param name="password">Contraseña en texto plano.</param>
        /// <param name="storedHash">Valor guardado en la base de datos.</param>
        /// <returns>true si coincide; false si no coincide o el valor guardado no tiene el formato v1.</returns>
        bool VerifyPassword(string password, string storedHash);
    }
}