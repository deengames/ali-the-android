using System;
using System.Collections.Generic;
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
        public void UpdateAbsorbsPowerUpUnderPlayer()
        {
            // Arrange
            var player = new Player();
            var floor = new Floor(30, 30, 0, new StandardGenerator(1111), new List<PowerUp>());
            floor.Player = player;
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

        [Test]
        public void GeneratePowerUpsPicksTwoChoicesOutOfConstructorValue()
        {
            var powerUps = new List<PowerUp>() { 
                new PowerUp(0, 0, healthBoost: 100),
                new PowerUp(0, 0, strengthBoost: 10),
                new PowerUp(0, 0, defenseBoost: 7)
            };

            var floor = new Floor(40, 30, 7, new StandardGenerator(1234), powerUps);
            Assert.That(!floor.PowerUps.Any());
            floor.GeneratePowerUps();

            Assert.That(floor.PowerUps.Any());
            Assert.That(floor.PowerUps.All(p => powerUps.Contains(p)));
        }

        [Test]
        public void PickingUpPowerUpsRemovesThemFromTheCollectionAndRemovesTheOtherOption()
        {
            // Generate three power-ups; you pick up one. The other two show up next floor.
            var powerUps = new List<PowerUp>() { 
                new PowerUp(0, 0, healthBoost: 100),
                new PowerUp(0, 0, strengthBoost: 10),
                new PowerUp(0, 0, defenseBoost: 7)
            };

            var globalRandom = new StandardGenerator(0);
            var floor = new Floor(100, 100, 10, globalRandom, powerUps);
            var nextFloor = new Floor(25, 50, 11, globalRandom, powerUps);

            floor.GeneratePowerUps();

            var twins = floor.PowerUps; // TODO: later, this will include backtracking power-ups
            Assert.That(twins.Count, Is.EqualTo(2));
            var twin = twins.First();
            twin.PickUp();

            Assert.That(!twins.Any());
            Assert.That(powerUps, Does.Not.Contain(twin));

            // Progress to next floor, see the other two
            nextFloor.GeneratePowerUps();
            Assert.That(nextFloor.PowerUps.Count, Is.EqualTo(2));
            foreach (var p in nextFloor.PowerUps)
            {
                Assert.That(powerUps.Contains(p));
            }
        }

        [Test]
        public void GenerateFloorDoesntRegeneratePowerUps()
        {
            var powerUps = new List<PowerUp>() { 
                new PowerUp(0, 0, strengthBoost: 8),
                new PowerUp(0, 0, defenseBoost: 8)
            };

            var floor = new Floor(30, 30, 0, new StandardGenerator(1), powerUps);
            floor.GeneratePowerUps();
            Assert.That(floor.PowerUps.Count, Is.EqualTo(2));

            // Create a copy so we don't modify the collection during enumeration
            foreach (var powerUp in floor.PowerUps.ToArray())
            {
                powerUp.PickUp();
            }

            Assert.That(floor.PowerUps.Count, Is.EqualTo(0));
            // Act
            floor.GeneratePowerUps();

            // Assert
            Assert.That(floor.PowerUps.Count, Is.EqualTo(0));

        }
    }
}