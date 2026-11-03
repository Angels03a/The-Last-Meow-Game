using System.Windows;
using System.Windows.Controls;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Pantalla de perfil de usuario para visualización de estadísticas y edición de avatar o nickname.
    /// </summary>
    public partial class ProfileScreen : LocalizableScreen
    {
        private const int DaysSincePasswordChange = 45;

        private bool _isEditingAvatar;
        private bool _isEditingNickname;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ProfileScreen"/>.
        /// </summary>
        public ProfileScreen()
        {
            InitializeComponent();
            _isEditingAvatar = false;
            _isEditingNickname = false;
        }

        /// <inheritdoc />
        protected override void UpdateDynamicLabels()
        {
            string lastPasswordFormat = TryFindResource("Profile_LastPasswordChange") as string ?? "{0}";
            lblLastPasswordChange.Text = string.Format(lastPasswordFormat, DaysSincePasswordChange);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow applicationMainWindow = (MainWindow)Application.Current.MainWindow;
            applicationMainWindow.ShowScreen(new MainMenuScreen());
        }

        private void BtnEditAvatar_Click(object sender, RoutedEventArgs e)
        {
            _isEditingAvatar = !_isEditingAvatar;
            SetAvatarEditMode(_isEditingAvatar);
        }

        private void BtnSelectAvatar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button buttonSender && buttonSender.Tag != null)
            {
                lblCurrentAvatar.Text = buttonSender.Tag.ToString();
                _isEditingAvatar = false;
                SetAvatarEditMode(_isEditingAvatar);
            }
        }

        private void BtnEditNickname_Click(object sender, RoutedEventArgs e)
        {
            _isEditingNickname = true;
            txtEditNickname.Text = lblDisplayNickname.Text;
            SetNicknameEditMode(_isEditingNickname);
        }

        private void BtnSaveNickname_Click(object sender, RoutedEventArgs e)
        {
            bool isNicknameValid = ValidateNickname(txtEditNickname.Text);

            if (isNicknameValid)
            {
                lblDisplayNickname.Text = txtEditNickname.Text;
                lblHeaderUsername.Text = txtEditNickname.Text;
                _isEditingNickname = false;
                SetNicknameEditMode(_isEditingNickname);
            }
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navegar a ChangePasswordScreen cuando esté implementada. (BL-046).
        }

        private void SetAvatarEditMode(bool isEditing)
        {
            if (isEditing)
            {
                grdStats.Visibility = Visibility.Collapsed;
                pnlAvatarSelection.Visibility = Visibility.Visible;
            }
            else
            {
                grdStats.Visibility = Visibility.Visible;
                pnlAvatarSelection.Visibility = Visibility.Collapsed;
            }
        }

        private void SetNicknameEditMode(bool isEditing)
        {
            if (isEditing)
            {
                lblDisplayNickname.Visibility = Visibility.Collapsed;
                btnEditNickname.Visibility = Visibility.Collapsed;

                txtEditNickname.Visibility = Visibility.Visible;
                btnSaveNickname.Visibility = Visibility.Visible;
                lblNicknameHint.Visibility = Visibility.Visible;
            }
            else
            {
                lblDisplayNickname.Visibility = Visibility.Visible;
                btnEditNickname.Visibility = Visibility.Visible;

                txtEditNickname.Visibility = Visibility.Collapsed;
                btnSaveNickname.Visibility = Visibility.Collapsed;
                lblNicknameHint.Visibility = Visibility.Collapsed;
                lblNicknameError.Visibility = Visibility.Hidden;
            }
        }

        private bool ValidateNickname(string inputNickname)
        {
            bool isValidResult;
            bool isTooShort = inputNickname.Length < ValidationHelper.MinNicknameLengthCharacters;
            bool isTooLong = inputNickname.Length > ValidationHelper.MaxNicknameLengthCharacters;

            if (string.IsNullOrWhiteSpace(inputNickname) || isTooShort || isTooLong)
            {
                lblNicknameError.Text = TryFindResource("CreateAccount_NicknameFormatError") as string;
                lblNicknameError.Visibility = Visibility.Visible;
                isValidResult = false;
            }
            else
            {
                lblNicknameError.Visibility = Visibility.Hidden;
                isValidResult = true;
            }

            return isValidResult;
        }
    }
}