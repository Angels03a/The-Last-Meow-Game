namespace TLMG.DataAccess.Repositories
{
    public interface IPlayerRepository
    {
        bool PlayerExistsByEmail(string email);
        bool PlayerExistsByNickname(string nickname);
        void CreatePlayer(string nickname, string email, string passwordHash);
    }
}