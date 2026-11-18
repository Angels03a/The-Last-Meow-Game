namespace TLMG.Client.Models
{
    /// <summary>Respuesta semántica de un diálogo general.</summary>
    public enum DialogResult { Cancel, Accept, Yes, No }

    /// <summary>Contenido localizado de un diálogo general.</summary>
    public sealed class GameDialog
    {
        public string MessageKey { get; set; }
        public string TitleKey { get; set; }
    }

    /// <summary>Construye los datos de presentación de un diálogo.</summary>
    public static class GameDialogFactory
    {
        public static GameDialog CreateConfirmation(string messageKey, string titleKey)
        {
            return new GameDialog { MessageKey = messageKey, TitleKey = titleKey };
        }
    }
}
