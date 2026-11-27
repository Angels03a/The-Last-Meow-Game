using TLMG.Client.Communication;
using TLMG.Client.Models;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Navigation
{
    internal sealed class RoomModerationNavigation
    {
        private const int NoUserId = 0;
        private const int NoRoomId = 0;
        private const string InvalidTargetErrorKey = "Moderation_InvalidTargetError";

        private readonly ILobbyConnection _lobbyConnection;
        private readonly INavigationManager _navigationManager;
        private readonly IDialogManager _dialogManager;
        private readonly ScreenId _returnScreen;

        internal RoomModerationNavigation(ILobbyConnection lobbyConnection,
            INavigationManager navigationManager, IDialogManager dialogManager, ScreenId returnScreen)
        {
            _lobbyConnection = lobbyConnection;
            _navigationManager = navigationManager;
            _dialogManager = dialogManager;
            _returnScreen = returnScreen;
        }

        internal bool CanOpen(int roomId, int targetUserId, ModerationMode mode)
        {
            bool canOpen = FindTarget(roomId, targetUserId, mode) != null;

            return canOpen;
        }

        internal void Open(int roomId, int targetUserId, ModerationMode mode)
        {
            LobbyPlayer target = FindTarget(roomId, targetUserId, mode);

            if (target != null)
            {
                string playerName = DisplayNameHelper.Resolve(target.UserId, target.Nickname);
                ModerationNavigationParameter parameter = new ModerationNavigationParameter(
                    target.UserId, playerName, roomId, mode, _returnScreen);

                _navigationManager.NavigateToWithoutHistory(ScreenId.Moderation, parameter);
            }
            else
            {
                _dialogManager.ShowError(InvalidTargetErrorKey);
            }
        }

        private LobbyPlayer FindTarget(int roomId, int targetUserId, ModerationMode mode)
        {
            LobbyRoom room = _lobbyConnection.CurrentRoom;
            LobbyPlayer target = null;
            int localUserId = _lobbyConnection.PlayerUserId;

            if (IsExpectedRoom(room, roomId) && localUserId != NoUserId
                && targetUserId != NoUserId && targetUserId != localUserId
                && IsAllowedMode(targetUserId, mode) && FindPlayer(room, localUserId) != null)
            {
                target = FindPlayer(room, targetUserId);
            }

            return target;
        }

        private bool IsExpectedRoom(LobbyRoom room, int roomId)
        {
            bool isExpectedRoom = false;

            if (room != null && roomId > NoRoomId && room.RoomId == roomId)
            {
                if (_returnScreen == ScreenId.Lobby)
                {
                    isExpectedRoom = !room.InGame && !_lobbyConnection.IsCountdownVisible;
                }
                else if (_returnScreen == ScreenId.Gameplay)
                {
                    isExpectedRoom = room.InGame && room.MatchId.HasValue;
                }
            }

            return isExpectedRoom;
        }

        private bool IsAllowedMode(int targetUserId, ModerationMode mode)
        {
            bool isAllowed = mode == ModerationMode.VoteToKick;

            if (mode == ModerationMode.Report || mode == ModerationMode.Block)
            {
                isAllowed = _lobbyConnection.HasRegisteredSession && targetUserId > NoUserId;
            }

            return isAllowed;
        }

        private static LobbyPlayer FindPlayer(LobbyRoom room, int userId)
        {
            LobbyPlayer selectedPlayer = null;

            if (room.Players != null)
            {
                foreach (LobbyPlayer player in room.Players)
                {
                    if (player != null && player.UserId == userId)
                    {
                        selectedPlayer = player;
                    }
                }
            }

            return selectedPlayer;
        }
    }
}