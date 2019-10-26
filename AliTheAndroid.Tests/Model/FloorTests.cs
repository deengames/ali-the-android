using System;
using System.Collections.Generic;
using System.Linq;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Tests.Helpers;
using DeenGames.AliTheAndroid.Tests.LongRunning;
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

        // Crash bug exposed by using 2D FOV map instead of our homebrew distance formula
        [TestCase(-10, -29)] 
        [TestCase(-7, 2)]
        [TestCase(10, -29)]
        [TestCase(220, 290)]
        [TestCase(110, 9)]
        [TestCase(3, 87)]
        public void IsInPlayerFovReturnsFalseWhenOutOfBounds(int x, int y)
        {
            var floor = new Floor(30, 20, 1, new StandardGenerator(879465));
            Assert.That(floor.IsInPlayerFov(x, y), Is.False);
        }

        [Test]
        public void MonstersDontGenerateNextToStairs()
        {
            RestrictRuntime(() => {
                var prng = new StandardGenerator(1325677035);
                for (var i = 0; i < 10; i++) {
                    // Real seed from a real bug; this failed on B6 (going down) then B3 (going up)
                    // Also, this finds a slink group on B6 that generates one guy too close to stairs up
                    var floor = new Floor(80, 32, i, prng);
                    foreach (var monster in floor.Monsters)
                    {
                        var distanceToStairsUp = Math.Sqrt(Math.Pow(monster.X - floor.StairsUpLocation.X, 2) + Math.Pow(monster.Y - floor.StairsUpLocation.Y, 2));
                        var distanceToStairsDown = Math.Sqrt(Math.Pow(monster.X - floor.StairsDownLocation.X, 2) + Math.Pow(monster.Y - floor.StairsDownLocation.Y, 2));
                        Assert.That(distanceToStairsUp >= Floor.MinimumDistanceFromMonsterToStairs, $"Stairs UP for {monster.Character} at {monster.X}, {monster.Y} on floor B{i + 1} are at d={distanceToStairsUp} (min is {Floor.MinimumDistanceFromMonsterToStairs})");
                        Assert.That(distanceToStairsDown >= Floor.MinimumDistanceFromMonsterToStairs, $"Stairs DOWN for {monster.Character} at {monster.X}, {monster.Y} on floor B{i + 1} are at d={distanceToStairsDown} (min is {Floor.MinimumDistanceFromMonsterToStairs})");
                    }
                }
            });
        }

        [Test]
        public void OnPlayerMovedAbsorbsPowerUpUnderPlayer()
        {
            RestrictRuntime(() => {
                // Arrange
                var player = new Player();
                var floor = new Floor(30, 30, 0, new StandardGenerator(1111));
                floor.Player = player;
                var powerUp = new PowerUp(0, 0, healthBoost:30);
                floor.PowerUps.Add(powerUp);
                var oldHealth = player.TotalHealth;
                player.X = powerUp.X;
                player.Y = powerUp.Y;

                floor.RecalculatePlayerFov();

                // Act
                floor.OnPlayerMoved();

                // Assert
                Assert.That(player.TotalHealth, Is.EqualTo(oldHealth + powerUp.HealthBoost));
                Assert.That(floor.LatestMessage, Does.Contain("You activate the power-up"));
                Assert.That(floor.PowerUps, Does.Not.Contain(powerUp));
            });
        }

        [Test]
        public void GeneratePicksTwoRandomChoicesForPairedPowerUps()
        {
            RestrictRuntime(() => {
                // Check two floors have different power-ups
                var floor1 = new Floor(40, 30, 7, new StandardGenerator(1234));
                var floor2 = new Floor(40, 30, 7, new StandardGenerator(4321));

                Assert.That(floor1.PowerUps.Any());
                Assert.That(floor2.PowerUps.Any());
                foreach (var p1 in floor1.PowerUps)
                {
                    Assert.That(!floor2.PowerUps.Contains(p1));
                }

                // Check that each floor has distinct power-ups (different messages = different types)
                Assert.That(floor1.PairedPowerUps.GroupBy(p => p.Message).Count(),
                    Is.EqualTo(floor1.PairedPowerUps.Length));
            });
        }

        [Test]
        public void PickingUpPowerUpsRemovesThemFromTheCollectionAndRemovesTheOtherOption()
        {
            RestrictRuntime(() => {
                var globalRandom = new StandardGenerator(0);
                var floor = new Floor(100, 100, 3, globalRandom);
                var nextFloor = new Floor(25, 50, 4, globalRandom);

                // We actually get three power-ups: two (paired/twins) and one in a secret room.
                // Filter out the secret room easily: it's covered with fake walls.
                var twins = floor.PowerUps.Where(p => !p.IsBacktrackingPowerUp && !floor.FakeWalls.Any(f => f.X == p.X && f.Y == p.Y));
                Assert.That(twins.Count, Is.EqualTo(2));
                var twin = twins.First();
                twin.PickUp();

                Assert.That(!twins.Any());
            });
        }

        [Test]
        public void PickingUpBackTrackingPowerUpRemovesPairedPowerUp()
        {
            RestrictRuntime(() => {
                // 0 = B1 = has two power-ups behind a fake wall
                var floor = new Floor(80, 28, 0, new StandardGenerator(23985));
                var backTrackingPowerUps = floor.PowerUps.Where(p => p.IsBacktrackingPowerUp);
                // Probably two
                backTrackingPowerUps.First().PickUp();
                Assert.That(!floor.PowerUps.Where(p => p.IsBacktrackingPowerUp).Any());
            });
        }

        [TestCase(0, true, true)]
        [TestCase(5, true, true)]
        [TestCase(Dungeon.NumFloors - 1, false, true)]
        public void GenerateFloorGeneratesUpAndDownStairsAppropraitely(int floorNum, bool expectStairsDown, bool expectStairsUp)
        {
            RestrictRuntime(() => {
                var globalRandom = new StandardGenerator(10201);
                var floor = new Floor(35, 25, floorNum, globalRandom);

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
            });
        }

        [Test]
        public void GenerateMonstersGeneratesMonstersOnAppropriateFloorsOnly()
        {
            RestrictRuntime(() => {
                // Slinks on B2, TenLegs on B4, Zugs on B6
                var random = new StandardGenerator(1021);
                var width = 50;
                var height = 40;

                var floors = new List<Floor>();
                // i => B1, B2, etc. base 1)
                for (var i = 1; i <= 8; i++)
                {
                    floors.Add(new Floor(width, height, i - 1, random));
                }

                // Assert, starting with B1
                Assert.That(floors[0].Monsters.All(m => m.Name == "Fuseling"));
                Assert.That(floors[1].Monsters.All(m => m.Name == "Fuseling" || m.Name == "Slink"));
                Assert.That(floors[2].Monsters.All(m => m.Name == "Fuseling" || m.Name == "Slink"));
                Assert.That(floors[3].Monsters.All(m => m.Name == "Fuseling" || m.Name == "Slink" || m.Name == "TenLegs"));
                Assert.That(floors[4].Monsters.All(m => m.Name == "Fuseling" || m.Name == "Slink" || m.Name == "TenLegs"));
                Assert.That(floors[5].Monsters.All(m => m.Name == "Fuseling" || m.Name == "Slink" || m.Name == "TenLegs" || m.Name == "Zug"));
                Assert.That(floors[6].Monsters.All(m => m.Name == "Fuseling" || m.Name == "Slink" || m.Name == "TenLegs" || m.Name == "Zug"));
            });
        }

        [Test]
        public void GenerateMonstersGeneratesMoreMonstersAsFloorsIncrease()
        {
            RestrictRuntime(() => {
                // Number of monsters is quasi-random. Pick the first floor with all monsters (B6) and the last; every monster should be more in number.
                // This won't pass with all seeds; only a carefully-selected seed. You may get a low number of zugs (1-3 => 1) then a +1 on the next floor.
                // Ditto for slinks, which spawn in random-sized groups.
                var random = new StandardGenerator(9999);

                var b6 = new Floor(40, 40, 5, random);
                var b10 = new Floor(40, 40, 9, random);

                Assert.That(b10.Monsters.Where(m => m.Name == "Fuseling").Count(), Is.GreaterThanOrEqualTo(b6.Monsters.Where(m => m.Name == "Fuseling").Count()));
                Assert.That(b10.Monsters.Where(m => m.Name == "Slink").Count(), Is.GreaterThanOrEqualTo(b6.Monsters.Where(m => m.Name == "Slink").Count()));
                Assert.That(b10.Monsters.Where(m => m.Name == "TenLegs").Count(), Is.GreaterThanOrEqualTo(b6.Monsters.Where(m => m.Name == "TenLegs").Count()));
                Assert.That(b10.Monsters.Where(m => m.Name == "Zug").Count(), Is.GreaterThanOrEqualTo(b6.Monsters.Where(m => m.Name == "Zug").Count()));
            });
        }

        [TestCase(2, Weapon.MiniMissile)]
        [TestCase(4, Weapon.Zapper)]
        [TestCase(6, Weapon.GravityCannon)]
        [TestCase(8, Weapon.InstaTeleporter)]
        [TestCase(9, Weapon.PlasmaCannon)]
        public void GenerateWeaponPickUpGeneratesWeaponsOnAppropriateFloor(int basementNumber, Weapon expectedWeapon)
        {
            RestrictRuntime(() => {
                var random = new StandardGenerator(93524);
                
                
                var floor = new Floor(30, 30, basementNumber - 1, random);
                Assert.That(floor.WeaponPickUp, Is.Not.Null);
                Assert.That(floor.WeaponPickUp.Weapon, Is.EqualTo(expectedWeapon));
            });
        }

        [Test]
        public void GenerateMapGeneratesFakeWallsAfterMiniMissileFloor()
        {
            RestrictRuntime(() => {
                const int MiniMissileFloor = 2; // B2
                var random = new StandardGenerator(354);
                var noPowerUps =  new List<PowerUp>();

                var floor = new Floor(30, 30, MiniMissileFloor - 1, random);
                Assert.That(floor.FakeWalls.Any());
                Assert.That(floor.FakeWalls.All(w => !w.IsBacktrackingWall));
            });
        }

        [Test]
        public void GenerateMapGeneratesBacktrackingFakeWallsOnFirstMapWithPairedPowerUps()
        {
            RestrictRuntime(() => {
                var random = new StandardGenerator(354);
                var noPowerUps =  new List<PowerUp>();

                var firstFloor = new Floor(30, 30, 0, random);
                Assert.That(firstFloor.FakeWalls.Any());
                Assert.That(firstFloor.FakeWalls.All(w => w.IsBacktrackingWall));

                // Make sure the power-ups are backtracking and paired
                var powerUps = firstFloor.PowerUps.Where(p => p.IsBacktrackingPowerUp).ToArray();
                Assert.That(powerUps.Count(), Is.EqualTo(2));

                var p1 = powerUps[0];
                var p2 = powerUps[1];

                Assert.That(p1.PairedTo, Is.EqualTo(p2), $"{p1.Message} should be paired to {p2.Message} but instead is paired to {p1.PairedTo.Message}!");
                Assert.That(p2.PairedTo, Is.EqualTo(p1), $"{p2.Message} should be paired to {p1.Message} but instead is paired to {p2.PairedTo.Message}!");
            });
        }

        [Test]
        public void GenerateMapGeneratesLockedDoorsAfterZapperFloor()
        {
            RestrictRuntime(() => {
                const int ZapperFloor = 4; // B4
                var random = new StandardGenerator(355);
                var noPowerUps =  new List<PowerUp>();

                var floor = new Floor(30, 30, ZapperFloor - 1, random);
                Assert.That(floor.Doors.Any(d => d.IsLocked && !d.IsBacktrackingDoor));
            });
        }

        [Test]
        public void GenerateMapGeneratesBacktrackingLockedDoorsOnZapperFloorMinusOneWithPairedPowerUps()
        {
            RestrictRuntime(() => {
                const int ZapperFloor = 4; // B4
                var random = new StandardGenerator(355);
                var noPowerUps =  new List<PowerUp>();

                var floor = new Floor(30, 30, ZapperFloor - 2, random);
                Assert.That(floor.Doors.Any(d => d.IsLocked && d.IsBacktrackingDoor));
                Assert.That(floor.Doors.Count(d => d.IsLocked && !d.IsBacktrackingDoor), Is.EqualTo(0));
                
                // Make sure the power-ups are backtracking and paired
                var powerUps = floor.PowerUps.Where(p => p.IsBacktrackingPowerUp).ToArray();
                Assert.That(powerUps.Count(), Is.EqualTo(2));

                var p1 = powerUps[0];
                var p2 = powerUps[1];

                Assert.That(p1.PairedTo, Is.EqualTo(p2));
                Assert.That(p2.PairedTo, Is.EqualTo(p1));

                // Generated in a new room, make sure it's accessible
                var firstLock = floor.Doors.First(d => d.IsLocked && d.IsBacktrackingDoor);
                var firstLockCoordinates = new GoRogue.Coord(firstLock.X, firstLock.Y);
                var pathFinder = new AStar(floor.Map, GoRogue.Distance.EUCLIDEAN);
                var path = pathFinder.ShortestPath(floor.StairsUpLocation, firstLockCoordinates, true);
            });
        }

        [Test]
        public void GenerateMapGeneratesGravityWavesAfterGravityCannonFloor()
        {
            RestrictRuntime(() => {
                const int GravityCannonFloor = 6; // B6
                var random = new StandardGenerator(356);
                var noPowerUps =  new List<PowerUp>();

                var floor = new Floor(80, 28, GravityCannonFloor - 1, random);
                Assert.That(floor.GravityWaves.Any());
                Assert.That(floor.GravityWaves.All(g => !g.IsBacktrackingWave)); 
            });
        }

        [Test]
        public void GenerateMapGeneratesBacktrackingGravityWavesOneFloorAboveGravityCannonWithPairedPowerUps()
        {
            RestrictRuntime(() => {
                const int GravityCannonFloor = 6; // B6
                var random = new StandardGenerator(356);
                var noPowerUps =  new List<PowerUp>();
                
                var floor = new Floor(30, 30, GravityCannonFloor - 2, random);
                Assert.That(floor.GravityWaves.Any());
                Assert.That(floor.GravityWaves.All(g => g.IsBacktrackingWave));

                // Make sure the power-ups are backtracking and paired
                var powerUps = floor.PowerUps.Where(p => p.IsBacktrackingPowerUp).ToArray();
                Assert.That(powerUps.Count(), Is.EqualTo(2));

                var p1 = powerUps[0];
                var p2 = powerUps[1];

                Assert.That(p1.PairedTo, Is.EqualTo(p2));
                Assert.That(p2.PairedTo, Is.EqualTo(p1));

                // Generated in a new room, make sure it's accessible
                var firstGravity = floor.GravityWaves.First();
                var firstGravityCoordinates = new GoRogue.Coord(firstGravity.X, firstGravity.Y);
                var pathFinder = new AStar(floor.Map, GoRogue.Distance.EUCLIDEAN);
                var path = pathFinder.ShortestPath(floor.StairsUpLocation, firstGravityCoordinates, true);
            });
        }

        [Test]
        public void GenerateMapGeneratesChasmsAfterTeleporterFloor()
        {
            RestrictRuntime(() => {
                const int InstaTeleporterFloor = 7; // B8
                var random = new StandardGenerator(357);
                var noPowerUps =  new List<PowerUp>();

                // Teleporter floor itself should have chasms surrounding the door
                var floor = new Floor(30, 30, InstaTeleporterFloor, random);
                Assert.That(floor.Chasms.All(c => GoRogue.Distance.EUCLIDEAN.Calculate(c.X, c.Y, floor.StairsDownLocation.X, floor.StairsDownLocation.Y) <= 1.5));

                floor = new Floor(30, 30, InstaTeleporterFloor + 1, random);
                Assert.That(floor.Chasms.Any());
            });
        }

        [Test]
        public void GenreateMapGeneratesBacktrackingChasmsOneFloorBeforeTeleporterWithPairedPowerUps()
        {
            RestrictRuntime(() => {
                const int InstaTeleporterFloor = 8; // B8
                var random = new StandardGenerator(357);
                var noPowerUps =  new List<PowerUp>();

                var floor = new Floor(30, 30, InstaTeleporterFloor - 2, random);
                Assert.That(floor.Chasms.Any());

                // Chasms don't have a "backtracking" property. Instead, Check that all the chasms are in one room.
                // Well, we can't check that either; so check that they're all adjacent to two other chasms. /shrug
                foreach (var chasm in floor.Chasms)
                {
                    var adjacencies = floor.Chasms.Where(c => c != chasm && Math.Sqrt(Math.Pow(c.X - chasm.X, 2) + Math.Pow(c.Y - chasm.Y, 2)) == 1);
                    Assert.That(adjacencies.Count(), Is.EqualTo(2));
                }

                // Make sure the power-ups are backtracking and paired
                var powerUps = floor.PowerUps.Where(p => p.IsBacktrackingPowerUp).ToArray();
                Assert.That(powerUps.Count(), Is.EqualTo(2));

                var p1 = powerUps[0];
                var p2 = powerUps[1];

                Assert.That(p1.PairedTo, Is.EqualTo(p2));
                Assert.That(p2.PairedTo, Is.EqualTo(p1));

                // Generated in a new room, make sure it's accessible
                var firstChasm = floor.Chasms.First();
                var firstChasmCoordinates = new GoRogue.Coord(firstChasm.X, firstChasm.Y);
                var pathFinder = new AStar(floor.Map, GoRogue.Distance.EUCLIDEAN);
                var path = pathFinder.ShortestPath(floor.StairsUpLocation, firstChasmCoordinates, true);
            });
            
        }

        [Test]
        // Fix for: https://trello.com/c/7vkfyZEY/34-were-not-generating-enough-chasms
        public void NumberOfChasmsGeneratedIsAlwaysMax()
        {
            RestrictRuntime(() => {
                const int ExpectedChasmCount = 5;
                var seed = 359;
                var noPowerUps =  new List<PowerUp>();
                for (var i = 0; i < 100; i++) {
                    var random = new StandardGenerator(seed + i);
                    var floor = new Floor(80, 28, 10, random);
                    Assert.That(floor.Chasms.Count, Is.GreaterThanOrEqualTo(ExpectedChasmCount));
                }
            });
        }

        [Test]
        public void GenerateGeneratesFakeWallBetweenStairs()
        {
            RestrictRuntime(() => {

                var floor = new Floor(90, 45, 7, new StandardGenerator(777));
                
                // We need to use a modified map that doesn't treat fake walls as unwalkable, so that A* can find the right route.
                // This was broken to fix pocket dimensions: https://trello.com/c/ZcMhxYPo/119-secret-rooms-arent-flooded-with-fake-walls
                var map = floor.Map;
                floor.FakeWalls.ForEach(f => map[f.X, f.Y] = true);

                var pathFinder = new AStar(map, GoRogue.Distance.EUCLIDEAN);
                var path = pathFinder.ShortestPath(floor.StairsUpLocation, floor.StairsDownLocation, true);
                
                // Any steps have any fake walls on them. Alternatively, this can be negated as:
                // For all steps, there are no fake walls.
                Assert.That(path.Steps.Any(p => floor.FakeWalls.Any(f => f.X == p.X && f.Y == p.Y)));
            });
        }

        // https://trello.com/c/fmynV9Qa/41-test-fails-because-chasm-generates-on-stairs-up
        [Test]
        public void StairsUpAndDownAreAlwaysWalkable()
        {
            RestrictRuntime(() => {
                var dungeon = new Dungeon(80, 28, gameSeed: 1036496413);
                while (dungeon.CurrentFloorNum < 9)
                {
                    dungeon.GoToNextFloor();
                    var floor = dungeon.CurrentFloor;
                    
                    // keep him from being on the stairs, thus causing our asserts to fail
                    floor.Player.X = 999;
                    floor.Player.Y = 999;

                    if (dungeon.CurrentFloorNum < 9)
                    {
                        if (dungeon.CurrentFloorNum == 5)
                        {
                            // B6 is not walkable because we flood the stairs room with (now non-walkable) plasma.
                            Assert.That(!floor.IsWalkable(floor.StairsDownLocation.X, floor.StairsDownLocation.Y));
                            Assert.That(floor.GravityWaves.Any(g => g.X == floor.StairsDownLocation.X && g.Y == floor.StairsDownLocation.Y));
                        }
                        else
                        {
                            // It should be walkable. If it's not, it should be just covered with a gravity wave. (They fill random rooms.)
                            // Ergo, removing the gravity wave should make it walkable.
                            var gravity = floor.GravityWaves.SingleOrDefault(g => g.X == floor.StairsDownLocation.X && g.Y == floor.StairsDownLocation.Y);
                            if (gravity != null)
                            {
                                floor.GravityWaves.Remove(gravity);
                            }

                            Assert.That(floor.IsWalkable(floor.StairsDownLocation.X, floor.StairsDownLocation.Y), $"Stairs down on B{dungeon.CurrentFloorNum + 1} is not walkable!");
                        }
                    }

                    if (dungeon.CurrentFloorNum >= 1)
                    {
                        Assert.That(floor.IsWalkable(floor.StairsUpLocation.X, floor.StairsUpLocation.Y), $"Stairs up on B{dungeon.CurrentFloorNum + 1} is not walkable!");
                    }
                }
            });
        }

        // https://trello.com/c/aqjBuMJC/29-dungeon-generates-with-randomly-placed-locked-doors
        // Turns out we were counting empty floors wrong all along. :facepalm: :facepalm: :facepalm:
        [Test]
        public void CountAdjacentFloorsReturnsCorrectCountExcludingCenter()
        {
            RestrictRuntime(() => {
                var floor = new Floor(80, 28, 0, new StandardGenerator(69874632));
                floor.Walls.RemoveAll(w => true);

                Assert.That(floor.CountAdjacentFloors(new GoRogue.Coord(2, 2)), Is.EqualTo(8));
            });
        }

        [Test]
        public void DataCubesGenerateOnFloorsB2toB9Inclusive()
        {
            RestrictRuntime(() => {

                var generator = new StandardGenerator(452323);

                var firstFloor = new Floor(80, 28, 0, generator);
                Assert.That(firstFloor.DataCube, Is.Null);

                for (var floorNum = 1; floorNum < 9; floorNum++)
                {
                    var floor = new Floor(80, 28, floorNum, generator);
                    Assert.That(floor.DataCube, Is.Not.Null);
                }

                var lastFloor = new Floor(80, 28, 10, generator);
                Assert.That(lastFloor.DataCube, Is.Null);
            });
        }
        

        // https://trello.com/c/2QRUML4b/57-some-fake-walls-just-cant-be-broken
        // Shooting a missile and directly hitting a fake wall, doesn't destroy it.
        // That's because we (incorrectly) don't spawn an explosion at the epicenter,
        // in order to avoid double-damaging monsters.
        [Test]
        public void CreateExplosionCreatesTilesAroundAndIncludingMissileEpicenter()
        {
            var generator = new StandardGenerator(313821775);

            var floor = new Floor(80, 28, 0, generator);
            var epicenter = new GoRogue.Coord(5, 5);
            var radius = Floor.ExplosionRadius;

            floor.CreateExplosion(epicenter.X, epicenter.Y);

            // Just for clarity, yes it's redundant
            Assert.That(floor.EffectEntities.Any(e => e.X == epicenter.X && e.Y == epicenter.Y && e is Explosion), $"Didn't find explosion at epicenter!");

            for (var y = epicenter.Y - radius; y <= epicenter.Y + radius; y++)
            {
                for (var x = epicenter.X - radius; x <= epicenter.X + radius; x++)
                {
                    var distance = Math.Sqrt(Math.Pow(x - epicenter.X, 2) + Math.Pow(y - epicenter.Y, 2));
                    if (distance <= radius) {
                        Assert.That(floor.EffectEntities.Any(e => e.X == x && e.Y == y && e is Explosion), $"Didn't find explosion at {x}, {y}");
                    }
                }
            }
        }

        [Test]
        public void ShipCoreOnlyGeneratesOnFinalFloor()
        {
            RestrictRuntime(() => {
                var generator = new StandardGenerator(562365745);
                for (var i = 0; i < 10; i++)
                {
                    var floor = new Floor(80, 28, i, generator);
                    var expectDrive = i == 9;

                    if (expectDrive)
                    {
                        Assert.That(floor.ShipCore, Is.Not.Null);
                        for (var x = floor.ShipCore.X - 1; x <= floor.ShipCore.X + 1; x++)
                        {
                            for (var y = floor.ShipCore.Y - 1; y <= floor.ShipCore.Y + 1; y++)
                            {
                                if (x != floor.ShipCore.X && y != floor.ShipCore.Y)
                                {
                                    // There should be a fake wall there. If not, it's because there's a real wall there (stairs is next to a wall).
                                    Assert.That(floor.FakeWalls.Any(f => f.X == x && f.Y == y), $"Core is at {floor.ShipCore.X}, {floor.ShipCore.Y}. Expected fake wall at {x}, {y} but there wasn't one.");
                                }
                            }
                        }
                    }
                    else
                    {
                        Assert.That(floor.ShipCore, Is.Null);
                    }
                }
            });
        }

        [Test]
        public void BossGeneratesOnB10()
        {
            var generator = new StandardGenerator(6846452);
            var floor = new Floor(80, 28, 9, generator);
            Assert.That(floor.Monsters.Where(m => m is Ameer).Count(), Is.EqualTo(1));
        }

        [Test]
        public void BossDoesntGenerateBeforeB10()
        {
            RestrictRuntime(() => {
                var generator = new StandardGenerator(4653256);            
                for (var i = 0; i < 9; i++)
                {
                    var floor = new Floor(80, 28, i, generator);
                    Assert.That(floor.Monsters.All(m => !(m is Ameer)));
                }
            });
        }

        [Test]
        public void ShipCoreDoesntGenerateCloseToStairs()
        {
            RestrictRuntime(() => {
                var generator = new StandardGenerator(1090796822);
                Floor lastFloor = null;

                for (var i = 0; i < 10; i++)
                {
                    lastFloor = new Floor(80, 28, i, generator);
                }

                Assert.That(lastFloor.ShipCore, Is.Not.Null);
                var core = lastFloor.ShipCore;
                var stairs = lastFloor.StairsUpLocation;

                var distance = Math.Sqrt(Math.Pow(core.X - stairs.X, 2) + Math.Pow(core.Y - stairs.Y, 2));
                Assert.That(distance, Is.GreaterThanOrEqualTo(8));
            });
        }

        // Regression for https://trello.com/c/L8VK30Q4/62-player-can-generate-on-top-of-weapons
        [Test]
        public void WeaponsDontGenerateOnStairsUp()
        {
            RestrictRuntime(() => {
                var dungeon = new Dungeon(80, 28, 1714594838);
                // Advance to B4
                dungeon.GoToNextFloor();
                dungeon.GoToNextFloor();
                dungeon.GoToNextFloor();
                dungeon.GoToNextFloor();
                
                var floor = dungeon.CurrentFloor;
                Assert.That(new GoRogue.Coord(floor.WeaponPickUp.X, floor.WeaponPickUp.Y), Is.Not.EqualTo(floor.StairsUpLocation));
            });
        }

        // Known offender, see https://trello.com/c/ZLF4LCGz/103-stairs-can-generate-under-data-cube
        [Test]
        public void GenerateDoesntGenerateDataCubeOnStairs()
        {
            RestrictRuntime(() => {
                var seed = 1470491287;
                var dungeon = new Dungeon(80, 28, seed);
                var b9 = dungeon.Floors[8];
                var dataCubePosition = new GoRogue.Coord(b9.DataCube.X, b9.DataCube.Y);
                Assert.That(b9.StairsDownLocation, Is.Not.EqualTo(dataCubePosition));
                Assert.That(b9.StairsUpLocation, Is.Not.EqualTo(dataCubePosition));
            });
        }

        // https://trello.com/c/KtLsagTW/105-doors-generating-next-to-chasms-make-the-game-unbeatable
        [Test]
        public void GenerateDoesntGenerateDoorsNextToChasms()
        {
            RestrictRuntime(() => {
                var seed = 1352595784;
                var dungeon = new Dungeon(80, 28, seed);
                var b10 = dungeon.Floors.Last();
                
                var offendingDoors = b10.Doors.Where(d => b10.Chasms.Any(c => Math.Pow(c.X - d.X, 2) + Math.Pow(c.Y - d.Y, 2) <= 1));
                Assert.That(!offendingDoors.Any(), $"Expected no doors adjacent to chasms but found {offendingDoors.Count()}!");
            });
        }

        // https://trello.com/c/BYFu7sGD/131-dungeon-generation-crashes
        [Test]
        public void GenerateDoesntGenerateFakeWallsOnTopOfStairs()
        {
            RestrictRuntime(() => {
                var seed = 808458114;
                var dungeon = new Dungeon(80, 28, seed);
                var b6 = dungeon.Floors.ElementAt(5);

                Assert.That(!b6.FakeWalls.Any(f => f.X == b6.StairsUpLocation.X && f.Y == b6.StairsUpLocation.Y));
                Assert.That(!b6.FakeWalls.Any(f => f.X == b6.StairsDownLocation.X && f.Y == b6.StairsDownLocation.Y));
            });
        }

        [Test]
        public void CreateIsolatedRoomReturnsRoomSizeIncludingOneTileBorder()
        {
            // https://trello.com/c/MDHAEkAz/139-backtracking-plasma-rooms-should-be-full-to-the-edges
            // Look for a room filled with plasma (some plasma tiles touch walls)
            var floor = new Floor(80, 28, 4, new StandardGenerator(29482014));
            var backtrackPowerUps = floor.PowerUps.Where(p => p.IsBacktrackingPowerUp && floor.GravityWaves.Any(g => p.X == g.X && p.Y == g.Y));
            // Backtracking rooms are always 5x5 (interior) and power-ups are always paired
            var leftX = backtrackPowerUps.Min(p => p.X);
            var powerUp = backtrackPowerUps.Single(p => p.X == leftX);

            var roomStartX = powerUp.X - 1;
            var roomStopX = roomStartX + 5;
            var roomStartY = powerUp.Y - 2;
            var roomStopY = roomStartY + 5;

            // Make sure every interior tile is filled with gravity waves
            for (var y = roomStartY; y < roomStopY; y++)
            {
                for (var x = roomStartX; x < roomStopX; x++)
                {
                    // Throws if there's no gravity wave where there should be one.
                    floor.GravityWaves.Single(g => g.X == x && g.Y == y);
                }
            }
        }

        [Test]
        public void StairsDownAreSurroundedByRelevantObstaclesOnWeaponFloors()
        {
            var dungeon = new Dungeon(80, 28, 84684965);

            // Fake walls on B2
            var floor = dungeon.Floors[1];
            var expectedSpots = getSurroundingTiles(floor.StairsDownLocation);
            foreach (var expected in expectedSpots)
            {
                Assert.That(floor.FakeWalls.SingleOrDefault(w => w.X == expected.X && w.Y == expected.Y), Is.Not.Null);
            }

            // Locked doors on B4
            floor = dungeon.Floors[3];
            expectedSpots = getSurroundingTiles(floor.StairsDownLocation);
            foreach (var expected in expectedSpots)
            {
                Assert.That(floor.Doors.Single(w => w.X == expected.X && w.Y == expected.Y).IsLocked);
            }

            // Gravity waves on B6 fill the room.
            floor = dungeon.Floors[5];
            expectedSpots = getSurroundingTiles(floor.StairsDownLocation);
            foreach (var expected in expectedSpots)
            {
                Assert.That(floor.GravityWaves.SingleOrDefault(w => w.X == expected.X && w.Y == expected.Y), Is.Not.Null);
            }

            // Chasms on B8. They're just AbstractEntity instances.
            floor = dungeon.Floors[7];
            expectedSpots = getSurroundingTiles(floor.StairsDownLocation);
            foreach (var expected in expectedSpots)
            {
                Assert.That(floor.Chasms.SingleOrDefault(w => w.X == expected.X && w.Y == expected.Y), Is.Not.Null);
            }
        }

        [Test]
        // https://trello.com/c/TjX5mEQg/169-power-ups-generate-on-top-of-chasms-epic-sigh
        public void PowerUpsDontGenerateOnChasms()
        {
            var dungeon = new Dungeon(80, 28, 948154598);
            var b8 = dungeon.Floors[7];
            Assert.That(b8.PowerUps.All(p => b8.Chasms.All(c => p.X != c.X || p.Y != c.Y)));
        }

        private List<GoRogue.Coord> getSurroundingTiles(GoRogue.Coord location)
        {
            var x = location.X;
            var y = location.Y;
            return new List<GoRogue.Coord>() {
                new GoRogue.Coord(x - 1, y - 1),
                new GoRogue.Coord(x - 1, y),
                new GoRogue.Coord(x - 1, y + 1),
                new GoRogue.Coord(x, y - 1),
                new GoRogue.Coord(x, y + 1),
                new GoRogue.Coord(x + 1, y - 1),
                new GoRogue.Coord(x + 1, y),
                new GoRogue.Coord(x + 1, y + 1),
            };
        }
    }
}