using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Domain.Constants;

namespace TLMG.Domain.Engine
{
    /// <summary>
    /// Tabla final de una partida terminada (Reglas 5.0.1 a 5.0.4): posiciones sin empates y puntos de cada jugador.
    /// Solo lee la API pública de MatchState y no la modifica. El marcador acumulado no es de aquí.
    /// </summary>
    public sealed class FinalStandings
    {
        private const int FirstPosition = 1;
        private const int BasePointsOffset = 1;
        private const int MinimumPoints = 0;
        private const int GuestPoints = 0;

        private FinalStandings(FinalStandingsStatus status, IReadOnlyList<PlayerStanding> standings)
        {
            Status = status;
            Standings = standings;
        }

        /// <summary>Obtiene si la tabla se calculó o el motivo del rechazo.</summary>
        public FinalStandingsStatus Status { get; }

        /// <summary>Obtiene las filas ordenadas de la 1.ª posición a la última; vacía si se rechazó.</summary>
        public IReadOnlyList<PlayerStanding> Standings { get; }

        /// <summary>
        /// Calcula la tabla final de una partida terminada. Cuentan para la base todos los que no salieron de la
        /// partida, invitados incluidos; quienes salieron van al final con 0 puntos. Los invitados conservan su
        /// posición y cobran 0. Si la partida no ha terminado, se rechaza sin cambiar nada. Si un identificador de
        /// invitado se repite, se toma una sola vez.
        /// </summary>
        /// <param name="match">Partida de la que se calcula la tabla.</param>
        /// <param name="guestPlayerIds">
        /// Jugadores invitados de la partida; los entrega Services a partir de datos autoritativos, nunca del cliente.
        /// </param>
        /// <returns>La tabla calculada o el rechazo con la tabla vacía.</returns>
        /// <exception cref="ArgumentNullException">match o guestPlayerIds es null.</exception>
        /// <exception cref="ArgumentException">Un invitado no es jugador de la partida.</exception>
        public static FinalStandings CalculateFromMatch(MatchState match, IEnumerable<int> guestPlayerIds)
        {
            HashSet<int> guestIds = ValidateGuests(match, guestPlayerIds);
            FinalStandings finalStandings;

            if (match.Phase != RoundPhase.MatchFinished)
            {
                finalStandings = new FinalStandings(FinalStandingsStatus.MatchNotFinished, new List<PlayerStanding>());
            }
            else
            {
                finalStandings = new FinalStandings(FinalStandingsStatus.Calculated, CreateStandings(match, guestIds));
            }

            return finalStandings;
        }

        /// <summary>
        /// Calcula los puntos de una posición (Reglas 5.0.1): base = max(2, contados − 1); la posición k recibe
        /// base − (k − 1) más el bono de la modalidad, de ganador en la 1.ª posición y de no ganador en las demás.
        /// Nunca baja de 0. Contados son quienes no abandonaron ni fueron expulsados. No conoce a los invitados: su
        /// cero lo aplica CalculateFromMatch, después de que el invitado ya contó para la base y ocupó su posición.
        /// </summary>
        /// <param name="position">Posición final entre los contados, empezando en 1.</param>
        /// <param name="countedPlayerCount">Jugadores contados para la base, de 1 a 6.</param>
        /// <param name="modeRules">Reglas de la modalidad, que fijan los bonos.</param>
        /// <returns>Los puntos de la posición.</returns>
        /// <exception cref="ArgumentNullException">modeRules es null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">La posición o los contados están fuera de rango.</exception>
        public static int CalculatePoints(int position, int countedPlayerCount, MatchModeRules modeRules)
        {
            return CalculatePoints(position, countedPlayerCount, modeRules, position == FirstPosition);
        }

        private static int CalculatePoints(int position, int countedPlayerCount, MatchModeRules modeRules,
            bool isWinner)
        {
            ValidatePointsArguments(position, countedPlayerCount, modeRules);
            int basePoints = Math.Max(ValidationRules.MinimumBasePoints, countedPlayerCount - BasePointsOffset);
            int bonusPoints = modeRules.NonWinnerBonusPoints;

            if (isWinner)
            {
                bonusPoints = modeRules.WinnerBonusPoints;
            }

            int points = Math.Max(MinimumPoints, basePoints - (position - FirstPosition) + bonusPoints);

            return points;
        }

