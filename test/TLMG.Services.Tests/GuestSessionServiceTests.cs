using TLMG.Contracts.DataContracts;
using TLMG.Domain.Authorization;
using TLMG.Domain.Constants;
using TLMG.Services.Accounts;
using TLMG.Services.Sessions;
using Xunit;

namespace TLMG.Services.Tests
{
    public sealed class GuestSessionServiceTests
    {
        [Fact]
        public void StartGuestSession_WithAvailableNumber_ReturnsSuccessWithEmptyErrorKey()
        {
            GuestSessionService service = new GuestSessionService(new SessionRegistry());

            OperationLogInResult result = service.StartGuestSession();

            Assert.True(result.IsSuccess);
            Assert.Equal(string.Empty, result.ErrorKey);
            Assert.True(result.IsGuest);
            Assert.False(string.IsNullOrWhiteSpace(result.SessionToken));
            Assert.True(GuestIdentity.TryParseGuestNumber(result.Nickname, out int number));
            Assert.Equal(GuestIdentity.CreateUserId(number), result.UserId);
        }

        [Fact]
        public void StartGuestSession_WhenRegistryRejects_ReturnsGuestUnavailable()
        {
            GuestSessionService service = new GuestSessionService(new RejectingRegistry());

            OperationLogInResult result = service.StartGuestSession();

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Login.GuestUnavailable, result.ErrorKey);
        }

        private sealed class RejectingRegistry : IGuestSessionRegistry
        {
            public GuestSessionStart StartGuestSession()
            {
                return GuestSessionStart.CreateRejected();
            }
        }
    }
}
