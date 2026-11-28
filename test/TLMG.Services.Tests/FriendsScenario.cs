using System;
using System.Collections.Generic;

using TLMG.Services.Friends;
using TLMG.Services.Sessions;

using TLMG.Services.Moderation;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Escenario de amigos: tres cuentas (Alpha, Bravo y Charlie) con un registro real de sesiones y un repositorio
    /// en memoria. Alpha siempre está en línea; Bravo lo está por defecto y Charlie nunca tiene canal de amigos.
    /// </summary>
    internal sealed class FriendsScenario
    {
        internal const int AlphaUserId = 1;

        internal const int BravoUserId = 2;

        internal const int CharlieUserId = 3;

        internal const string AlphaNickname = "Alpha";

        internal const string BravoNickname = "Bravo";

        internal const string CharlieNickname = "Charlie";

        internal FriendsScenario(bool isBravoOnline = true)
        {
            Repository.AddUser(AlphaUserId, AlphaNickname);
            Repository.AddUser(BravoUserId, BravoNickname);
            Repository.AddUser(CharlieUserId, CharlieNickname);
            BlockPolicy blockPolicy = new BlockPolicy(Blocks);
            Registry.ConfigureBlockPolicy(blockPolicy);
            Service = new FriendsService(Repository, Registry, Registry, blockPolicy);
            AlphaToken = StartRegisteredSession(AlphaUserId, AlphaNickname, AlphaCallback);

            if (isBravoOnline)
            {
                BravoToken = StartRegisteredSession(BravoUserId, BravoNickname, BravoCallback);
            }
            else
            {
                BravoToken = Registry.StartRegisteredSession(BravoUserId, BravoNickname, out SessionEnd replacedBravo);
            }

            CharlieToken = Registry.StartRegisteredSession(CharlieUserId, CharlieNickname, out SessionEnd replacedCharlie);
        }

        internal NoBlocksRepository Blocks { get; } = new NoBlocksRepository();

        internal SessionRegistry Registry { get; } = new SessionRegistry();

        internal FriendshipRepositoryStub Repository { get; } = new FriendshipRepositoryStub();

        internal FriendsService Service { get; }

        internal FriendsCallbackStub AlphaCallback { get; } = new FriendsCallbackStub();

        internal FriendsCallbackStub BravoCallback { get; } = new FriendsCallbackStub();

        internal string AlphaToken { get; }

        internal string BravoToken { get; }

        internal string CharlieToken { get; }

        /// <summary>Envía los avisos como lo hace ServiceImplementation, fuera de cualquier candado.</summary>
        internal static void Deliver(IReadOnlyList<Action> notifications)
        {
            foreach (Action notification in notifications)
            {
                notification();
            }
        }

        private string StartRegisteredSession(int userId, string nickname, FriendsCallbackStub callback)
        {
            string token = Registry.StartRegisteredSession(userId, nickname, out SessionEnd replacedSession);
            Registry.SubscribeFriendsChannel(token, "friends" + userId, callback, Array.Empty<int>());

            return token;
        }
    }
}
