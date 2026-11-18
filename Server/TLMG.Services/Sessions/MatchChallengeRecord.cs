using System.Collections.Generic;

using TLMG.Contracts.DataContracts;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Desenlace del último reto de una partida. El motor pasa de inmediato a la siguiente ronda o termina la
    /// partida y olvida el reto, así que el registro lo guarda aquí para que todos lo vean. Vive hasta que se abre
    /// la siguiente apuesta o se coloca la siguiente carta. Solo se modifica bajo _sessionsLock.
    /// </summary>
    internal sealed class MatchChallengeRecord
    {
        private readonly List<MatchRevealedCardView> _revealedCards = new List<MatchRevealedCardView>();

        private MatchChallengeRecord(int bettorUserId, int promisedCount)
        {
            BettorUserId = bettorUserId;
            PromisedCount = promisedCount;
            Outcome = MatchChallengeOutcome.InProgress;
            PunishmentResolution = MatchPunishmentResolution.None;
        }

        /// <summary>Obtiene la cuenta del apostador.</summary>
        internal int BettorUserId { get; }

        /// <summary>Obtiene las cartas que prometió voltear.</summary>
        internal int PromisedCount { get; }

        /// <summary>Obtiene el estado o resultado del reto.</summary>
        internal MatchChallengeOutcome Outcome { get; private set; }

        /// <summary>Obtiene las cartas volteadas, en orden, con el dueño de su pila.</summary>
        internal IReadOnlyList<MatchRevealedCardView> RevealedCards => _revealedCards.AsReadOnly();

        /// <summary>Obtiene la cuenta del castigado; 0 si el reto no falló.</summary>
        internal int PunishedUserId { get; private set; }

        /// <summary>Obtiene la cuenta de quien eligió o debe elegir; 0 si el reto no falló.</summary>
        internal int PunishmentChooserUserId { get; private set; }

        /// <summary>Obtiene cómo va o cómo terminó el castigo.</summary>
        internal MatchPunishmentResolution PunishmentResolution { get; private set; }

        /// <summary>Crea el registro de un reto que empieza.</summary>
        /// <param name="bettorUserId">Cuenta del apostador.</param>
        /// <param name="promisedCount">Cartas que prometió voltear.</param>
        /// <returns>El registro, en curso y sin cartas volteadas.</returns>
        internal static MatchChallengeRecord Begin(int bettorUserId, int promisedCount)
        {
            MatchChallengeRecord challengeRecord = new MatchChallengeRecord(bettorUserId, promisedCount);

            return challengeRecord;
        }

        /// <summary>Anota una carta volteada.</summary>
        /// <param name="pileOwnerUserId">Dueño de la pila de la que se volteó.</param>
        /// <param name="card">Tipo de la carta volteada.</param>
        internal void RecordReveal(int pileOwnerUserId, MatchCardKind card)
        {
            _revealedCards.Add(new MatchRevealedCardView { PileOwnerUserId = pileOwnerUserId, Card = card });
        }

        /// <summary>Marca el reto como cumplido; no hay castigo.</summary>
        internal void MarkSucceeded()
        {
            Outcome = MatchChallengeOutcome.Succeeded;
        }

        /// <summary>Marca el reto como fallido y anota el castigo resultante.</summary>
        /// <param name="chooserUserId">Quien elige la carta que se pierde.</param>
        /// <param name="punishedUserId">Quien la pierde.</param>
        /// <param name="resolution">Pending si falta elegir; SingleLifeNoChoice si se eliminó sin elegir.</param>
        internal void MarkFailed(int chooserUserId, int punishedUserId, MatchPunishmentResolution resolution)
        {
            Outcome = MatchChallengeOutcome.Failed;
            PunishmentChooserUserId = chooserUserId;
            PunishedUserId = punishedUserId;
            PunishmentResolution = resolution;
        }

        /// <summary>Anota cómo terminó un castigo que estaba pendiente.</summary>
        /// <param name="resolution">Resultado final del castigo.</param>
        internal void ResolvePunishment(MatchPunishmentResolution resolution)
        {
            PunishmentResolution = resolution;
        }
    }
}
