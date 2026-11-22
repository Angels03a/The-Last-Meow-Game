using Xunit;

using TLMG.Domain.Constants;
using TLMG.Domain.Entities;
using TLMG.Domain.Validators;

namespace TLMG.Domain.Tests
{
    /// <summary>
    /// Tests of PlayerAccountRegistrationValidator. The limits are written here on purpose and not read from
    /// ValidationRules, so that changing a limit in production breaks a test: nickname 3 to 12 characters, local part
    /// of the email 6 to 30 characters, email up to 254 characters, password 12 to 128 characters.
    /// </summary>
    public class PlayerAccountRegistrationValidatorTests
    {
        private const string ValidNickname = "Player";
        private const string ValidEmail = "player@example.test";
        private const string ValidPassword = "Synthetic12!";
        private const string EmailDomain = "@example.test";
        private const string ShortEmailDomain = ".c";
        private const string PasswordPrefix = "Aa1!";
        private const int MinimumNicknameLength = 3;
        private const int MaximumNicknameLength = 12;
        private const int MinimumPasswordLength = 12;
        private const int MaximumPasswordLength = 128;
        private const int MaximumEmailLength = 254;
        private const int MaximumEmailLocalPartLength = 30;
        private const int MinimumEmailLocalPartLength = 6;
        private const int AtSignLength = 1;

        /// <summary>The nickname accepts letters, digits and underscore within 3 to 12 characters.</summary>
        /// <param name="nickname">Nickname under test.</param>
        [Theory]
        [InlineData("abc")]
        [InlineData("abcdefghijkl")]
        [InlineData("Cat_1")]
        [InlineData("___")]
        [InlineData("123")]
        [InlineData("ABC")]
        public void ValidateNickname_WithAllowedCharactersWithinRange_ReturnsEmpty(string nickname)
        {
            // Arrange

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidateNickname(nickname);

            // Assert
            Assert.Equal(string.Empty, errorKey);
        }

        /// <summary>Missing, blank, out-of-range or non-allowed nicknames are rejected with the format error.</summary>
        /// <param name="nickname">Nickname under test.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("ab")]
        [InlineData("abcdefghijklm")]
        [InlineData("Cat One")]
        [InlineData("Gatñ")]
        [InlineData("Cat!")]
        [InlineData("Cat-1")]
        [InlineData("Cat.1")]
        [InlineData("Cat\n")]
        [InlineData(" Cat")]
        public void ValidateNickname_WithInvalidNickname_ReturnsNicknameFormatError(string nickname)
        {
            // Arrange

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidateNickname(nickname);

            // Assert
            Assert.Equal(ErrorKeys.CreateAccount.NicknameFormat, errorKey);
        }

        /// <summary>The nickname length limits are exactly the ones in ValidationRules.</summary>
        /// <param name="length">Nickname length.</param>
        /// <param name="isValid">Whether that length is accepted.</param>
        [Theory]
        [InlineData(MinimumNicknameLength - 1, false)]
        [InlineData(MinimumNicknameLength, true)]
        [InlineData(MaximumNicknameLength, true)]
        [InlineData(MaximumNicknameLength + 1, false)]
        public void ValidateNickname_AtLengthBoundary_AcceptsOnlyTheRange(int length, bool isValid)
        {
            // Arrange
            string nickname = new string('n', length);

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidateNickname(nickname);

            // Assert
            Assert.Equal(isValid, errorKey == string.Empty);
        }

        /// <summary>Emails with letters, digits and dots in the local part and a dotted domain are accepted.</summary>
        /// <param name="email">Email under test.</param>
        [Theory]
        [InlineData("abcdef@example.test")]
        [InlineData("ABCDEF@example.test")]
        [InlineData("abc.def@example.test")]
        [InlineData("abc123@example.test")]
        [InlineData("abcdef@sub.example.test")]
        [InlineData("abcdef@a.b")]
        public void ValidateEmail_WithValidFormat_ReturnsEmpty(string email)
        {
            // Arrange

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidateEmail(email);

            // Assert
            Assert.Equal(string.Empty, errorKey);
        }

