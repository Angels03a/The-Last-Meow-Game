using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Xunit;

using TLMG.Domain.Engine;

namespace TLMG.Domain.Tests
{
    /// <summary>Tests for MatchModeRules.</summary>
    public class MatchModeRulesTests
    {
        private const BindingFlags PublicInstanceMembers = BindingFlags.Instance | BindingFlags.Public;

        /// <summary>Checks CreateFromCardsPerPlayerCount_WithSupportedCount_ReturnsModeData.</summary>
        [Theory]
        [InlineData(4, 1, 3, 2)]
        [InlineData(6, 1, 5, 2)]
        [InlineData(8, 2, 6, 4)]
        public void CreateFromCardsPerPlayerCount_WithSupportedCount_ReturnsModeData(
            int cardsPerPlayerCount,
            int expectedFatalCatCount,
            int expectedSafeCardCount,
            int expectedSuccessfulChallengesToWin)
        {
            // Act
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount);

            // Assert
            Assert.Equal(cardsPerPlayerCount, modeRules.CardsPerPlayerCount);
            Assert.Equal(expectedFatalCatCount, modeRules.FatalCatCount);
            Assert.Equal(expectedSafeCardCount, modeRules.SafeCardCount);
            Assert.Equal(expectedSuccessfulChallengesToWin, modeRules.SuccessfulChallengesToWin);
        }

        /// <summary>Checks CreateFromCardsPerPlayerCount_WithSupportedCount_AllowsExtraLifeOnlyInEightMode.</summary>
        [Theory]
        [InlineData(4, false)]
        [InlineData(6, false)]
        [InlineData(8, true)]
        public void CreateFromCardsPerPlayerCount_WithSupportedCount_AllowsExtraLifeOnlyInEightMode(
            int cardsPerPlayerCount,
            bool expectedAllowsExtraLife)
        {
            // Act
            MatchModeRules modeRules = MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount);

            // Assert
            Assert.Equal(expectedAllowsExtraLife, modeRules.AllowsExtraLife);
        }

        /// <summary>Checks CreateFromCardsPerPlayerCount_WithUnsupportedCount_ThrowsArgumentOutOfRange.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(9)]
        [InlineData(-4)]
        public void CreateFromCardsPerPlayerCount_WithUnsupportedCount_ThrowsArgumentOutOfRange(int cardsPerPlayerCount)
        {
            // Act
            Exception thrown = Record.Exception(
                () => MatchModeRules.CreateFromCardsPerPlayerCount(cardsPerPlayerCount));

            // Assert
            Assert.IsType<ArgumentOutOfRangeException>(thrown);
        }

        /// <summary>Checks Properties_Always_HaveNoPublicSetter.</summary>
        [Fact]
        public void Properties_Always_HaveNoPublicSetter()
        {
            // Arrange
            PropertyInfo[] publicProperties = typeof(MatchModeRules).GetProperties(PublicInstanceMembers);

            // Act
            IEnumerable<MethodInfo> publicSetters = publicProperties
                .Select(property => property.GetSetMethod(false))
                .Where(setter => setter != null);

            // Assert
            Assert.Empty(publicSetters);
        }

        /// <summary>Checks Methods_Always_ContainNoPublicInstanceMethodBesidesAccessors.</summary>
        [Fact]
        public void Methods_Always_ContainNoPublicInstanceMethodBesidesAccessors()
        {
            // Arrange
            MethodInfo[] declaredMethods = typeof(MatchModeRules).GetMethods(
                PublicInstanceMembers | BindingFlags.DeclaredOnly);

            // Act
            IEnumerable<MethodInfo> modeChangingMethods = declaredMethods.Where(method => !method.IsSpecialName);

            // Assert
            Assert.Empty(modeChangingMethods);
        }

        /// <summary>Checks Fields_Always_ContainNoPublicInstanceField.</summary>
        [Fact]
        public void Fields_Always_ContainNoPublicInstanceField()
        {
            // Act
            FieldInfo[] publicFields = typeof(MatchModeRules).GetFields(PublicInstanceMembers);

            // Assert
            Assert.Empty(publicFields);
        }
    }
}
