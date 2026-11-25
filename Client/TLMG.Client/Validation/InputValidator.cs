using TLMG.Client.Constants;

namespace TLMG.Client.Validation
{
    public static class InputValidator
    {
        private const int MinimumCharacterIndex = 0;
        private const int EmptyLength = 0;
        private const char AtSymbol = '@';

        private const string NicknameFormatErrorKey = "CreateAccount_NicknameFormatError";
        private const string NicknameOffensiveErrorKey = "CreateAccount_NicknameOffensiveError";
        private const string EmailRequiredSuffix = "_EmailRequiredError";
        private const string EmailCharactersSuffix = "_EmailCharactersError";
        private const string EmailFormatSuffix = "_EmailFormatError";
        private const string EmailDomainSuffix = "_EmailDomainError";
        private const string CreateAccountPrefix = "CreateAccount";
        private const string CreateAccountPasswordPolicyErrorKey = "CreateAccount_PasswordPolicyError";
        private const string PolicySuffix = "_PolicyError";
        private const string MismatchSuffix = "_MismatchError";
        private const string ChatEmptyErrorKey = "Lobby_ChatEmptyError";
        private const string ChatTooLongErrorKey = "Lobby_ChatTooLongError";
        private const string ChatCharactersErrorKey = "Lobby_ChatCharactersError";
        private const string ReportCommentTooLongErrorKey = "Moderation_CommentTooLongError";
        private const string ReportCommentCharactersErrorKey = "Moderation_CommentCharactersError";

        public static bool CanAcceptTextInput(string proposedText, ValidationInputType inputType)
        {
            bool isAccepted = true;

            if (proposedText == null)
            {
                isAccepted = false;
            }
            else if (proposedText.Length > EmptyLength)
            {
                switch (inputType)
                {
                    case ValidationInputType.Nickname:
                        isAccepted = proposedText.Length <= ValidationConstants.NicknameMaximumLength
                            && ValidationConstants.NicknameFormatRegex.IsMatch(proposedText);
                        break;
                    case ValidationInputType.Email:
                        isAccepted = proposedText.Length <= ValidationConstants.EmailMaximumLength
                            && ValidationConstants.EmailInputCharactersRegex.IsMatch(proposedText);
                        break;
                    case ValidationInputType.PasswordVisible:
                        isAccepted = proposedText.Length <= ValidationConstants.PasswordMaximumLength
                            && !ContainsSurrogateOrControl(proposedText);
                        break;
                    case ValidationInputType.DigitsOnly:
                        isAccepted = proposedText.Length <= ValidationConstants.VerificationDigitMaximumLength
                            && ValidationConstants.DigitsOnlyRegex.IsMatch(proposedText);
                        break;
                    case ValidationInputType.ChatMessage:
                        isAccepted = proposedText.Length <= ValidationConstants.ChatMessageMaximumLength
                            && AreAllCharactersInWhitelist(proposedText, ValidationConstants.AllowedChatCharacters);
                        break;
                    case ValidationInputType.ReportComment:
                        isAccepted = proposedText.Length <= ValidationConstants.ReportCommentMaximumLength
                            && !ContainsSurrogateOrControl(proposedText);
                        break;
                    default:
                        isAccepted = true;
                        break;
                }
            }

            return isAccepted;
        }

        public static string GetNicknameValidationErrorKey(string nickname)
        {
            string errorKey = null;
            string trimmedNickname = (nickname ?? string.Empty).Trim();
            bool isLengthValid = trimmedNickname.Length >= ValidationConstants.NicknameMinimumLength
                && trimmedNickname.Length <= ValidationConstants.NicknameMaximumLength;
            bool isFormatValid = isLengthValid
                && ValidationConstants.NicknameFormatRegex.IsMatch(trimmedNickname);

            if (!isFormatValid)
            {
                errorKey = NicknameFormatErrorKey;
            }
            else if (ContainsForbiddenTerm(trimmedNickname))
            {
                errorKey = NicknameOffensiveErrorKey;
            }

            return errorKey;
        }

        public static string GetEmailValidationErrorKey(string email, string screenPrefix)
        {
            string errorKey = null;
            string trimmedEmail = (email ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(trimmedEmail))
            {
                errorKey = screenPrefix + EmailRequiredSuffix;
            }
            else if (!ValidationConstants.EmailInputCharactersRegex.IsMatch(trimmedEmail))
            {
                errorKey = screenPrefix + EmailCharactersSuffix;
            }
            else if (!IsEmailStructureValid(trimmedEmail))
            {
                errorKey = screenPrefix + EmailFormatSuffix;
            }
            else if (!IsAllowedEmailDomain(trimmedEmail))
            {
                errorKey = screenPrefix + EmailDomainSuffix;
            }

            return errorKey;
        }

