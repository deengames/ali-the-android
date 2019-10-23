using System.Collections.Generic;
using System.Linq;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Tests.Helpers;
using NUnit.Framework;
using Troschuetz.Random.Generators;

namespace DeenGames.AliTheAndroid.Tests.Infrastructure
{
    // A bunch of separate tests that test things we serialize independently.
    // Eg. player separate from dungeon, and don't check the dungeon's player
    [TestFixture]
    public class SerializationTests
    {
        [OneTimeSetUp]
        public void BindKeyboardToDeadKeyboard()
        {
            // TODO: DELETE! Refactor into "if not bound ..."
            DependencyInjection.kernel.Bind<IKeyboard>().To<DeadKeyboard>();
        }

        // Just the core dungeon properties, not recurisvely on all sub-entities/properties
        [Test]
        public void SerializeAndDeserializeDungeon()
        {
            var expected = new Dungeon(80, 28, 2340234);
            expected.GoToNextFloor();

            var serialized = Serializer.Serialize(expected);
            var actual = Serializer.Deserialize<Dungeon>(serialized);
            
            // If we reached this far, serialization succeeded
            Assert.That(actual, Is.Not.Null);

            Assert.That(actual.Width, Is.EqualTo(expected.Width));
            Assert.That(actual.Height, Is.EqualTo(expected.Height));
            Assert.That(actual.CurrentFloorNum, Is.EqualTo(expected.CurrentFloorNum));
            Assert.That(actual.CurrentFloor, Is.Not.Null);
            Assert.That(actual.GameSeed, Is.EqualTo(expected.GameSeed));
            
            Assert.That(actual.Player, Is.Not.Null);
        }

        [Test]
        public void SerializeAndDeserializePlayer()
        {
            var expected = new Player();

            var shieldDamage = 90;
            expected.Acquire(Weapon.GravityCannon);
            expected.Acquire(Weapon.InstaTeleporter);
            expected.CurrentWeapon = Weapon.GravityCannon;
            expected.GotDataCube(DataCube.GetCube(3, new GoRogue.Coord(0, 0)));
            expected.Shield.Damage(shieldDamage);

            var serialized = Serializer.Serialize(expected);
            var actual = Serializer.Deserialize<Player>(serialized);

            Assert.That(actual, Is.Not.Null);
            
            this.AssertBasicPropertiesEqual(actual, expected);
            Assert.That(actual.CanFireGravityCannon, Is.EqualTo(expected.CanFireGravityCannon));
            Assert.That(actual.CanMove, Is.EqualTo(expected.CanMove));
            Assert.That(actual.CurrentHealth, Is.EqualTo(expected.CurrentHealth));
            AssertCollectionsEqual(actual.DataCubes, expected.DataCubes);
            Assert.That(actual.Defense, Is.EqualTo(expected.Defense));
            Assert.That(actual.DirectionFacing, Is.EqualTo(expected.DirectionFacing));
            Assert.That(actual.IsDead, Is.EqualTo(expected.IsDead));
            Assert.That(actual.Strength, Is.EqualTo(expected.Strength));
            Assert.That(actual.TotalHealth, Is.EqualTo(expected.TotalHealth));
            Assert.That(actual.VisionRange, Is.EqualTo(expected.VisionRange));
            Assert.That(actual.Shield.CurrentShield, Is.EqualTo(Shield.MaxShield - shieldDamage));

            Assert.That(actual.Weapons.Count, Is.EqualTo(expected.Weapons.Count));
            foreach (var weapon in expected.Weapons)
            {
                Assert.That(actual.Weapons.Contains(weapon));
            }
        }
        
