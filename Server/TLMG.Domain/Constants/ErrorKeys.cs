namespace TLMG.Domain.Constants
{
    public static class ErrorKeys
    {
        public static class Common
        {
            public const string LogOutActiveMatch = "Common_LogOutActiveMatchError";

            public const string InvalidSession = "Common_InvalidSessionError";
            public const string Unexpected = "Common_UnexpectedError";
            public const string RoomFull = "Common_RoomFullError";
            public const string OutOfMemory = "Common_OutOfMemoryError";
            public const string PermissionDenied = "Common_PermissionDeniedError";
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
            public const string CardsPerPlayerNotValid = "Lobby_CardsPerPlayerNotValidError";
            public const string PlayerDisconnectedFormat = "Lobby_PlayerDisconnectedMessageFormat";
        }

        public static class Leaderboard
        {
            public const string ConnectionError = "Leaderboard_ConnectionErrorMessage";
            public const string Empty = "Leaderboard_EmptyMessage";
        }

        public static class CreateAccount
        {
            public const string IncompleteFields = "CreateAccount_IncompleteFieldsError";
            public const string NicknameFormat = "CreateAccount_NicknameFormatError";
            public const string EmailFormat = "CreateAccount_EmailFormatError";
            public const string PasswordPolicy = "CreateAccount_PasswordPolicyError";
            public const string Terms = "CreateAccount_TermsError";
            public const string DataModel = "CreateAccount_DataModelError";
            public const string EmailTaken = "CreateAccount_EmailTakenError";
            public const string NicknameTaken = "CreateAccount_NicknameTakenError";
        }

        public static class Verification
        {
            public const string IncorrectCode = "Verification_IncorrectCodeError";
            public const string ExpiredCode = "Verification_ExpiredCodeError";
            public const string AttemptsExhausted = "Verification_AttemptsExhaustedError";
        }

        public static class Login
        {
            public const string IncompleteFields = "Login_IncompleteFieldsError";
            public const string EmailFormat = "Login_EmailFormatError";
            public const string InvalidCredentials = "Login_InvalidCredentialsError";
            public const string DataModel = "Login_DataModelError";
            public const string GuestUnavailable = "Login_GuestUnavailableError";
        }

        public static class Friends
        {
            public const string CannotInvite = "Friends_CannotInviteError";
            public const string InvitationNotValid = "Friends_InvitationNotValidError";
            public const string NicknameFormat = "Friends_NicknameFormatError";
            public const string UserNotFound = "Friends_UserNotFoundError";
            public const string CannotRequestSelf = "Friends_CannotRequestSelfError";
            public const string AlreadyFriends = "Friends_AlreadyFriendsError";
            public const string RequestAlreadySent = "Friends_RequestAlreadySentError";
            public const string RequestNotValid = "Friends_RequestNotValidError";
        }
        public static class Profile
        {
            public const string NicknameFormat = "Profile_NicknameFormatError";
            public const string NicknameTaken = "Profile_NicknameTakenError";
            public const string AvatarNotValid = "Profile_AvatarNotValidError";
            public const string DataModel = "Profile_DataModelError";
        }
        public static class Password
        {
            public const string PolicyNotMet = "ChangePassword_PolicyError";
            public const string CurrentPasswordIncorrect = "ChangePassword_CurrentPasswordError";
            public const string SamePassword = "ChangePassword_SamePasswordError";
            public const string EmailFormat = "ForgotPassword_EmailFormatError";
            public const string DataModel = "Password_DataModelError";
        }
    }
}