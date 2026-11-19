namespace TLMG.Client.Navigation
{
    /// <summary>
    /// Carries the email and the already verified code to the new-password screen; the server checks the code
    /// again when the password is saved.
    /// </summary>
    public class NewPasswordNavigationParameter
    {
        public NewPasswordNavigationParameter(string email, string verifiedCode)
        {
            Email = email;
            VerifiedCode = verifiedCode;
        }

        public string Email { get; }

        public string VerifiedCode { get; }
    }
}