        // Operates on collections, not object-specific properties
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        public void SerializeAndDeserializeFloor(int floorNum)
        {
            var random = new StandardGenerator(floorNum);
            var expected = new Floor(80, 28, floorNum, random);
            expected.Player = new Player();

            var serialized = Serializer.Serialize(expected);
            var actual = Serializer.Deserialize<Floor>(serialized);
            actual.InitializeMapAndFov();

            Assert.That(actual.FloorNum, Is.EqualTo(expected.FloorNum));

            // Collections of entities
            this.AssertCollectionsEqual(expected.Walls, actual.Walls);
            this.AssertCollectionsEqual(expected.FakeWalls, actual.FakeWalls);
            this.AssertCollectionsEqual(expected.Doors, actual.Doors);
            this.AssertCollectionsEqual(expected.GravityWaves, actual.GravityWaves);
            this.AssertCollectionsEqual(expected.Chasms, actual.Chasms);
            this.AssertCollectionsEqual(expected.Monsters, actual.Monsters);
            this.AssertCollectionsEqual(expected.PowerUps, actual.PowerUps);
            this.AssertCollectionsEqual(expected.QuantumPlasma, actual.QuantumPlasma);

            // stairs down/up, weapon pick-up, data cube, ship core
            Assert.That(expected.StairsDownLocation, Is.EqualTo(actual.StairsDownLocation));
            Assert.That(expected.StairsUpLocation, Is.EqualTo(actual.StairsUpLocation));

            if (expected.WeaponPickUp != null)
            {
                AssertBasicPropertiesEqual(expected.WeaponPickUp, actual.WeaponPickUp);
            }

            if (expected.DataCube != null)
            {
                AssertBasicPropertiesEqual(expected.DataCube, actual.DataCube);
                Assert.That(expected.DataCube.FloorNumber, Is.EqualTo(actual.DataCube.FloorNumber));
                Assert.That(expected.DataCube.IsRead, Is.EqualTo(actual.DataCube.IsRead));
                Assert.That(expected.DataCube.Text, Is.EqualTo(actual.DataCube.Text));
                Assert.That(expected.DataCube.Title, Is.EqualTo(actual.DataCube.Title));
            }

            if (expected.ShipCore != null)
            {
                AssertBasicPropertiesEqual(expected.ShipCore,actual.ShipCore);
            }

            Assert.That(actual.Player, Is.Not.Null);

            // Other stuff we need for functionality to work
            Assert.That(actual.Width, Is.EqualTo(expected.Width));
            Assert.That(actual.Height, Is.EqualTo(expected.Height));
            
            Assert.That(actual.Map, Is.Not.Null);

            // Doesn't always pass yet for reasons unknown, need to investigate later.
            // for (var y = 0 ; y < expected.height; y++)
            // {
            //     for (var x = 0; x < expected.width; x++)
            //     {
            //         Assert.That(actual.map[x, y] == expected.map[x, y], $"Floor #{floorNum} map at {x}, {y} should be {expected.map[x, y]} but it's {actual.map[x, y]}");
            //     }
            // }
        }

        // Bug: floor number didn't deserialize. Test each wave's properties rigorously.
        [Test]
        public void SerializeAndDeserializeGravityWave()
        {
            var expected = new Floor(80, 28, 4, new StandardGenerator(846453));
            var serialized = Serializer.Serialize(expected);
            var actual = Serializer.Deserialize<Floor>(serialized);

            // Make sure we're not just testing default/false values
            Assert.That(expected.GravityWaves.Any(g => g.IsBacktrackingWave == true));

            foreach (var expectedEntity in expected.GravityWaves)
            {
                var deserializedEntity = actual.GravityWaves.Single(e => e.X == expectedEntity.X && e.Y == expectedEntity.Y);
                Assert.That(deserializedEntity.IsBacktrackingWave, Is.EqualTo(expectedEntity.IsBacktrackingWave));
                Assert.That(deserializedEntity.FloorNum, Is.EqualTo(expectedEntity.FloorNum));
            }
        }


        [Test]
        public void SerializeAndDeserializeDoors()
        {
            // Arrange: backtracking, opened, locked are all true
            var expected = new Door(45, 13, true, true);
            expected.IsOpened = true;

            var json = Serializer.Serialize(expected);
            var actual = Serializer.Deserialize<Door>(json);

            Assert.That(actual.IsBacktrackingDoor, Is.EqualTo(expected.IsBacktrackingDoor));
            Assert.That(actual.IsLocked, Is.EqualTo(expected.isLocked));
            Assert.That(actual.isOpened, Is.EqualTo(expected.isOpened));
        }

