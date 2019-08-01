using System;
using System.IO;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Tests.Helpers;
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
        }
    }
}