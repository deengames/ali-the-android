using System;
using System.Linq;
using AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Tests.Helpers;
using Ninject;
using NUnit.Framework;
using Troschuetz.Random.Generators;

namespace AliTheAndroid.Tests.Model
{
    [TestFixture]
    public class FloorTests : AbstractTest
    {
        [SetUp]
        public void SetupDungeonDependencies()
        {
            DependencyInjection.kernel = new StandardKernel();
            DependencyInjection.kernel.Bind<IKeyboard>().To<DeadKeyboard>();
        }

        [Test]
        public void GenerateGeneratesPowerUpNearStairs()
        {
            // Good choice of seed: stairs have no adjacent floors, but extended search finds one.
            var floor = new Floor(50, 40, 1, new StandardGenerator(1234), new Player());
                        
            Assert.That(floor.PowerUps.Count, Is.GreaterThan(0));
            var powerUp = floor.PowerUps.First();
            var distance = Math.Sqrt(Math.Pow(powerUp.X - floor.StairsLocation.X, 2) + Math.Pow(powerUp.Y - floor.StairsLocation.Y, 2));
            Assert.That(distance, Is.LessThanOrEqualTo(10));
        }

        [Test]
        public void UpdateAbsorbsPowerUpUnderPlayer()
        {
            // Arrange
            var player = new Player();
            var floor = new Floor(30, 30, 1, new StandardGenerator(1111), player);
            var powerUp = new PowerUp(0, 0, healthBoost:30);
            floor.PowerUps.Add(powerUp);
            var oldHealth = player.TotalHealth;
            player.X = powerUp.X;
            player.Y = powerUp.Y;

            // Act
            floor.Update(TimeSpan.MinValue);

            // Assert
            Assert.That(player.TotalHealth, Is.EqualTo(oldHealth + powerUp.HealthBoost));
            Assert.That(floor.LatestMessage, Does.Contain("You activate the power-up"));
            Assert.That(floor.PowerUps, Does.Not.Contain(powerUp));
        }
    }
}