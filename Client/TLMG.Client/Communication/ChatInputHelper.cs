using TLMG.Client.Constants;
using TLMG.Client.Validation;

namespace TLMG.Client.Communication
{
    /// <summary>Checks chat input using shared client validation without replacing server validation.</summary>
    internal static class ChatInputHelper
    {
        public const int MaximumMessageCharacters = ValidationConstants.ChatMessageMaximumLength;

        /// <summary>Normalizes text before chat submission.</summary>
        internal static string TrimMessageText(string typedText)
        {
            string trimmedText = (typedText ?? string.Empty).Trim();

            return trimmedText;
        }

        /// <summary>Checks whether the trimmed text is empty.</summary>
        internal static bool IsEmptyMessage(string trimmedText)
        {
            bool isEmptyMessage = string.IsNullOrEmpty(trimmedText);

            return isEmptyMessage;
        }

        /// <summary>Checks whether the chat text can be entered, including an empty value while editing.</summary>
        public static bool CanEnterText(string messageText)
        {
            bool isTextAllowed = InputValidator.CanAcceptTextInput(messageText, ValidationInputType.ChatMessage);

            return isTextAllowed;
        }
    }
}
