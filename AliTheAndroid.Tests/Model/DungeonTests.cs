using System;
using System.Linq;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Tests.Helpers;
using Ninject;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.Model
{
    [TestFixture]
    public class DungeonTests : AbstractTest
    {
        [SetUp]
        public void SetupDungeonDependencies()
        {
            DependencyInjection.kernel = new StandardKernel();
            DependencyInjection.kernel.Bind<IKeyboard>().To<DeadKeyboard>();
        }

        [TestCase(-10)]
        [TestCase(-1)]
        [TestCase(0)]
        public void ConstructorThrowsIfWidthIsNotPositive(int width)
        {
            RestrictRuntime(() => {
                Assert.Throws<ArgumentException>(() => new Dungeon(width, 99));
            });
        }

        [TestCase(-10)]
        [TestCase(-1)]
        [TestCase(0)]
        public void ConstructorThrowsIfHeightIsNotPositive(int height)
        {
            RestrictRuntime(() => {
                Assert.Throws<ArgumentException>(() => new Dungeon(99, height));
            });
        }

        [Test]
        public void ConstructorSetsPlayerAndWidthAndHeight()
        {
            RestrictRuntime(() => {
                var dungeon = new Dungeon(80, 28, gameSeed: 1234);
                Assert.That(dungeon.Player, Is.Not.Null);
                Assert.That(dungeon.Width, Is.EqualTo(80));
                Assert.That(dungeon.Height, Is.EqualTo(28));
            });
        }

        [Test]
        public void GoToNextFloorChangesFloorAndIncrementsFloorNumberAndGeneratesPowerUps()
        {
            RestrictRuntime(() => {
                var dungeon = new Dungeon(50, 40, gameSeed: 999);
                var currentFloor = dungeon.CurrentFloorNum;

                dungeon.GoToNextFloor();
                Assert.That(dungeon.CurrentFloorNum, Is.EqualTo(currentFloor + 1));
                Assert.That(dungeon.CurrentFloor.PowerUps.Count, Is.GreaterThan(0));

                var firstFloor = dungeon.CurrentFloor;

                dungeon.GoToNextFloor();
                Assert.That(dungeon.CurrentFloorNum, Is.EqualTo(currentFloor + 2));
                var secondFloor = dungeon.CurrentFloor;

                Assert.That(secondFloor, Is.Not.EqualTo(firstFloor));
            });
        }

        [Test]
        public void GoToPreviousFloorDecrementsFloorNumberAndPositionsPlayerAtStairsDown()
        {
            RestrictRuntime(() => {
                var dungeon = new Dungeon(35, 25, gameSeed: 12323);
                // Start at B1 / floor 0
                dungeon.GoToNextFloor();

                dungeon.GoToNextFloor();
                Assert.That(dungeon.CurrentFloorNum, Is.EqualTo(1));

                dungeon.GoToPreviousFloor();
                Assert.That(dungeon.CurrentFloorNum, Is.EqualTo(0));
                Assert.That(dungeon.Player.X, Is.EqualTo(dungeon.CurrentFloor.StairsDownLocation.X));
                Assert.That(dungeon.Player.Y, Is.EqualTo(dungeon.CurrentFloor.StairsDownLocation.Y));
            });
        }

        [Test]
        public void GoToNextFloorDoesntRegeneratePowerUps()
        {
            RestrictRuntime(() => {
                var dungeon = new Dungeon(35, 25, gameSeed: 12323);

                // Start at B2 / floor 1
                dungeon.GoToNextFloor();
                dungeon.GoToNextFloor();
                Assert.That(dungeon.CurrentFloorNum, Is.EqualTo(1));

                var currentFloor = dungeon.CurrentFloor;
                // 3 power-ups: two paired and a secret-room one. The latter has no
                // onPickUp event or anything to clear it out on pickup; that logic
                // lives in Floor.cs itself (process player input).
                Assert.That(currentFloor.PowerUps.Any());

                // Pick up all power-ups. Clone to avoid concurrent-modification exception
                foreach (var powerup in currentFloor.PowerUps.ToArray())
                {
                    powerup.PickUp();
                }

                Assert.That(currentFloor.PowerUps.Count, Is.EqualTo(1));

                dungeon.GoToPreviousFloor();
                dungeon.GoToNextFloor();
                Assert.That(dungeon.CurrentFloorNum, Is.EqualTo(1));
                Assert.That(dungeon.CurrentFloor.PowerUps.Count, Is.EqualTo(1));
            });
        }

        [Test]
        public void DataCubeDoesntGenerateOnPowerUp()
        {
            // https://trello.com/c/TDqordPQ/146-data-cube-generates-on-power-up
            // 1559243382 / 2B
            var dungeon = new Dungeon(80, 28, 1559243382);
            var b2 = dungeon.Floors[1];
            Assert.That(!b2.PowerUps.Any(p => p.X == b2.DataCube.X && p.Y == b2.DataCube.Y));
        }

        [Test]
        public void ConstructorCreatesTutorialMessage()
        {
            var dungeon = new Dungeon(80, 28, 1209231093);
            var b1 = dungeon.Floors[0];
            var tutorialMessage = b1.LatestMessage;

            // Some sensible checks
            Assert.That(tutorialMessage.Contains("You beam to"));
            Assert.That(tutorialMessage.Contains("Use WASD to move"));
            Assert.That(tutorialMessage.Contains("F to fire"));
            Assert.That(tutorialMessage.Contains("Q and E to turn"));
        }
    }
}