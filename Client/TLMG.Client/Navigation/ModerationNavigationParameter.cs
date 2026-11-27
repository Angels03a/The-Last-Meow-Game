using TLMG.Client.Models;

namespace TLMG.Client.Navigation
{
    public sealed class ModerationNavigationParameter
    {
        public ModerationNavigationParameter(int targetUserId, string playerName,
            int roomId, ModerationMode mode, ScreenId returnScreen)
        {
            TargetUserId = targetUserId;
            PlayerName = playerName;
            RoomId = roomId;
            Mode = mode;
            ReturnScreen = returnScreen;
        }

        public int TargetUserId { get; }
        public string PlayerName { get; }
        public int RoomId { get; }
        public ModerationMode Mode { get; }
        public ScreenId ReturnScreen { get; }
    }
}