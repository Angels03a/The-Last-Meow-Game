namespace TLMG.Domain.Constants
{
    /// <summary>
    /// Catálogo centralizado de claves de error que el servidor envía al cliente.
    /// </summary>
    public static class ErrorKeys
    {
        public static class Common
        {
            public const string InvalidSession = "Common_InvalidSessionError";
            public const string Unexpected = "Common_UnexpectedError";
            public const string RoomFull = "Common_RoomFullError";
        }

        public static class Lobby
        {
            public const string NotInRoom = "Lobby_NotInRoomError";
            public const string MatchAlreadyStarted = "Lobby_MatchAlreadyStartedError";
            public const string ChatEmpty = "Lobby_ChatEmptyError";
            public const string ChatTooLong = "Lobby_ChatTooLongError";
            public const string ChatCharacters = "Lobby_ChatCharactersError";
            public const string ChatRateLimit = "Lobby_ChatRateLimitError";
            public const string ChatNotSubscribed = "Lobby_ChatNotSubscribedError";
        }

        public static class CreateAccount
        {
            public const string NicknameFormat = "CreateAccount_NicknameFormatError";
            public const string EmailFormat = "CreateAccount_EmailFormatError";
            public const string PasswordPolicy = "CreateAccount_PasswordPolicyError";
            public const string Terms = "CreateAccount_TermsError";
        }
    }
}