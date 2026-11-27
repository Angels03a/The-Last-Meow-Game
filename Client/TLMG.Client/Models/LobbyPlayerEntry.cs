namespace TLMG.Client.Models
{
    public class LobbyPlayerEntry
    {
        public int RoomId { get; set; }
        public int UserId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public bool IsLocalPlayer { get; set; }
        public bool IsReady { get; set; }
        public bool CanReportOrBlock { get; set; }
    }
}