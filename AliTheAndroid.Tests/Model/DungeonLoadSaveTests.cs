using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Tests.Helpers;
using GoRogue.MapViews;
using Newtonsoft.Json;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.Model
{
    [TestFixture]
    public class DungeonLoadSaveTests
    {
        [Test]
        public void DeserializeDeserializesSerializedInstance()
        {
            DependencyInjection.kernel.Bind<IKeyboard>().To<DeadKeyboard>();
            var expected = new Dungeon(80, 30, 2340234);

            var serialized = expected.Serialize();
            var actual = Dungeon.Deserialize(serialized);
            
            // If we reached this far, serialization succeeded
            Assert.That(actual, Is.Not.Null);

            // Sanity-test the dungeon. Power-ups are self-directional loops, did they deserialize OK?
            // A series of increasingly deep checks.
            Assert.That(actual.Width, Is.EqualTo(expected.Width));
            Assert.That(actual.Height, Is.EqualTo(expected.Height));
            Assert.That(actual.Floors.Count, Is.EqualTo(expected.Floors.Count));

            var expectedFloors = expected.Floors;
            var actualFloors = actual.Floors;

            for (var i = 0; i < expectedFloors.Count; i++)
            {
                var expectedFloor = expectedFloors[i];
                var actualFloor = actualFloors[i];
                this.AssertFloorsAreEqual(expectedFloor, actualFloor);
            }
        }

        private void AssertFloorsAreEqual(Floor expectedFloor, Floor actualFloor)
        {
            // Maps are only used for generation and are null when deserialized (non-public).
            //this.AssertMapsAreEqual(expectedFloor.map, actualFloor.map);

            // Collections of entities
            this.AssertCollectionsEqual(expectedFloor.Walls, actualFloor.Walls);
            this.AssertCollectionsEqual(expectedFloor.FakeWalls, actualFloor.FakeWalls);
            this.AssertCollectionsEqual(expectedFloor.Doors, actualFloor.Doors);
            this.AssertCollectionsEqual(expectedFloor.GravityWaves, actualFloor.GravityWaves);
            this.AssertCollectionsEqual(expectedFloor.Chasms, actualFloor.Chasms);
            this.AssertCollectionsEqual(expectedFloor.Monsters, actualFloor.Monsters);
            this.AssertCollectionsEqual(expectedFloor.PowerUps, actualFloor.PowerUps);
            this.AssertCollectionsEqual(expectedFloor.QuantumPlasma, actualFloor.QuantumPlasma);
            // stairs down/up, PLAYER, weapon pick-up, data cube, ship core
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

        private void AssertCollectionsEqual(IEnumerable<AbstractEntity> expected, IEnumerable<AbstractEntity> actual)
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