namespace TLMG.Domain.Repositories
{
    public enum GameRegistrationStatus
    {
        Created,
        AlreadyRegistered,
        InvalidRegistration,
        RegisteredPlayerNotFound,
        DataFailure,
        RegistrationConflict
    }
}
