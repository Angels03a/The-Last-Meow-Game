using System;
using System.Collections.Generic;
using System.Linq;

using TLMG.Client.Communication;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Presentation
{
    internal static class MatchRecoveryMessageFormatter
    {
        private const string RecoveryExhaustedKey = "Recovery_LocalExhausted";
        private const string ConnectingKey = "Recovery_Connecting";
        private const string PausedKey = "Recovery_Paused";
        private const string UnknownPlayerKey = "Recovery_UnknownPlayer";
        private const string PlayerSecondsKey = "Recovery_PlayerSeconds";
        private const string PlayerAwaitingServerKey = "Recovery_PlayerAwaitingServer";
        private const string WaitingInitialLinksKey = "Recovery_WaitingInitialLinks";
        private const string PlayerMessageSeparator = " · ";
        private const int NoRemainingSeconds = 0;
        private const int NoReconnectDeadlines = 0;

        internal static string Describe(IMatchConnection matchConnection)
        {
            string recoveryMessage = string.Empty;

            if (matchConnection.IsMatchRecoveryExhausted)
            {
                recoveryMessage = LanguageManager.FindText(RecoveryExhaustedKey);
            }
            else if (!matchConnection.IsMatchSubscribed)
            {
                recoveryMessage = LanguageManager.FindText(ConnectingKey);
            }
            else
            {
                MatchView matchView = matchConnection.CurrentMatchView;

                if (matchView?.ReconnectDeadlinesUtc?.Count > NoReconnectDeadlines)
                {
                    DateTime estimatedServerTimeUtc = matchView.ServerTimeUtc + matchConnection.CurrentMatchViewAge;
                    recoveryMessage = DescribePausedMatch(matchView, estimatedServerTimeUtc);
                }
                else if (matchView?.IsWaitingForInitialLinks == true)
                {
                    recoveryMessage = LanguageManager.FindText(WaitingInitialLinksKey);
                }
            }

            return recoveryMessage;
        }

        private static string DescribePausedMatch(MatchView matchView, DateTime estimatedServerTimeUtc)
        {
            IEnumerable<KeyValuePair<int, DateTime>> orderedDeadlines =
                from reconnectDeadline in matchView.ReconnectDeadlinesUtc
                orderby reconnectDeadline.Value, reconnectDeadline.Key
                select reconnectDeadline;

            List<string> playerMessages = new List<string>();

            foreach (KeyValuePair<int, DateTime> reconnectDeadline in orderedDeadlines)
            {
                string playerName = FindPlayerName(matchView, reconnectDeadline.Key);
                int remainingSeconds = (int)Math.Ceiling(
                    (reconnectDeadline.Value - estimatedServerTimeUtc).TotalSeconds);
                string playerMessage;

                if (remainingSeconds > NoRemainingSeconds)
                {
                    playerMessage = string.Format(LanguageManager.FindText(PlayerSecondsKey),
                        playerName, remainingSeconds);
                }
                else
                {
                    playerMessage = string.Format(LanguageManager.FindText(PlayerAwaitingServerKey), playerName);
                }

                playerMessages.Add(playerMessage);
            }

            string pausedMatchMessage = LanguageManager.FindText(PausedKey) + Environment.NewLine
                + string.Join(PlayerMessageSeparator, playerMessages);

            return pausedMatchMessage;
        }

        private static string FindPlayerName(MatchView matchView, int userId)
        {
            string playerName = LanguageManager.FindText(UnknownPlayerKey);

            if (matchView.Players != null)
            {
                foreach (MatchPlayerView matchPlayer in matchView.Players)
                {
                    if (matchPlayer.UserId == userId)
                    {
                        playerName = matchPlayer.Nickname ?? playerName;
                    }
                }
            }

            return playerName;
        }
    }
}
