namespace TLMG.DataAccess.Repositories
{
    public interface IPlayerRepository
    {
        bool ExistsByEmail(string email);
        bool ExistsByNickname(string nickname);
        void CreatePlayer(string nickname, string email, string passwordHash);
    }
}