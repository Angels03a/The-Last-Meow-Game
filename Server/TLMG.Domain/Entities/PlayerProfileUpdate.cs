namespace TLMG.Domain.Entities
{
    /// <summary>Pure data used to move the requested profile changes into the domain.</summary>
    public class PlayerProfileUpdate
    {
        public string Nickname { get; set; }

        public string AvatarKey { get; set; }
    }
}