        [Test]
        public void SerializeAndDeserializeMonster()
        {
            var expected = new Floor(80, 28, 9, new StandardGenerator(1111111211));
            var serialized = Serializer.Serialize(expected);
            var actual = Serializer.Deserialize<Floor>(serialized);

            foreach (var expectedEntity in expected.Monsters)
            {
                var monsters = actual.Monsters.Where(e => e.X == expectedEntity.X && e.Y == expectedEntity.Y);
                Assert.That(monsters.Count(), Is.EqualTo(1), $"Expected one monster at {expectedEntity.X}, {expectedEntity.Y} but got {monsters.Count()}");
                var deserializedEntity = monsters.Single();
                Assert.That(deserializedEntity.CanMove, Is.EqualTo(expectedEntity.CanMove));
                Assert.That(deserializedEntity.CurrentHealth, Is.EqualTo(expectedEntity.CurrentHealth));
                Assert.That(deserializedEntity.Defense, Is.EqualTo(expectedEntity.Defense));
                Assert.That(deserializedEntity.IsDead, Is.EqualTo(expectedEntity.IsDead));
                Assert.That(deserializedEntity.Name, Is.EqualTo(expectedEntity.Name));
                Assert.That(deserializedEntity.Strength, Is.EqualTo(expectedEntity.Strength));
                Assert.That(deserializedEntity.TotalHealth, Is.EqualTo(expectedEntity.TotalHealth));
                Assert.That(deserializedEntity.VisionRange, Is.EqualTo(expectedEntity.VisionRange));
            }
        }

        [Test]
        public void SerializeAndDeserializePowerUps()
        {
            var random = new StandardGenerator(2348321);
            var e1 = new PowerUp(31, 12, '^', true, 9, 8, 7, 6);
            
            var e2 = PowerUp.Generate(random);
            PowerUp.Pair(e1, e2);

            var expected = new PowerUp[] { e1, e2 };
            var serialized = Serializer.Serialize(expected);
            var actual = Serializer.Deserialize<PowerUp[]>(serialized);

            for (var i = 0; i < 2; i++)
            {
                var expectedEntity = expected[i];
                var deserializedEntity = actual[i];
                
                Assert.That(deserializedEntity.DefenseBoost, Is.EqualTo(expectedEntity.DefenseBoost));
                Assert.That(deserializedEntity.HealthBoost, Is.EqualTo(expectedEntity.HealthBoost));
                Assert.That(deserializedEntity.IsBacktrackingPowerUp, Is.EqualTo(expectedEntity.IsBacktrackingPowerUp));
                Assert.That(deserializedEntity.Message, Is.EqualTo(expectedEntity.Message));
                Assert.That(deserializedEntity.StrengthBoost, Is.EqualTo(expectedEntity.StrengthBoost));
                Assert.That(deserializedEntity.VisionBoost, Is.EqualTo(expectedEntity.VisionBoost));

                // JSON.NET is configured properly to handle (serialize) loops/references in objects.
                // BUT, if p1 and p2 are paired (refer to ecah other), when I deserialize, I get back
                // p1 (paired to p2) and p2 (paired to null).  Not to sweat, our code can handle this.
                // We work around this by repairing things after deserializing the entire dungeon.
                // See: https://github.com/JamesNK/Newtonsoft.Json/issues/715
                // See: https://github.com/JamesNK/Newtonsoft.Json/pull/1567
                
                // UPDATE: this work-around didn't work; see: https://trello.com/c/XU4p02Lk/135-in-some-cases-power-ups-behind-a-chasm-arent-paired-if-you-load-game
                // Instead, we now manually bi-directionally pair power-ups. So this is no longer an issue.
                // TitleConsole calls PairPowerUps() post-serialization to get around this, which calls PowerUp.Pair, which is symmetric.
                Assert.That(deserializedEntity.PairedTo, Is.Not.Null);
            }
        }

