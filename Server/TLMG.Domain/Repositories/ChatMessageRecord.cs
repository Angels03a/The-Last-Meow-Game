using System;

namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Datos de un mensaje de chat aceptado que se conservan como evidencia para reportes; no expone entidades de
    /// Entity Framework.
    /// </summary>
    public class ChatMessageRecord
    {
        /// <summary>Identificador de la cuenta emisora (id de User).</summary>
        public int UserId { get; set; }

        /// <summary>Ámbito del mensaje: sala de espera o partida.</summary>
        public ChatMessageScope Scope { get; set; }

        /// <summary>Identificador de la partida persistida; null mientras la partida no se guarde.</summary>
        public int? GameId { get; set; }

        /// <summary>Número de la sala en memoria; informativo, se reinicia con el servidor.</summary>
        public int? RoomNumber { get; set; }

        /// <summary>Texto ya validado y normalizado.</summary>
        public string Text { get; set; }

        /// <summary>Hora UTC del servidor en que se aceptó el mensaje.</summary>
        public DateTime SentAtUtc { get; set; }
    }
}