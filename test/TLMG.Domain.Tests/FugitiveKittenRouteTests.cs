using System;
using TLMG.Domain.Engine;
using Xunit;

namespace TLMG.Domain.Tests
{
    public class FugitiveKittenRouteTests
    {
        [Fact]
        public void Route_HasApprovedContinuousSegments()
        {
            FugitiveKittenRoute route = new FugitiveKittenRoute();
            int[] ends = { 900, 1800, 2800, 3700, 4700, 6500, 7500, 8400, 9400, 10300, 11300,
                12200, 13200, 14100, 15000 };
            Assert.Equal(ends.Length, route.Segments.Count);
            int start = 0;
            for (int index = 0; index < ends.Length; index++)
            {
                Assert.Equal(start, route.Segments[index].StartMilliseconds);
                Assert.Equal(ends[index], route.Segments[index].EndMilliseconds);
                start = ends[index];
            }
            Assert.Equal(900, route.Segments[14].EndMilliseconds - route.Segments[14].StartMilliseconds);
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(899, false)]
        [InlineData(900, true)]
        [InlineData(1800, true)]
        [InlineData(1950, true)]
        [InlineData(1951, false)]
        [InlineData(2799, false)]
        [InlineData(2800, true)]
        [InlineData(14250, true)]
        [InlineData(14251, false)]
        [InlineData(15000, false)]
        public void Catch_UsesInclusiveServerTolerance(int milliseconds, bool expected)
        {
            FugitiveKittenRoute route = new FugitiveKittenRoute();
            Assert.Equal(expected, route.CanCatchAt(TimeSpan.FromMilliseconds(milliseconds)));
        }
    }
}
