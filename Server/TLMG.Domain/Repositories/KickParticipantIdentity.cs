using System;

using TLMG.Domain.Constants;

namespace TLMG.Domain.Repositories
{
    public sealed class KickParticipantIdentity
    {
        public KickParticipantIdentity(int? userId, Guid? guestSessionId)
        {
            UserId = userId;
            GuestSessionId = guestSessionId;
        }

        public int? UserId { get; }

        public Guid? GuestSessionId { get; }

        public bool IsValid
        {
            get
            {
                bool isRegistered = UserId.HasValue && UserId.Value > ValidationRules.NoUserId
                    && !GuestSessionId.HasValue;
                bool isGuest = !UserId.HasValue && GuestSessionId.HasValue
                    && GuestSessionId.Value != Guid.Empty;
                bool isValid = isRegistered || isGuest;

                return isValid;
            }
        }
    }
}