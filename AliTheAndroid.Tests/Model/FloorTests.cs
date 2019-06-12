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
using GoRogue.Pathing;

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
        [TestCase(Dungeon.NumFloors - 1, false, true)]
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
            var random = new StandardGenerator(999);
            var noPowerUps = new List<PowerUp>();

            var b6 = new Floor(40, 40, 5, random, noPowerUps);
            var b10 = new Floor(40, 40, 9, random, noPowerUps);

            Assert.That(b10.Monsters.Where(m => m.Name == "Fuseling").Count(), Is.GreaterThanOrEqualTo(b6.Monsters.Where(m => m.Name == "Fuseling").Count()));
            Assert.That(b10.Monsters.Where(m => m.Name == "Slink").Count(), Is.GreaterThanOrEqualTo(b6.Monsters.Where(m => m.Name == "Slink").Count()));
            Assert.That(b10.Monsters.Where(m => m.Name == "TenLegs").Count(), Is.GreaterThanOrEqualTo(b6.Monsters.Where(m => m.Name == "TenLegs").Count()));
            Assert.That(b10.Monsters.Where(m => m.Name == "Zug").Count(), Is.GreaterThanOrEqualTo(b6.Monsters.Where(m => m.Name == "Zug").Count()));
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

        [Test]
        public void GenerateMapGeneratesFakeWallsAfterMiniMissileFloor()
        {
            const int MiniMissileFloor = 2; // B2
            var random = new StandardGenerator(354);
            var noPowerUps =  new List<PowerUp>();

            var floor = new Floor(30, 30, MiniMissileFloor - 1, random, noPowerUps);
            Assert.That(floor.FakeWalls.Any());
            Assert.That(floor.FakeWalls.All(w => !w.IsBacktrackingWall));
        }

        [Test]
        public void GenerateMapGeneratesBacktrackingFakeWallsOnFirstMap()
        {
            var random = new StandardGenerator(354);
            var noPowerUps =  new List<PowerUp>();

            var firstFloor = new Floor(30, 30, 0, random, noPowerUps);
            Assert.That(firstFloor.FakeWalls.Any());
            Assert.That(firstFloor.FakeWalls.All(w => w.IsBacktrackingWall));
        }

        [Test]
        public void GenerateMapGeneratesLockedDoorsAfterZapperFloor()
        {
            const int ZapperFloor = 4; // B4
            var random = new StandardGenerator(355);
            var noPowerUps =  new List<PowerUp>();

            var floor = new Floor(30, 30, ZapperFloor - 1, random, noPowerUps);
            Assert.That(floor.Doors.Any(d => d.IsLocked && !d.IsBacktrackingDoor));
        }

        [Test]
        public void GenerateMapGeneratesBacktrackingLockedDoorsOnZapperFloorMinusOne()
        {
            const int ZapperFloor = 4; // B4
            var random = new StandardGenerator(355);
            var noPowerUps =  new List<PowerUp>();

            var previousFloor = new Floor(30, 30, ZapperFloor - 2, random, noPowerUps);
            Assert.That(previousFloor.Doors.Any(d => d.IsLocked && d.IsBacktrackingDoor));
            Assert.That(previousFloor.Doors.Count(d => d.IsLocked && !d.IsBacktrackingDoor), Is.EqualTo(0));

            // Generated in a new room, make sure it's accessible
            var firstLock = previousFloor.Doors.First(d => d.IsLocked && d.IsBacktrackingDoor);
            var firstLockCoordinates = new GoRogue.Coord(firstLock.X, firstLock.Y);
            var pathFinder = new AStar(previousFloor.map, GoRogue.Distance.EUCLIDEAN);
            var path = pathFinder.ShortestPath(previousFloor.StairsUpLocation, firstLockCoordinates, true);
        }

        [Test]
        public void GenerateMapGeneratesGravityWavesAfterGravityCannonFloor()
        {
            const int GravityCannonFloor = 6; // B6
            var random = new StandardGenerator(356);
            var noPowerUps =  new List<PowerUp>();

            var floor = new Floor(30, 30, GravityCannonFloor - 1, random, noPowerUps);
            Assert.That(floor.GravityWaves.Any());
            Assert.That(floor.GravityWaves.All(g => !g.IsBacktrackingWave));            
        }

        [Test]
        public void GenerateMapGeneratesBacktrackingGravityWavesOneFloorAboveGravityCannon()
        {
            const int GravityCannonFloor = 6; // B6
            var random = new StandardGenerator(356);
            var noPowerUps =  new List<PowerUp>();
            
            var previousFloor = new Floor(30, 30, GravityCannonFloor - 2, random, noPowerUps);
            Assert.That(previousFloor.GravityWaves.Any());
            Assert.That(previousFloor.GravityWaves.All(g => g.IsBacktrackingWave));

            // Generated in a new room, make sure it's accessible
            var firstGravity = previousFloor.GravityWaves.First();
            var firstGravityCoordinates = new GoRogue.Coord(firstGravity.X, firstGravity.Y);
            var pathFinder = new AStar(previousFloor.map, GoRogue.Distance.EUCLIDEAN);
            var path = pathFinder.ShortestPath(previousFloor.StairsUpLocation, firstGravityCoordinates, true);
        }

        [Test]
        public void GenerateMapGeneratesChasmsAfterTeleporterFloor()
        {
            const int InstaTeleporterFloor = 8; // B8
            var random = new StandardGenerator(357);
            var noPowerUps =  new List<PowerUp>();

            var floor = new Floor(30, 30, InstaTeleporterFloor - 1, random, noPowerUps);
            Assert.That(floor.Chasms.Any());
        }

        [Test]
        public void GenreateMapGeneratesBacktrackingChasmsOneFloorBeforeTeleporter()
        {
            const int InstaTeleporterFloor = 8; // B8
            var random = new StandardGenerator(357);
            var noPowerUps =  new List<PowerUp>();

            var previousFloor = new Floor(30, 30, InstaTeleporterFloor - 2, random, noPowerUps);
            Assert.That(previousFloor.Chasms.Any());

            // Chasms don't have a "backtracking" property. Instead, Check that all the chasms are in one room.
            // Well, we can't check that either; so check that they're all adjacent to two other chasms. /shrug
            foreach (var chasm in previousFloor.Chasms)
            {
                var adjacencies = previousFloor.Chasms.Where(c => c != chasm && Math.Sqrt(Math.Pow(c.X - chasm.X, 2) + Math.Pow(c.Y - chasm.Y, 2)) == 1);
                Assert.That(adjacencies.Count(), Is.EqualTo(2));
            }

            // Generated in a new room, make sure it's accessible
            var firstChasm = previousFloor.Chasms.First();
            var firstChasmCoordinates = new GoRogue.Coord(firstChasm.X, firstChasm.Y);
            var pathFinder = new AStar(previousFloor.map, GoRogue.Distance.EUCLIDEAN);
            var path = pathFinder.ShortestPath(previousFloor.StairsUpLocation, firstChasmCoordinates, true);
            
        }

        [Test]
        // Fix for: https://trello.com/c/7vkfyZEY/34-were-not-generating-enough-chasms
        public void NumberOfChasmsGeneratedIsAlwaysMax()
        {
            const int ExpectedChasmCount = 5;
            var seed = 358;
            var noPowerUps =  new List<PowerUp>();
            for (var i = 0; i < 100; i++) {
                var random = new StandardGenerator(seed + i);
                var floor = new Floor(30, 30, 10, random, noPowerUps);
                Assert.That(floor.Chasms.Count, Is.GreaterThanOrEqualTo(ExpectedChasmCount));
            }
        }

        [Test]
        public void GenerateGeneratesFakeWallBetweenStairs()
        {
            var floor = new Floor(90, 45, 7, new StandardGenerator(777), new List<PowerUp>());

            var pathFinder = new AStar(floor.map, GoRogue.Distance.EUCLIDEAN);
            var path = pathFinder.ShortestPath(floor.StairsUpLocation, floor.StairsDownLocation, true);
            
            // Any steps have any fake walls on them. Alternatively, this can be negated as:
            // For all steps, there are no fake walls.
            Assert.That(path.Steps.Any(p => floor.FakeWalls.Any(f => f.X == p.X && f.Y == p.Y)));
        }
    }
}