namespace TLMG.Client.Communication
{
    /// <summary>Checks allowed chat characters without replacing server validation.</summary>
    internal static class ChatInputHelper
    {
        /// <summary>Maximum number of characters per message.</summary>
        public const int MaximumMessageCharacters = 160;
        public const int FirstValidIndex = 0;
        private const string AllowedCharacters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÁÉÍÓÚáéíóúÑñ0123456789 ,?¿";

        /// <summary>Trims spaces from the text entered in chat.</summary>
        /// <param name="typedText">Text from the chat box; may be null.</param>
        /// <returns>The trimmed text, or an empty string if it was null.</returns>
        internal static string TrimMessageText(string typedText)
        {
            string trimmedText = (typedText ?? string.Empty).Trim();

            return trimmedText;
        }

        /// <summary>Checks whether the trimmed text is empty.</summary>
        /// <param name="trimmedText">Text returned by <see cref="TrimMessageText"/>.</param>
        /// <returns>true if the message is empty; otherwise, false.</returns>
        internal static bool IsEmptyMessage(string trimmedText)
        {
            bool isEmptyMessage = string.IsNullOrEmpty(trimmedText);

            return isEmptyMessage;
        }

        /// <summary>Checks the length and characters, stopping at the first disallowed character.</summary>
        /// <param name="messageText">Text that would appear in the chat box.</param>
        /// <returns>Whether the text can be entered, including an empty value while editing.</returns>
        public static bool CanEnterText(string messageText)
        {
            bool isTextAllowed = messageText != null && messageText.Length <= MaximumMessageCharacters;
            if (isTextAllowed)
            {
                foreach (char character in messageText)
                {
                    if (AllowedCharacters.IndexOf(character) < FirstValidIndex)
                    {
                        isTextAllowed = false;
                        break;
                    }
                }
            }
            return isTextAllowed;
        }
    }
}