        /// <summary>Missing, blank and malformed emails are rejected with the email format error.</summary>
        /// <param name="email">Email under test.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("abcdef")]
        [InlineData("abcdef@")]
        [InlineData("@example.test")]
        [InlineData("abcdef@@example.test")]
        [InlineData("abcdef@example")]
        [InlineData("abcdef@.test")]
        [InlineData("abcdef@example.")]
        [InlineData("abc_def@example.test")]
        [InlineData("abc-def@example.test")]
        [InlineData("abcdef @example.test")]
        [InlineData("abcdef@exam ple.test")]
        [InlineData("abcdef@example.test\n")]
        [InlineData(" abcdef@example.test")]
        public void ValidateEmail_WithInvalidFormat_ReturnsEmailFormatError(string email)
        {
            // Arrange

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidateEmail(email);

            // Assert
            Assert.Equal(ErrorKeys.CreateAccount.EmailFormat, errorKey);
        }

        /// <summary>The local part of the email must have 6 to 30 characters.</summary>
        /// <param name="localPartLength">Length of the part before the at sign.</param>
        /// <param name="isValid">Whether that length is accepted.</param>
        [Theory]
        [InlineData(MinimumEmailLocalPartLength - 1, false)]
        [InlineData(MinimumEmailLocalPartLength, true)]
        [InlineData(MaximumEmailLocalPartLength, true)]
        [InlineData(MaximumEmailLocalPartLength + 1, false)]
        public void ValidateEmail_AtLocalPartBoundary_AcceptsOnlyTheRange(int localPartLength, bool isValid)
        {
            // Arrange
            string email = new string('a', localPartLength) + EmailDomain;

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidateEmail(email);

            // Assert
            Assert.Equal(isValid, errorKey == string.Empty);
        }

        /// <summary>The whole email accepts up to 254 characters and rejects one more.</summary>
        /// <param name="totalLength">Length of the whole email.</param>
        /// <param name="isValid">Whether that length is accepted.</param>
        [Theory]
        [InlineData(MaximumEmailLength, true)]
        [InlineData(MaximumEmailLength + 1, false)]
        public void ValidateEmail_AtTotalLengthBoundary_AcceptsOnlyTheRange(int totalLength, bool isValid)
        {
            // Arrange
            string email = BuildEmailOfLength(totalLength);

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidateEmail(email);

            // Assert
            Assert.Equal(totalLength, email.Length);
            Assert.Equal(isValid, errorKey == string.Empty);
        }

        /// <summary>A password with every required character type and 12 to 128 characters is accepted.</summary>
        /// <param name="password">Password under test.</param>
        [Theory]
        [InlineData(ValidPassword)]
        [InlineData("Aa1_aaaaaaaa")]
        [InlineData("Ññ1!aaaaaaaa")]
        public void ValidatePassword_WithAllRequiredCharacterTypes_ReturnsEmpty(string password)
        {
            // Arrange

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidatePassword(password);

            // Assert
            Assert.Equal(string.Empty, errorKey);
        }

        /// <summary>Missing, blank or incomplete passwords are rejected with the password policy error.</summary>
        /// <param name="password">Password under test.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("            ")]
        [InlineData("Short1!")]
        [InlineData("synthetic12!")]
        [InlineData("SYNTHETIC12!")]
        [InlineData("Syntheticxx!")]
        [InlineData("Synthetic123")]
        [InlineData("123456789012")]
        [InlineData("!!!!!!!!!!!!")]
        public void ValidatePassword_WithMissingOrIncompleteCharacterTypes_ReturnsPasswordPolicyError(
            string password)
        {
            // Arrange

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidatePassword(password);

            // Assert
            Assert.Equal(ErrorKeys.CreateAccount.PasswordPolicy, errorKey);
        }

        /// <summary>The password length limits are exactly the ones in ValidationRules.</summary>
        /// <param name="length">Password length, with every character type present.</param>
        /// <param name="isValid">Whether that length is accepted.</param>
        [Theory]
        [InlineData(MinimumPasswordLength - 1, false)]
        [InlineData(MinimumPasswordLength, true)]
        [InlineData(MaximumPasswordLength, true)]
        [InlineData(MaximumPasswordLength + 1, false)]
        public void ValidatePassword_AtLengthBoundary_AcceptsOnlyTheRange(int length, bool isValid)
        {
            // Arrange
            string password = PasswordPrefix + new string('x', length - PasswordPrefix.Length);

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidatePassword(password);

            // Assert
            Assert.Equal(length, password.Length);
            Assert.Equal(isValid, errorKey == string.Empty);
        }

        /// <summary>A null registration is rejected as incomplete fields.</summary>
        [Fact]
        public void ValidateAccountPlayerRegistration_WithNullRegistration_ReturnsIncompleteFieldsError()
        {
            // Arrange
            PlayerRegistration registration = null;

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidateAccountPlayerRegistration(registration);

            // Assert
            Assert.Equal(ErrorKeys.CreateAccount.IncompleteFields, errorKey);
        }

