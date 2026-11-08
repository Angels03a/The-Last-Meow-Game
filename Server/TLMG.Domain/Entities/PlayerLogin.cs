namespace TLMG.Domain.Entities
{
    /// <summary>
    /// Estructura de datos puros para transportar la información de inicio de sesión hacia el Dominio.
    /// </summary>
    public class PlayerLogin
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}