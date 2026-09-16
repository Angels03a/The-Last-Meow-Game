using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace TLMG.Views
{
    public partial class LoginScreen : UserControl
    {
        private static readonly string[] _dominiosPermitidos = { "outlook.com", "hotmail.com", "live.com", "gmail.com", "uv.mx" };

        public LoginScreen()
        {
            InitializeComponent();
        }

        private void TxtCorreo_LostFocus(object sender, RoutedEventArgs e)
        {
            string mensajeError = ValidarCorreo(txtCorreo.Text);
            lblErrorCorreo.Text = mensajeError;
            lblErrorCorreo.Visibility = string.IsNullOrEmpty(mensajeError) ? Visibility.Collapsed : Visibility.Visible;
        }

        private string ValidarCorreo(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return (string)Application.Current.Resources["Login_ErrorCorreoObligatorio"];

            if (Regex.IsMatch(correo, @"[%&]"))
                return (string)Application.Current.Resources["Login_ErrorCorreoCaracteres"];

            if (!correo.Contains("@") || !correo.Contains("."))
                return (string)Application.Current.Resources["Login_ErrorCorreoFormato"];

            string dominio = correo.Substring(correo.IndexOf('@') + 1).ToLower();
            bool dominioValido = false;

            foreach (string d in _dominiosPermitidos)
            {
                if (dominio == d)
                {
                    dominioValido = true;
                    break;
                }
            }

            if (!dominioValido)
                return (string)Application.Current.Resources["Login_ErrorCorreoDominio"];

            return null;
        }

        private void BtnMostrarContrasena_Click(object sender, RoutedEventArgs e)
        {
            if (txtContrasenaVisible.Visibility == Visibility.Collapsed)
            {
                txtContrasenaVisible.Text = pwdContrasena.Password;
                txtContrasenaVisible.Visibility = Visibility.Visible;
                pwdContrasena.Visibility = Visibility.Collapsed;
            }
            else
            {
                pwdContrasena.Password = txtContrasenaVisible.Text;
                pwdContrasena.Visibility = Visibility.Visible;
                txtContrasenaVisible.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnIniciarSesion_Click(object sender, RoutedEventArgs e)
        {
            string mensajeErrorCorreo = ValidarCorreo(txtCorreo.Text);
            string contrasena = pwdContrasena.Visibility == Visibility.Visible ? pwdContrasena.Password : txtContrasenaVisible.Text;

            if (mensajeErrorCorreo != null || string.IsNullOrWhiteSpace(contrasena))
            {
                MessageBox.Show((string)Application.Current.Resources["Login_ErrorCamposIncompletos"]);
                return;
            }

            // TODO: conectar con el servidor (TCP) y validar credenciales reales (CU-03) cuando esa capa exista.
            MessageBox.Show((string)Application.Current.Resources["Login_MensajeExito"]);
        }

        private void BtnJugarComoInvitado_Click(object sender, RoutedEventArgs e)
        {
            // TODO: navegar a GUI-MenúPrincipal (modo invitado) cuando esa pantalla exista.
            MessageBox.Show("Modo invitado: pantalla de Menú Principal aún no construida.");
        }

        private void BtnOlvideContrasena_Click(object sender, RoutedEventArgs e)
        {
            // TODO: navegar a ForgotPasswordScreen cuando exista (siguiente en el orden de construcción).
            MessageBox.Show("Pantalla de recuperación de contraseña aún no construida.");
        }

        private void BtnTabCrearCuenta_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Esta pantalla se construirá en una próxima iteración.");
        }
    }
}
