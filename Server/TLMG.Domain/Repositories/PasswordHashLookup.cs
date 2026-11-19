namespace TLMG.Domain.Repositories
{
    public class PasswordHashLookup
    {
        public CredentialsLookupStatus Status { get; set; }

        public string PasswordHash { get; set; }
    }
}