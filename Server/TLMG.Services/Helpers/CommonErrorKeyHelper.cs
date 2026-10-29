namespace TLMG.Services.Helpers
{
    /// <summary>
    /// Claves de error que varias pantallas comparten; llevan el prefijo Common_ (Estándar §7.3).
    /// </summary>
    public static class CommonErrorKeyHelper
    {
        /// <summary>El token no pertenece a una sesión vigente.</summary>
        public const string InvalidSessionErrorKey = "Common_InvalidSessionError";

        /// <summary>Fallo inesperado del servidor, incluidos los fallos de la base de datos.</summary>
        public const string UnexpectedErrorKey = "Common_UnexpectedError";

        /// <summary>La sala ya tiene el máximo de jugadores.</summary>
        public const string RoomFullErrorKey = "Common_RoomFullError";
    }
}