namespace TLMG.Domain.Repositories
{
    public class PasswordHashLookup
    {
        public CredentialsLookupStatus Status { get; set; } = CredentialsLookupStatus.DataFailure;

        public string PasswordHash { get; set; }
    }
}