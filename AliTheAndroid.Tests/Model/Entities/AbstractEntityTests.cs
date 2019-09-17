using System;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Model.Entities;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.Model.Entities
{
    
    [TestFixture]
    public class AbstractEntitytests
    {
        
        [Test]
        public void ConstructorSetsValuesToPassedInValues()
        {
            var x = 1;
            var y = 2;
            var character = '6';
            var color = Color.AliceBlue;
            var entity = new AbstractEntity(x, y, character, color);

            Assert.That(entity.X, Is.EqualTo(x));
            Assert.That(entity.Y, Is.EqualTo(y));
            Assert.That(entity.Character, Is.EqualTo(character));
            Assert.That(entity.Color, Is.EqualTo(color));
        }

        [TestCase(SimpleEntity.Chasm, ' ')]
        [TestCase(SimpleEntity.Wall, (char)46)]
        public void CreateCreatesKnownEntities(SimpleEntity type, char expectedCharacter)
        {
            var actual = AbstractEntity.Create(type, 0, 0);
            Assert.That(actual.Character, Is.EqualTo(expectedCharacter));
        }

        public void CreateThrowsForUnknownSimpleEntityValues()
        {
            var unknown = (SimpleEntity)137;
            Assert.Throws<ArgumentException>(() => AbstractEntity.Create(unknown, 0, 0));
        }
    }
}