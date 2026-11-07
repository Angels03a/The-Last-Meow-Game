namespace TLMG.Domain.Entities
{
    public class Player
    {
        public string Nickname { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }

        public Player(string nickname, string email, string passwordHash)
        {
            Nickname = nickname;
            Email = email;
            PasswordHash = passwordHash;
        }
    }
}