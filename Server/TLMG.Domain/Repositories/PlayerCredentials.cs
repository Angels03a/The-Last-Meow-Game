namespace TLMG.Domain.Repositories
{
    public class PlayerCredentials
    {
        public CredentialsLookupStatus Status { get; set; } = CredentialsLookupStatus.DataFailure;

        public int UserId { get; set; }

        public string Nickname { get; set; }

        public string PasswordHash { get; set; }

        public string State { get; set; }
    }
}