        public static int CalculatePasswordStrength(string password)
        {
            int strengthScore = 0;

            if (!string.IsNullOrEmpty(password))
            {
                bool hasValidLength = password.Length >= ValidationConstants.PasswordMinimumLength
                    && password.Length <= ValidationConstants.PasswordMaximumLength;

                if (hasValidLength)
                {
                    strengthScore++;
                }

                if (ValidationConstants.UppercaseLetterRegex.IsMatch(password))
                {
                    strengthScore++;
                }

                if (ValidationConstants.LowercaseLetterRegex.IsMatch(password))
                {
                    strengthScore++;
                }

                if (ValidationConstants.DigitRegex.IsMatch(password))
                {
                    strengthScore++;
                }

                if (ValidationConstants.SpecialCharacterRegex.IsMatch(password))
                {
                    strengthScore++;
                }
            }

            return strengthScore;
        }

        public static string GetPasswordPolicyErrorKey(string password, string screenPrefix)
        {
            string errorKey = null;
            int strengthScore = CalculatePasswordStrength(password);

            if (strengthScore < ValidationConstants.RequiredPasswordStrengthScore)
            {
                errorKey = screenPrefix == CreateAccountPrefix
                    ? CreateAccountPasswordPolicyErrorKey
                    : screenPrefix + PolicySuffix;
            }

            return errorKey;
        }

        public static string GetPasswordConfirmationErrorKey(
            string password,
            string confirmPassword,
            string screenPrefix)
        {
            string errorKey = null;

            if (password != confirmPassword)
            {
                errorKey = screenPrefix + MismatchSuffix;
            }

            return errorKey;
        }

        public static bool IsValidVerificationCode(string code)
        {
            bool isValid = !string.IsNullOrEmpty(code)
                && code.Length == ValidationConstants.VerificationCodeLength
                && ValidationConstants.DigitsOnlyRegex.IsMatch(code);

            return isValid;
        }

        public static string GetChatMessageValidationErrorKey(string messageText)
        {
            string errorKey = null;
            string trimmedMessage = (messageText ?? string.Empty).Trim();

            if (trimmedMessage.Length < ValidationConstants.ChatMessageMinimumLength)
            {
                errorKey = ChatEmptyErrorKey;
            }
            else if (trimmedMessage.Length > ValidationConstants.ChatMessageMaximumLength)
            {
                errorKey = ChatTooLongErrorKey;
            }
            else if (!AreAllCharactersInWhitelist(trimmedMessage, ValidationConstants.AllowedChatCharacters))
            {
                errorKey = ChatCharactersErrorKey;
            }

            return errorKey;
        }

        public static string GetReportCommentValidationErrorKey(string commentText)
        {
            string errorKey = null;
            string trimmedComment = (commentText ?? string.Empty).Trim();

            if (trimmedComment.Length > ValidationConstants.ReportCommentMaximumLength)
            {
                errorKey = ReportCommentTooLongErrorKey;
            }
            else if (ContainsSurrogateOrControl(trimmedComment))
            {
                errorKey = ReportCommentCharactersErrorKey;
            }

            return errorKey;
        }

        private static bool IsEmailStructureValid(string trimmedEmail)
        {
            bool isLengthValid = trimmedEmail.Length >= ValidationConstants.EmailMinimumLength
                && trimmedEmail.Length <= ValidationConstants.EmailMaximumLength;
            bool isStructureValid = isLengthValid
                && ValidationConstants.EmailStructureRegex.IsMatch(trimmedEmail);

            return isStructureValid;
        }

        private static bool IsAllowedEmailDomain(string trimmedEmail)
        {
            bool isDomainAllowed = false;
            int atIndex = trimmedEmail.LastIndexOf(AtSymbol);

            if (atIndex > MinimumCharacterIndex && atIndex < trimmedEmail.Length - 1)
            {
                string domain = trimmedEmail.Substring(atIndex + 1).ToLowerInvariant();

                foreach (string allowedDomain in ValidationConstants.AllowedEmailDomains)
                {
                    if (domain == allowedDomain)
                    {
                        isDomainAllowed = true;
                    }
                }
            }

            return isDomainAllowed;
        }

        private static bool ContainsForbiddenTerm(string trimmedNickname)
        {
            bool hasForbiddenTerm = false;
            string normalizedNickname = trimmedNickname.ToLowerInvariant();

            foreach (string forbiddenTerm in ValidationConstants.ForbiddenNicknameTerms)
            {
                if (normalizedNickname.Contains(forbiddenTerm))
                {
                    hasForbiddenTerm = true;
                }
            }

            return hasForbiddenTerm;
        }

        private static bool AreAllCharactersInWhitelist(string text, string whitelist)
        {
            bool areAllAllowed = true;

            foreach (char character in text)
            {
                if (whitelist.IndexOf(character) < MinimumCharacterIndex)
                {
                    areAllAllowed = false;
                }
            }

            return areAllAllowed;
        }

        private static bool ContainsSurrogateOrControl(string text)
        {
            bool hasDisallowedCharacter = false;

            foreach (char character in text)
            {
                bool isDisallowed = char.IsSurrogate(character)
                    || (char.IsControl(character) && character != '\r' && character != '\n');

                if (isDisallowed)
                {
                    hasDisallowedCharacter = true;
                }
            }

            return hasDisallowedCharacter;
        }

        public static bool CanEnterChatText(string text)
        {
            bool canEnterChatText = CanAcceptTextInput(text, ValidationInputType.ChatMessage);

            return canEnterChatText;
        }
    }
}