        /// <summary>
        /// Ordena a los jugadores por posición final (Reglas 5.0.4): el ganador; después quienes siguen en juego, por
        /// más retos, más vidas y menor secuencia del último éxito; luego los eliminados, del más tardío al primero; al
        /// final quienes salieron, por salida efectiva más tardía y luego por inicio más tardío (H-11). El
        /// ordenamiento es estable, así que el último desempate es el orden de turnos.
        /// </summary>
        private static List<MatchPlayer> OrderPlayersByPosition(MatchState match)
        {
            List<MatchPlayer> playersByPosition = new List<MatchPlayer>();
            if (match.Winner != null)
            {
                playersByPosition.Add(match.Winner);
            }

            playersByPosition.AddRange(match.Players
                .Where(player => player.IsInPlay && !ReferenceEquals(player, match.Winner))
                .OrderByDescending(player => player.SuccessfulChallengeCount)
                .ThenByDescending(player => player.Lives)
                .ThenBy(player => player.LastSuccessfulChallengeSequence));
            playersByPosition.AddRange(match.EliminatedPlayers.Reverse());
            playersByPosition.AddRange(match.Players
                .Where(player => player.HasLeftMatch)
                .OrderByDescending(player => player.Exit.EffectiveAtUtc)
                .ThenByDescending(player => player.Exit.StartedAtUtc));

            return playersByPosition;
        }

        /// <summary>
        /// Arma una fila por jugador en orden de posición. Cuentan para la base quienes no salieron; el invitado
        /// conserva su posición y cobra 0, y quien salió cobra 0 sin pasar por CalculatePoints (Reglas 5.0.2).
        /// </summary>
        private static IReadOnlyList<PlayerStanding> CreateStandings(MatchState match, HashSet<int> guestIds)
        {
            List<MatchPlayer> playersByPosition = OrderPlayersByPosition(match);
            List<PlayerStanding> standings = new List<PlayerStanding>();
            int countedPlayerCount = match.Players.Count(player => !player.HasLeftMatch);

            foreach (MatchPlayer player in playersByPosition)
            {
                int position = standings.Count + FirstPosition;
                int points = GuestPoints;

                if (!player.HasLeftMatch && !guestIds.Contains(player.PlayerId))
                {
                    points = CalculatePoints(position, countedPlayerCount, match.ModeRules,
                        ReferenceEquals(player, match.Winner));
                }

                standings.Add(new PlayerStanding(player.PlayerId, position, points));
            }

            return standings.AsReadOnly();
        }

        /// <summary>Valida los argumentos y devuelve los invitados sin repetir.</summary>
        private static HashSet<int> ValidateGuests(MatchState match, IEnumerable<int> guestPlayerIds)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            if (guestPlayerIds == null)
            {
                throw new ArgumentNullException(nameof(guestPlayerIds));
            }

            HashSet<int> guestIds = new HashSet<int>(guestPlayerIds);

            if (!guestIds.IsSubsetOf(match.Players.Select(player => player.PlayerId)))
            {
                throw new ArgumentException("Un invitado no es jugador de la partida.", nameof(guestPlayerIds));
            }

            return guestIds;
        }

        private static void ValidatePointsArguments(int position, int countedPlayerCount, MatchModeRules modeRules)
        {
            if (modeRules == null)
            {
                throw new ArgumentNullException(nameof(modeRules));
            }

            bool hasValidCount = countedPlayerCount >= FirstPosition
                && countedPlayerCount <= ValidationRules.MaximumPlayersPerMatchCount;

            if (!hasValidCount)
            {
                throw new ArgumentOutOfRangeException(nameof(countedPlayerCount));
            }

            if (position < FirstPosition || position > countedPlayerCount)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }
        }
    }
}
