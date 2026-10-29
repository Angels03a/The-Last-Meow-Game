namespace TLMG.DataAccess.Repositories
{
    /// <summary>
    /// Datos mínimos de un amigo aceptado; no expone la entidad de Entity Framework.
    /// </summary>
    public class AcceptedFriend
    {
        /// <summary>Identificador de la cuenta del amigo (el mismo de Friendship).</summary>
        public int UserId { get; set; }

        /// <summary>Nickname del amigo.</summary>
        public string Nickname { get; set; }
    }
}