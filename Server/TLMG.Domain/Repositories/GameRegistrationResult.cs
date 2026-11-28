namespace TLMG.Domain.Repositories
{
    public sealed class GameRegistrationResult
    {
        public GameRegistrationStatus GameRegistrationStatus { get; set; }
        public int GameId { get; set; }
    }
}
