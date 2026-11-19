using System;
using System.Linq;
using TLMG.Client.Communication;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.ViewModels
{
    /// <summary>Proyecta la espera visual desde la última vista, sin tomar decisiones de partida.</summary>
    internal static class MatchRecoveryText
    {
        internal static string Describe(IMatchConnection connection)
        {
            if (connection.IsMatchRecoveryExhausted)
            {
                return LanguageManager.FindText("Recovery_LocalExhausted");
            }

            if (!connection.IsMatchSubscribed)
            {
                return LanguageManager.FindText("Recovery_Connecting");
            }

            MatchView view = connection.CurrentMatchView;
            if (view?.ReconnectDeadlinesUtc?.Count > 0)
            {
                DateTime serverNow = view.ServerTimeUtc + connection.CurrentMatchViewAge;
                var players = view.ReconnectDeadlinesUtc.OrderBy(entry => entry.Value).ThenBy(entry => entry.Key)
                    .Select(entry =>
                    {
                        string name = view.Players?.FirstOrDefault(player => player.UserId == entry.Key)?.Nickname
                            ?? LanguageManager.FindText("Recovery_UnknownPlayer");
                        int seconds = (int)Math.Ceiling((entry.Value - serverNow).TotalSeconds);
                        return seconds > 0
                            ? string.Format(LanguageManager.FindText("Recovery_PlayerSeconds"), name, seconds)
                            : string.Format(LanguageManager.FindText("Recovery_PlayerAwaitingServer"), name);
                    });
                return LanguageManager.FindText("Recovery_Paused") + Environment.NewLine + string.Join(" · ", players);
            }

            return view?.IsWaitingForInitialLinks == true
                ? LanguageManager.FindText("Recovery_WaitingInitialLinks") : string.Empty;
        }
    }
}
