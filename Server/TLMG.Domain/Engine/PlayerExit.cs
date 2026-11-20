using System;

namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Player exit times in UTC (Rule 4.0.6 and H-11). A voluntary exit starts and takes effect at the same instant.
    /// For a missed reconnection, the exit starts at disconnection and takes effect when the grace period ends.
    /// Services supplies both times. The engine never reads the clock.
    /// </summary>
    public sealed class PlayerExit
    {
        /// <summary>Creates an exit with its start and effective times.</summary>
        /// <param name="startedAtUtc">UTC time when the exit or disconnection began.</param>
        /// <param name="effectiveAtUtc">UTC time when the exit took effect.</param>
        /// <exception cref="ArgumentException">
        /// A time is outside UTC or the start is later than the effective time.
        /// </exception>
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

        /// <summary>UTC time when the exit or disconnection began.</summary>
        public DateTime StartedAtUtc { get; }

        /// <summary>UTC time when the exit took effect.</summary>
        public DateTime EffectiveAtUtc { get; }

        /// <summary>
        /// Creates an immediate exit, such as a voluntary exit, with matching start and effective times.
        /// </summary>
        /// <param name="atUtc">UTC exit time.</param>
        /// <returns>Immediate exit.</returns>
        /// <exception cref="ArgumentException">atUtc is not a UTC time.</exception>
        public static PlayerExit Immediate(DateTime atUtc)
        {
            return new PlayerExit(atUtc, atUtc);
        }

        /// <summary>Shared UTC check for the engine. MatchState supplies its own parameter name.</summary>
        internal static void ValidateUtc(DateTime utcTime, string parameterName)
        {
            if (utcTime.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("La hora debe ser UTC.", parameterName);
            }
        }
    }
}
