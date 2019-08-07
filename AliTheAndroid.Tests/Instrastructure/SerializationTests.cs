using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Tests.Helpers;
using GoRogue.MapViews;
using Newtonsoft.Json;
using NUnit.Framework;
using Troschuetz.Random.Generators;

namespace DeenGames.AliTheAndroid.Tests.Infrastructure
{
    // A bunch of separate tests that test things we serialize independently.
    // Eg. player separate from dungeon, and don't check the dungeon's player
    [TestFixture]
    public class SerializationTests
    {
        [Test]
        public void SerializeAndDeserializeDungeon()
        {
            var expected = new Dungeon(80, 30, 2340234);
            expected.GoToNextFloor();

            var serialized = Serializer.Serialize(expected);
            var actual = Serializer.Deserialize<Dungeon>(serialized);
            
            // If we reached this far, serialization succeeded
            Assert.That(actual, Is.Not.Null);

            Assert.That(actual.Width, Is.EqualTo(expected.Width));
            Assert.That(actual.Height, Is.EqualTo(expected.Height));
            Assert.That(actual.CurrentFloorNum, Is.EqualTo(expected.CurrentFloorNum));
            Assert.That(actual.CurrentFloor, Is.Not.Null);
            Assert.That(actual.GameSeed, Is.Null); // Readonly, only used in generation
            
            Assert.That(actual.Player, Is.Not.Null);
        }

        [Test]
        public void SerializeAndDeserializePlayer()
        {
            var expected = new Player();

            expected.Acquire(Weapon.GravityCannon);
            expected.Acquire(Weapon.InstaTeleporter);
            expected.CurrentWeapon = Weapon.GravityCannon;
            expected.GotDataCube(DataCube.GetCube(3, new GoRogue.Coord(0, 0)));

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
            
            Assert.That(actual.Weapons.Count, Is.EqualTo(expected.Weapons.Count));
            foreach (var weapon in expected.Weapons)
            {
                Assert.That(actual.Weapons.Contains(weapon));
            }
        }
        
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
            var random = new StandardGenerator(1144804299);
            var expected = new Floor(80, 30, floorNum, random);
            var serialized = Serializer.Serialize(expected);
            var actual = Serializer.Deserialize<Floor>(serialized);

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

            // Other stuff we need for functionality to work
            Assert.That(actual.width, Is.EqualTo(expected.width));
            Assert.That(actual.height, Is.EqualTo(expected.height));
            
            Assert.That(actual.map, Is.Not.Null);
            Assert.That(actual.map[5, 2], Is.True);
            // for (var y = 0 ; y < expected.width; y++)
            // {
            //     for (var x = 0; x < expected.height; x++)
            //     {
            //         Assert.That(actual.map[x, y] == expected.map[x, y], $"Map at {x}, {y} should be {expected.map[x, y]} but it's {actual.map[x, y]}");
            //     }
            // }
        }

        private void AssertBasicPropertiesEqual(AbstractEntity e1, AbstractEntity e2)
        {
            Assert.That(e1.X, Is.EqualTo(e2.X));
            Assert.That(e1.Y, Is.EqualTo(e2.Y));
            Assert.That(e1.Character, Is.EqualTo(e2.Character));
            Assert.That(e1.Color, Is.EqualTo(e2.Color));
        }

        private void AssertMapsAreEqual(ArrayMap<bool> expectedMap, ArrayMap<bool> actualMap)
        {
            Assert.That(actualMap.Width, Is.EqualTo(expectedMap.Width));
            Assert.That(actualMap.Height, Is.EqualTo(expectedMap.Height));
            for (var y = 0; y < expectedMap.Height; y++)
            {
                for (var x = 0; x < expectedMap.Width; x++)
                {
                    Assert.That(actualMap[x, y], Is.EqualTo(expectedMap[x, y]));
                }
            }
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