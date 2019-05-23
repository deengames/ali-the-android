using System;
using System.Collections.Generic;
using System.Linq;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Tests.Helpers;
using Ninject;
using NUnit.Framework;
using Troschuetz.Random.Generators;
using DeenGames.AliTheAndroid.Enums;

namespace DeenGames.AliTheAndroid.Tests.Model
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
        public void OnPlayerMovedAbsorbsPowerUpUnderPlayer()
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
            floor.OnPlayerMoved();

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
            var floor = new Floor(100, 100, 3, globalRandom, powerUps);
            var nextFloor = new Floor(25, 50, 4, globalRandom, powerUps);

            floor.GeneratePowerUps();

            var twins = floor.PowerUps; // TODO: later, this could include backtracking power-ups
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

        [TestCase(0, true, true)]
        [TestCase(5, true, true)]
        [TestCase(Dungeon.NumFloors, false, true)]
        public void GenerateFloorGeneratesUpAndDownStairsAppropraitely(int floorNum, bool expectStairsDown, bool expectStairsUp)
        {
            var globalRandom = new StandardGenerator(10201);
            var floor = new Floor(35, 25, floorNum, globalRandom, new List<PowerUp>());

            if (expectStairsDown) {
                Assert.That(floor.StairsDownLocation, Is.Not.EqualTo(GoRogue.Coord.NONE));
            } else {
                Assert.That(floor.StairsDownLocation, Is.EqualTo(GoRogue.Coord.NONE));
            }

            if (expectStairsUp) {
                Assert.That(floor.StairsUpLocation, Is.Not.EqualTo(GoRogue.Coord.NONE));
            } else {
                Assert.That(floor.StairsUpLocation, Is.EqualTo(GoRogue.Coord.NONE));
            }
        }

        [Test]
        public void GenerateMonstersGeneratesMonstersOnAppropriateFloorsOnly()
        {
            Console.WriteLine("STarting on problematic test");
            // Slinks on B2, TenLegs on B4, Zugs on B6
            var random = new StandardGenerator(1021);
            var noPowerUps = new List<PowerUp>();
            var width = 50;
            var height = 40;

            var floors = new List<Floor>();
            // i => B1, B2, etc. base 1)
            for (var i = 1; i <= 8; i++)
            {
                Console.WriteLine($"Generating floor B{i} ...");
                floors.Add(new Floor(width, height, i - 1, random, noPowerUps));
            }

            // Assert, starting with B1
            Assert.That(floors[0].Monsters.All(m => m.Name == "Fuseling"));
            Assert.That(floors[1].Monsters.All(m => m.Name == "Fuseling" || m.Name == "Slink"));
            Assert.That(floors[2].Monsters.All(m => m.Name == "Fuseling" || m.Name == "Slink"));
            Assert.That(floors[3].Monsters.All(m => m.Name == "Fuseling" || m.Name == "Slink" || m.Name == "TenLegs"));
            Assert.That(floors[4].Monsters.All(m => m.Name == "Fuseling" || m.Name == "Slink" || m.Name == "TenLegs"));
            Assert.That(floors[5].Monsters.All(m => m.Name == "Fuseling" || m.Name == "Slink" || m.Name == "TenLegs" || m.Name == "Zug"));
            Assert.That(floors[6].Monsters.All(m => m.Name == "Fuseling" || m.Name == "Slink" || m.Name == "TenLegs" || m.Name == "Zug"));
        }

        [Test]
        public void GenerateMonstersGeneratesMoreMonstersAsFloorsIncrease()
        {
            // Number of monsters is quasi-random. Pick the first floor with all monsters (B6) and the last; every monster should be more in number.
            // This won't pass with all seeds; only a carefully-selected seed. You may get a low number of zugs (1-3 => 1) then a +1 on the next floor.
            var random = new StandardGenerator(9);
            var noPowerUps = new List<PowerUp>();

            var b6 = new Floor(40, 40, 5, random, noPowerUps);
            var b10 = new Floor(40, 40, 9, random, noPowerUps);

            Assert.That(b10.Monsters.Where(m => m.Name == "Fuseling").Count(), Is.GreaterThan(b6.Monsters.Where(m => m.Name == "Fuseling").Count()));
            Assert.That(b10.Monsters.Where(m => m.Name == "Slink").Count(), Is.GreaterThan(b6.Monsters.Where(m => m.Name == "Slink").Count()));
            Assert.That(b10.Monsters.Where(m => m.Name == "TenLegs").Count(), Is.GreaterThan(b6.Monsters.Where(m => m.Name == "TenLegs").Count()));
            Assert.That(b10.Monsters.Where(m => m.Name == "Zug").Count(), Is.GreaterThan(b6.Monsters.Where(m => m.Name == "Zug").Count()));
        }

        [TestCase(2, Weapon.MiniMissile)]
        [TestCase(4, Weapon.Zapper)]
        [TestCase(6, Weapon.GravityCannon)]
        [TestCase(8, Weapon.InstaTeleporter)]
        [TestCase(9, Weapon.PlasmaCannon)]
        public void GenerateWeaponPickUpGeneratesWeaponsOnAppropriateFloor(int basementNumber, Weapon expectedWeapon)
        {
            var random = new StandardGenerator(93524);
            var noPowerUps = new List<PowerUp>();
            
            var floor = new Floor(30, 30, basementNumber - 1, random, noPowerUps);
            Assert.That(floor.WeaponPickUp, Is.Not.Null);
            Assert.That(floor.WeaponPickUp.Weapon, Is.EqualTo(expectedWeapon));
        }
    }
}