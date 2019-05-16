using System;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Tests.Helpers;
using Ninject;
using NUnit.Framework;

namespace AliTheAndroid.Tests.Model
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
            Assert.Throws<ArgumentException>(() => new Dungeon(width, 99));
        }

        [TestCase(-10)]
        [TestCase(-1)]
        [TestCase(0)]
        public void ConstructorThrowsIfHeightIsNotPositive(int height)
        {
            Assert.Throws<ArgumentException>(() => new Dungeon(99, height));
        }

        [Test]
        public void ConstructorSetsPlayerAndWidthAndHeight()
        {
            var dungeon = new Dungeon(24, 31);
            Assert.That(dungeon.Player, Is.Not.Null);
            Assert.That(dungeon.Width, Is.EqualTo(24));
            Assert.That(dungeon.Height, Is.EqualTo(31));
        }

        [Test]
        public void GoToNextFloorChangesFloorAndIncrementsFloorNumberAndGeneratesPowerUps()
        {
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
        }

        [Test]
        public void GoToPreviousFloorDecrementsFloorNumberAndPositionsPlayerAtStairsDown()
        {
            var dungeon = new Dungeon(35, 25, gameSeed: 12323);
            // Start at B1 / floor 0
            dungeon.GoToNextFloor();

            dungeon.GoToNextFloor();
            Assert.That(dungeon.CurrentFloorNum, Is.EqualTo(1));

            dungeon.GoToPreviousFloor();
            Assert.That(dungeon.CurrentFloorNum, Is.EqualTo(0));
            Assert.That(dungeon.Player.X, Is.EqualTo(dungeon.CurrentFloor.StairsDownLocation.X));
            Assert.That(dungeon.Player.Y, Is.EqualTo(dungeon.CurrentFloor.StairsDownLocation.Y));
        }

        [Test]
        public void GoToNextFloorDoesntRegeneratePowerUps()
        {
            var dungeon = new Dungeon(35, 25, gameSeed: 12323);

            // Start at B2 / floor 1
            dungeon.GoToNextFloor();
            dungeon.GoToNextFloor();
            Assert.That(dungeon.CurrentFloorNum, Is.EqualTo(1));
            Assert.That(dungeon.CurrentFloor.PowerUps.Count > 0);

            dungeon.CurrentFloor.PowerUps[0].PickUp();
            Assert.That(dungeon.CurrentFloor.PowerUps.Count == 0);

            dungeon.GoToPreviousFloor();
            dungeon.GoToNextFloor();
            Assert.That(dungeon.CurrentFloorNum, Is.EqualTo(1));
            Assert.That(dungeon.CurrentFloor.PowerUps.Count == 0);            
        }

    }
}