using System.Windows;
using System.Windows.Controls;

namespace TLMG.Views
{
    public partial class ProfileScreen : LocalizableScreen
    {
        private const int MinNicknameLength = 3;
        private const int MaxNicknameLength = 12;

        private const int DaysSincePasswordChange = 45;

        private bool _isEditingAvatar;
        private bool _isEditingNickname;

        public ProfileScreen()
        {
            InitializeComponent();
            _isEditingAvatar = false;
            _isEditingNickname = false;
        }

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
            ToggleAvatarEditMode(_isEditingAvatar);
        }

        private void BtnSelectAvatar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button buttonSender && buttonSender.Tag != null)
            {
                lblCurrentAvatar.Text = buttonSender.Tag.ToString();
                _isEditingAvatar = false;
                ToggleAvatarEditMode(_isEditingAvatar);
            }
        }

        private void BtnEditNickname_Click(object sender, RoutedEventArgs e)
        {
            _isEditingNickname = true;
            txtEditNickname.Text = lblDisplayNickname.Text;
            ToggleNicknameEditMode(_isEditingNickname);
        }

        private void BtnSaveNickname_Click(object sender, RoutedEventArgs e)
        {
            bool isNicknameValid = ValidateNickname(txtEditNickname.Text);

            if (isNicknameValid)
            {
                lblDisplayNickname.Text = txtEditNickname.Text;
                lblHeaderUsername.Text = txtEditNickname.Text;
                _isEditingNickname = false;
                ToggleNicknameEditMode(_isEditingNickname);
            }
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navegar a la pantalla ChangePasswordScreen cuando esté implementada.
        }

        private void ToggleAvatarEditMode(bool isEditing)
        {
            if (isEditing)
            {
                grdStats.Visibility = Visibility.Collapsed;
                grdAvatarSelection.Visibility = Visibility.Visible;
            }
            else
            {
                grdStats.Visibility = Visibility.Visible;
                grdAvatarSelection.Visibility = Visibility.Collapsed;
            }
        }

        private void ToggleNicknameEditMode(bool isEditing)
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
            bool isTooShort = inputNickname.Length < MinNicknameLength;
            bool isTooLong = inputNickname.Length > MaxNicknameLength;

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