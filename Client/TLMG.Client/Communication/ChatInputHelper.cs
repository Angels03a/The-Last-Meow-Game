using TLMG.Client.Constants;
using TLMG.Client.Validation;

namespace TLMG.Client.Communication
{
    internal static class ChatInputHelper
    {
        public const int MaximumMessageCharacters = ValidationConstants.ChatMessageMaximumLength;

        internal static string TrimMessageText(string typedText)
        {
            string trimmedText = (typedText ?? string.Empty).Trim();

            return trimmedText;
        }

        internal static bool IsEmptyMessage(string trimmedText)
        {
            bool isEmptyMessage = string.IsNullOrEmpty(trimmedText);

            return isEmptyMessage;
        }

        public static bool CanEnterText(string messageText)
        {
            bool isTextAllowed = InputValidator.CanAcceptTextInput(messageText, ValidationInputType.ChatMessage);

            return isTextAllowed;
        }
    }
}
