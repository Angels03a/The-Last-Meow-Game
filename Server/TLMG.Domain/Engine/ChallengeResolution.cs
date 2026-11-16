using System.Collections.Generic;

namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Resolución de un reto: el apostador voltea una carta por llamada, primero su propia pila y luego la pila que
    /// elija, hasta cumplir el número prometido o revelar un Gato con Maullido Fatal que la vida extra no absorba.
    /// MatchState la crea cuando el reto queda listo y la coordina; no guarda reloj ni usa azar.
    /// </summary>
    public sealed class ChallengeResolution
    {
        private readonly MatchPlayer _bettor;
        private readonly int _promisedCardCount;
        private readonly IReadOnlyList<MatchPlayer> _players;

        /// <summary>Crea la resolución del reto de un apostador.</summary>
        /// <param name="bettor">Apostador con la apuesta más alta.</param>
        /// <param name="promisedCardCount">Cartas que prometió voltear.</param>
        /// <param name="players">Jugadores de la partida, para devolverles sus cartas al terminar.</param>
        internal ChallengeResolution(MatchPlayer bettor, int promisedCardCount, IReadOnlyList<MatchPlayer> players)
        {
            _bettor = bettor;
            _promisedCardCount = promisedCardCount;
            _players = players;
        }

        /// <summary>Obtiene las cartas volteadas que cuentan para lo prometido; el Gato absorbido cuenta.</summary>
        public int CountedCardCount { get; private set; }

        /// <summary>Obtiene la última carta volteada, o null si todavía no se volteó ninguna.</summary>
        public CardKind? LastRevealedCard { get; private set; }

        /// <summary>Obtiene el dueño de la pila de la última carta volteada, o null si no se volteó ninguna.</summary>
        public int? LastRevealedPlayerId { get; private set; }

        /// <summary>Obtiene el dueño de la pila del Gato que hizo fallar el reto, o null si no falló.</summary>
        internal MatchPlayer FatalCatOwner { get; private set; }

        /// <summary>Voltea la carta de arriba sin voltear de la pila elegida, si el orden lo permite.</summary>
        /// <param name="pileOwner">Dueño de la pila elegida; no es null.</param>
        /// <returns>Revealed, ChallengeSucceeded o ChallengeFailed; en otro caso, el motivo del rechazo.</returns>
        internal ChallengeRevealStatus RevealCard(MatchPlayer pileOwner)
        {
            ChallengeRevealStatus revealStatus;

            if (pileOwner.UnrevealedCardCount == 0)
            {
                revealStatus = ChallengeRevealStatus.PileFullyRevealed;
            }
            else if (_bettor.UnrevealedCardCount > 0 && !ReferenceEquals(pileOwner, _bettor))
            {
                revealStatus = ChallengeRevealStatus.OwnPileFirst;
            }
            else
            {
                revealStatus = ApplyReveal(pileOwner);
            }

            return revealStatus;
        }

        /// <summary>Devuelve a la mano de cada jugador todas las cartas que había colocado.</summary>
        internal void ReturnPilesToHands()
        {
            foreach (MatchPlayer player in _players)
            {
                player.ReturnPileToHand();
            }
        }

        /// <summary>Voltea la carta y evalúa primero el Gato: sin vida extra el reto falla antes de contar.</summary>
        private ChallengeRevealStatus ApplyReveal(MatchPlayer pileOwner)
        {
            CardKind revealedCard = pileOwner.RevealTopCard();
            ChallengeRevealStatus revealStatus;
            LastRevealedCard = revealedCard;
            LastRevealedPlayerId = pileOwner.PlayerId;

            if (revealedCard == CardKind.FatalCat && !_bettor.HasExtraLife)
            {
                FatalCatOwner = pileOwner;
                revealStatus = ChallengeRevealStatus.ChallengeFailed;
            }
            else
            {
                revealStatus = CountRevealedCard(revealedCard);
            }

            return revealStatus;
        }

        /// <summary>Cuenta la carta volteada; un Gato que llega aquí consume la vida extra y cuenta.</summary>
        private ChallengeRevealStatus CountRevealedCard(CardKind revealedCard)
        {
            ChallengeRevealStatus revealStatus = ChallengeRevealStatus.Revealed;

            if (revealedCard == CardKind.FatalCat)
            {
                _bettor.HasExtraLife = false;
            }

            CountedCardCount++;

            if (CountedCardCount >= _promisedCardCount)
            {
                revealStatus = ChallengeRevealStatus.ChallengeSucceeded;
            }

            return revealStatus;
        }
    }
}