        [Test]
        public void PairPowerUpsPairsPairedPowerUps()
        {
            var random = new StandardGenerator(784653);
            var floor = new Floor(80, 31, 0, random);
            var serialized = Serializer.Serialize(floor);
            var deserialized = Serializer.Deserialize<Floor>(serialized);

            // Sanity
            Assert.That(deserialized.PowerUps[0].PairedTo, Is.EqualTo(deserialized.PowerUps[1]));
            Assert.That(deserialized.PowerUps[1].PairedTo, Is.EqualTo(deserialized.PowerUps[0]));

            // Act
            deserialized.PairPowerUps();

            // Assert. This doesn't work on deserialized.PowerUps.
            // It looks like JSON.net is not equating the references, because PowerUps[0] != PairedPowerUps[0].
            // And yet, this works in practice: new game, save, quit, load, get powerup => destroys paired one.
            // ¯\_(ツ)_/¯
            var p1 = deserialized.PairedPowerUps[0];
            var p2 = deserialized.PairedPowerUps[1];
            Assert.That(p1.PairedTo, Is.EqualTo(p2));
            Assert.That(p2.PairedTo, Is.EqualTo(p1));

            // https://trello.com/c/zT3IX8nh/147-unpaired-power-ups-again
            // Make sure paired power-ups have PickUpCallback specified
            Assert.That(deserialized.PowerUps.Where(p => p.PairedTo != null).All(p => p.PickUpCallback != null));
        }

        [Test]
        public void PowerUpsInBacktrackingRoomOnB3ArePaired()
        {
            // https://trello.com/c/Gs0o4E8y/121-power-ups-in-a-room-of-locked-doors-on-b3-are-not-paired
            // Seed 350164614, 3F: the power-ups aren't paired?! -_-
            
            // TODO: try save/load if this doesn't reproduce the issue
            var dungeon = new Dungeon(80, 30, 350164614);
            var serialized = Serializer.Serialize(dungeon);

            // Production work-flow, see TitleConsole.cs
            dungeon = Serializer.Deserialize<Dungeon>(serialized);
            // Go in and re-pair power-ups which are not paired any more
            foreach (var floor in dungeon.Floors)
            {
                floor.PairPowerUps();
                floor.InitializeMapAndFov();
                floor.RecreateSubclassedMonsters();
            }
                
            var b3 = dungeon.Floors[2];
            
            // Magic numbers deduced from testing
            var topLeftDoor = b3.Doors[21]; // 60, 24
            var bottomRightdoor = b3.Doors.Last();

            // PowerUps[1] at (61, 25) and PowerUps[2] at (63, 26)
            var shouldBePaired = b3.PowerUps.Where(p => p.X >= topLeftDoor.X && p.Y >= topLeftDoor.Y && p.X <= bottomRightdoor.X && p.Y <= bottomRightdoor.Y);
            Assert.That(shouldBePaired.All(p => shouldBePaired.Contains(p.PairedTo)));
        }

        [Test]
        public void PowerUpsInBacktrackingRoomOnB7ArePaired()
        {
            // https://trello.com/c/XU4p02Lk/135-power-ups-behind-a-chasm-arent-paired-if-you-load-game
        // New game, get to B7, save, load, and power-ups are no longer paired.
        
            var dungeon = new Dungeon(80, 30, 12345);
            var serialized = Serializer.Serialize(dungeon);

            // Production work-flow, see TitleConsole.cs
            dungeon = Serializer.Deserialize<Dungeon>(serialized);
            var b7 = dungeon.Floors[6];

            // Go in and re-pair power-ups which are not paired any more
            b7.PairPowerUps();
                
            var shouldBePaired = b7.PowerUps.Where(p => p.IsBacktrackingPowerUp && !b7.FakeWalls.Any(f => f.X == p.X && f.Y == p.Y));
            foreach (var powerup in shouldBePaired)
            {
                Assert.That(powerup.PairedTo, Is.Not.Null);
            }
        }

        private void AssertBasicPropertiesEqual(AbstractEntity e1, AbstractEntity e2)
        {
            Assert.That(e1.X, Is.EqualTo(e2.X));
            Assert.That(e1.Y, Is.EqualTo(e2.Y));
            Assert.That(e1.Character, Is.EqualTo(e2.Character));
            Assert.That(e1.Color, Is.EqualTo(e2.Color));
        }

        private void AssertCollectionsEqual(IEnumerable<AbstractEntity> actual, IEnumerable<AbstractEntity> expected)
        {
            Assert.That(expected.Count(), Is.EqualTo(actual.Count()));
            foreach (var e in expected)
            {
                Assert.That(
                    actual.Count(a => a.X == e.X && a.Y == e.Y && a.Character == e.Character && a.Color == e.Color),
                    Is.EqualTo(1));
            }
        }
    }
}