        /// <summary>A registration with valid fields and accepted terms is accepted.</summary>
        [Fact]
        public void ValidateAccountPlayerRegistration_WithValidFieldsAndAcceptedTerms_ReturnsEmpty()
        {
            // Arrange
            PlayerRegistration registration = CreateRegistration(ValidNickname, ValidEmail, ValidPassword, true);

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidateAccountPlayerRegistration(registration);

            // Assert
            Assert.Equal(string.Empty, errorKey);
        }

        /// <summary>Each invalid field of a registration is reported with the error of that field.</summary>
        /// <param name="nickname">Nickname of the registration.</param>
        /// <param name="email">Email of the registration.</param>
        /// <param name="password">Password of the registration.</param>
        /// <param name="hasAcceptedTerms">Whether the terms were accepted.</param>
        /// <param name="expectedErrorKey">Error expected for the failing rule.</param>
        [Theory]
        [InlineData(null, ValidEmail, ValidPassword, true, ErrorKeys.CreateAccount.NicknameFormat)]
        [InlineData("ab", ValidEmail, ValidPassword, true, ErrorKeys.CreateAccount.NicknameFormat)]
        [InlineData(ValidNickname, null, ValidPassword, true, ErrorKeys.CreateAccount.EmailFormat)]
        [InlineData(ValidNickname, "abcde@example.test", ValidPassword, true, ErrorKeys.CreateAccount.EmailFormat)]
        [InlineData(ValidNickname, ValidEmail, null, true, ErrorKeys.CreateAccount.PasswordPolicy)]
        [InlineData(ValidNickname, ValidEmail, "Short1!", true, ErrorKeys.CreateAccount.PasswordPolicy)]
        [InlineData(ValidNickname, ValidEmail, ValidPassword, false, ErrorKeys.CreateAccount.Terms)]
        public void ValidateAccountPlayerRegistration_WithOneInvalidField_ReturnsThatFieldError(
            string nickname,
            string email,
            string password,
            bool hasAcceptedTerms,
            string expectedErrorKey)
        {
            // Arrange
            PlayerRegistration registration = CreateRegistration(nickname, email, password, hasAcceptedTerms);

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidateAccountPlayerRegistration(registration);

            // Assert
            Assert.Equal(expectedErrorKey, errorKey);
        }

        /// <summary>When several fields fail, the nickname is reported first, then email, password and terms.</summary>
        /// <param name="nickname">Nickname of the registration.</param>
        /// <param name="email">Email of the registration.</param>
        /// <param name="password">Password of the registration.</param>
        /// <param name="hasAcceptedTerms">Whether the terms were accepted.</param>
        /// <param name="expectedErrorKey">Error of the highest-precedence failing rule.</param>
        [Theory]
        [InlineData("ab", "abcde@example.test", "Short1!", false, ErrorKeys.CreateAccount.NicknameFormat)]
        [InlineData(ValidNickname, "abcde@example.test", "Short1!", false, ErrorKeys.CreateAccount.EmailFormat)]
        [InlineData(ValidNickname, ValidEmail, "Short1!", false, ErrorKeys.CreateAccount.PasswordPolicy)]
        public void ValidateAccountPlayerRegistration_WithSeveralInvalidFields_ReportsTheFirstByPrecedence(
            string nickname,
            string email,
            string password,
            bool hasAcceptedTerms,
            string expectedErrorKey)
        {
            // Arrange
            PlayerRegistration registration = CreateRegistration(nickname, email, password, hasAcceptedTerms);

            // Act
            string errorKey = PlayerAccountRegistrationValidator.ValidateAccountPlayerRegistration(registration);

            // Assert
            Assert.Equal(expectedErrorKey, errorKey);
        }

        private static PlayerRegistration CreateRegistration(
            string nickname,
            string email,
            string password,
            bool hasAcceptedTerms)
        {
            PlayerRegistration registration = new PlayerRegistration
            {
                Nickname = nickname,
                Email = email,
                Password = password,
                HasAcceptedTerms = hasAcceptedTerms
            };

            return registration;
        }

        private static string BuildEmailOfLength(int totalLength)
        {
            string localPart = new string('a', MaximumEmailLocalPartLength);
            int domainLength = totalLength - localPart.Length - AtSignLength;
            string domain = new string('d', domainLength - ShortEmailDomain.Length) + ShortEmailDomain;

            return localPart + "@" + domain;
        }
    }
}
