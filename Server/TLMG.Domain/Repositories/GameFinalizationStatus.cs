namespace TLMG.Domain.Repositories
{
    public enum GameFinalizationStatus
    {
        DataFailure,
        Saved,
        AlreadySaved,
        InvalidFinalization,
        FinalizationConflict
    }
}
