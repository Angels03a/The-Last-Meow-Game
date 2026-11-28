using TLMG.Contracts.DataContracts;
using TLMG.Domain.Constants;
using TLMG.Services.Moderation;
using TLMG.Services.Sessions;
using Xunit;

namespace TLMG.Services.Tests
{
    public sealed class ReportServiceTests
    {
        private readonly SessionRegistry _registry = new SessionRegistry();

        [Fact]
        public void ReportPlayer_WithUnknownSession_ReturnsInvalidSession()
        {
            ReportService service = new ReportService(new LoginReportRepository(new PlayerRepositoryStub()),
                _registry, MatchHistoryStub.CreateCoordinator(_registry));
            string token = "unknown";

            OperationResult result = service.ReportPlayer(token, new ReportSubmission()).OperationResult;

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Common.InvalidSession, result.ErrorKey);
        }

        [Fact]
        public void ReportPlayer_WithGuestSession_ReturnsPermissionDenied()
        {
            ReportService service = new ReportService(new LoginReportRepository(new PlayerRepositoryStub()),
                _registry, MatchHistoryStub.CreateCoordinator(_registry));
            string token = _registry.StartGuestSession().SessionToken;

            OperationResult result = service.ReportPlayer(token, new ReportSubmission()).OperationResult;

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorKeys.Common.PermissionDenied, result.ErrorKey);
        }
    }
}
