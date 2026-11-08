namespace TLMG.Domain.Entities
{
    /// <summary>
    /// Estructura de datos puros para transportar la información de registro hacia el Dominio.
    /// </summary>
    public class PlayerRegistration
    {
        public string Nickname { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool HasAcceptedTerms { get; set; }
    }
}