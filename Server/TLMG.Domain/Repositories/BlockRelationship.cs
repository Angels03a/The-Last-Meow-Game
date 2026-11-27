namespace TLMG.Domain.Repositories
{
    public sealed class BlockRelationship
    {
        public int BlockerUserId { get; set; }

        public int BlockedUserId { get; set; }

        public string BlockedNickname { get; set; }
    }
}
