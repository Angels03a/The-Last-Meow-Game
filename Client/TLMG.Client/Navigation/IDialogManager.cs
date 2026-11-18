using TLMG.Client.Models;

namespace TLMG.Client.Navigation
{
    /// <summary>
    /// Diálogos que los ViewModels piden sin conocer las vistas. Reciben claves de recurso, nunca textos: la
    /// traducción al idioma activo la hace la implementación (D-64).
    /// </summary>
    public interface IDialogManager
    {
        /// <summary>Muestra un error al jugador.</summary>
        /// <param name="errorKey">ErrorKey recibida del servidor o clave de recurso del cliente.</param>
        void ShowError(string errorKey);

        /// <summary>Informa al jugador de algo que salió bien; solo tiene el botón Aceptar.</summary>
        /// <param name="messageKey">Clave de recurso del mensaje.</param>
        void ShowMessage(string messageKey);

        /// <summary>Pide al jugador que confirme o cancele una acción.</summary>
        /// <param name="messageKey">Clave de recurso del mensaje.</param>
        /// <param name="titleKey">Clave de recurso del título.</param>
        /// <returns><c>true</c> si el jugador confirmó; <c>false</c> si canceló.</returns>
        bool Confirm(string messageKey, string titleKey);

        /// <summary>Muestra una confirmación personalizada y conserva la respuesta elegida.</summary>
        DialogResult ConfirmWithResult(string messageKey, string titleKey);
    }
}