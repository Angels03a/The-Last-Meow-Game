using System;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>Tests UTC exit times, start at or before effective exit and immediate exits.</summary>
    public class PlayerExitTests
    {
        private const int GraceSeconds = 15;

        private static readonly DateTime _startedAtUtc = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime _effectiveAtUtc = _startedAtUtc.AddSeconds(GraceSeconds);

        /// <summary>Checks PlayerExit_WithStartBeforeEffective_KeepsBothTimes.</summary>
        [Fact]
        public void PlayerExit_WithStartBeforeEffective_KeepsBothTimes()
        {
            // Arrange
            DateTime startedAtUtc = _startedAtUtc;

            // Act
            PlayerExit exit = new PlayerExit(startedAtUtc, _effectiveAtUtc);

            // Assert
            Assert.Equal(_startedAtUtc, exit.StartedAtUtc);
            Assert.Equal(_effectiveAtUtc, exit.EffectiveAtUtc);
        }

        /// <summary>Checks Immediate_WithUtcTime_UsesSameStartAndEffectiveTime.</summary>
        [Fact]
        public void Immediate_WithUtcTime_UsesSameStartAndEffectiveTime()
        {
            // Arrange
            DateTime atUtc = _effectiveAtUtc;

            // Act
            PlayerExit exit = PlayerExit.Immediate(atUtc);

            // Assert
            Assert.Equal(atUtc, exit.StartedAtUtc);
            Assert.Equal(atUtc, exit.EffectiveAtUtc);
        }

        /// <summary>Checks PlayerExit_WithStartAfterEffective_ThrowsArgumentException.</summary>
        [Fact]
        public void PlayerExit_WithStartAfterEffective_ThrowsArgumentException()
        {
            // Arrange
            DateTime startedAtUtc = _effectiveAtUtc;

            // Act
            Action createExit = () => new PlayerExit(startedAtUtc, _startedAtUtc);

            // Assert
            ArgumentException exception = Assert.Throws<ArgumentException>(createExit);
            Assert.Equal("startedAtUtc", exception.ParamName);
        }

        /// <summary>Checks PlayerExit_WithStartThatIsNotUtc_ThrowsArgumentException.</summary>
        /// <param name="timeKind">Time kind other than UTC.</param>
        [Theory]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        public void PlayerExit_WithStartThatIsNotUtc_ThrowsArgumentException(DateTimeKind timeKind)
        {
            // Arrange
            DateTime startedAt = DateTime.SpecifyKind(_startedAtUtc, timeKind);

            // Act
            Action createExit = () => new PlayerExit(startedAt, _effectiveAtUtc);

            // Assert
            ArgumentException exception = Assert.Throws<ArgumentException>(createExit);
            Assert.Equal("startedAtUtc", exception.ParamName);
        }

        /// <summary>Checks PlayerExit_WithEffectiveTimeThatIsNotUtc_ThrowsArgumentException.</summary>
        /// <param name="timeKind">Time kind other than UTC.</param>
        [Theory]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        public void PlayerExit_WithEffectiveTimeThatIsNotUtc_ThrowsArgumentException(DateTimeKind timeKind)
        {
            // Arrange
            DateTime effectiveAt = DateTime.SpecifyKind(_effectiveAtUtc, timeKind);

            // Act
            Action createExit = () => new PlayerExit(_startedAtUtc, effectiveAt);

            // Assert
            ArgumentException exception = Assert.Throws<ArgumentException>(createExit);
            Assert.Equal("effectiveAtUtc", exception.ParamName);
        }

        /// <summary>Checks Immediate_WithTimeThatIsNotUtc_ThrowsArgumentException.</summary>
        /// <param name="timeKind">Time kind other than UTC.</param>
        [Theory]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        public void Immediate_WithTimeThatIsNotUtc_ThrowsArgumentException(DateTimeKind timeKind)
        {
            // Arrange
            DateTime at = DateTime.SpecifyKind(_startedAtUtc, timeKind);

            // Act
            Action createExit = () => PlayerExit.Immediate(at);

            // Assert
            Assert.Throws<ArgumentException>(createExit);
        }
    }
}
