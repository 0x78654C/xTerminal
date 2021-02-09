using SpeedTest.Models;
using Xunit;

namespace SpeedTest.Tests
{
    public class CoordinateTests
    {
        [Fact]
        public void GetDistanceTo_should_return_expected_distance()
        {
            var start = new Coordinate(1, 1);
            var end = new Coordinate(5, 5);
            var distance = start.GetDistanceTo(end);
            var expected = 629060.759879635;
            var delta = distance - expected;

            Assert.True(delta < 1e-8);
        }
    }
}