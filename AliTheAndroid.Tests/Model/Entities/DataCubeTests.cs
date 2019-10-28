using System;
using DeenGames.AliTheAndroid.Model.Entities;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.Model.Entities
{
    [TestFixture]
    public class DataCubeTests
    {
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(10)]
        [TestCase(17)]
        public void GetCubeThrowsIfFloorIsOutOfRange(int floorNum)
        {
            Assert.Throws<InvalidOperationException>(() => DataCube.GetCube(floorNum, GoRogue.Coord.NONE));
        }

        [Test]
        public void GetCubeReturnsSpecifiedCube()
        {
            // Cube for B5 is the virus one
            var cube = DataCube.GetCube(5, new GoRogue.Coord(20, 13));
            Assert.That(cube.Title, Is.EqualTo("Virus"));
            Assert.That(cube.Text, Does.Contain("virus"));

            // Test boundary cases
            var firstCube = DataCube.GetCube(2, new GoRogue.Coord(1, 13));
            Assert.That(firstCube.Title, Is.EqualTo("First Day"));

            var lastCube = DataCube.GetCube(9, new GoRogue.Coord(58, 23));
            Assert.That(lastCube.Title, Is.EqualTo("Too Late"));
        }
    }
}