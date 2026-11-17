using System;

namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Salida de un jugador de la partida (Reglas 4.0.6 y H-11): cuándo comenzó y cuándo se hizo efectiva, en UTC. En
    /// una salida voluntaria ambas coinciden; en una desconexión no recuperada, el inicio es cuándo comenzó la
    /// desconexión y la efectiva es cuándo venció su gracia. Las horas las da Services; el motor no lee el reloj.
    /// </summary>
    public sealed class PlayerExit
    {
        /// <summary>Crea una salida con su inicio y su hora efectiva.</summary>
        /// <param name="startedAtUtc">Hora UTC en que comenzó la salida o la desconexión.</param>
        /// <param name="effectiveAtUtc">Hora UTC en que la salida se hizo efectiva.</param>
        /// <exception cref="ArgumentException">Una hora no es UTC o el inicio es posterior a la efectiva.</exception>
        public PlayerExit(DateTime startedAtUtc, DateTime effectiveAtUtc)
        {
            ValidateUtc(startedAtUtc, nameof(startedAtUtc));
            ValidateUtc(effectiveAtUtc, nameof(effectiveAtUtc));

            if (startedAtUtc > effectiveAtUtc)
            {
                throw new ArgumentException("El inicio no puede ser posterior a la salida efectiva.",
                    nameof(startedAtUtc));
            }

            StartedAtUtc = startedAtUtc;
            EffectiveAtUtc = effectiveAtUtc;
        }

        /// <summary>Obtiene la hora UTC en que comenzó la salida o la desconexión.</summary>
        public DateTime StartedAtUtc { get; }

        /// <summary>Obtiene la hora UTC en que la salida se hizo efectiva.</summary>
        public DateTime EffectiveAtUtc { get; }

        /// <summary>Crea una salida inmediata, como la voluntaria: el inicio y la hora efectiva coinciden.</summary>
        /// <param name="atUtc">Hora UTC de la salida.</param>
        /// <returns>La salida inmediata.</returns>
        /// <exception cref="ArgumentException">atUtc no es una hora UTC.</exception>
        public static PlayerExit Immediate(DateTime atUtc)
        {
            return new PlayerExit(atUtc, atUtc);
        }

        /// <summary>Único validador UTC del motor; MatchState lo reutiliza con el nombre de su parámetro.</summary>
        internal static void ValidateUtc(DateTime utcTime, string parameterName)
        {
            if (utcTime.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("La hora debe ser UTC.", parameterName);
            }
        }
    }
}
