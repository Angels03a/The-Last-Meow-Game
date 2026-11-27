using System;

namespace TLMG.Domain.Engine
{
    public sealed class PlayerExit
    {
        private PlayerExit(DateTime startedAtUtc, DateTime effectiveAtUtc, PlayerExitCause cause)
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
            Cause = cause;
        }

        public DateTime StartedAtUtc { get; }

        public DateTime EffectiveAtUtc { get; }

        public PlayerExitCause Cause { get; }

        public static PlayerExit CreateVoluntary(DateTime atUtc)
        {
            PlayerExit exit = new PlayerExit(atUtc, atUtc, PlayerExitCause.Voluntary);

            return exit;
        }

        public static PlayerExit CreateDisconnection(DateTime startedAtUtc, DateTime effectiveAtUtc)
        {
            PlayerExit exit = new PlayerExit(startedAtUtc, effectiveAtUtc, PlayerExitCause.Disconnection);

            return exit;
        }

        public static PlayerExit CreateVotedOut(DateTime atUtc)
        {
            PlayerExit exit = new PlayerExit(atUtc, atUtc, PlayerExitCause.VotedOut);

            return exit;
        }

        public static PlayerExit CreateSanctioned(DateTime atUtc)
        {
            PlayerExit exit = new PlayerExit(atUtc, atUtc, PlayerExitCause.Sanctioned);

            return exit;
        }

        internal static void ValidateUtc(DateTime utcTime, string parameterName)
        {
            if (utcTime.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("La hora debe ser UTC.", parameterName);
            }
        }
    }
}
