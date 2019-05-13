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
            var dungeon = new Dungeon(24, 10);
            Assert.That(dungeon.Player, Is.Not.Null);
            Assert.That(dungeon.Width, Is.EqualTo(24));
            Assert.That(dungeon.Height, Is.EqualTo(10));
        }

        [Test]
        public void GoToNextFloorChangesFloorAndIncrementsFloorNumber()
        {
            var dungeon = new Dungeon(50, 40, gameSeed: 999);

            dungeon.GoToNextFloor();
            Assert.That(dungeon.CurrentFloorNum, Is.EqualTo(1));
            var firstFloor = dungeon.CurrentFloor;

            dungeon.GoToNextFloor();
            Assert.That(dungeon.CurrentFloorNum, Is.EqualTo(2));
            var secondFloor = dungeon.CurrentFloor;

            Assert.That(secondFloor, Is.Not.EqualTo(firstFloor));
        }
    }
}