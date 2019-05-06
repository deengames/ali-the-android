using DeenGames.AliTheAndroid.Entities;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.Entities {
    
    [TestFixture]
    public class AbstractEntitytests {
        
        [Test]
        public void ConstructorSetsValuesToPassedInValues()
        {
            var x = 1;
            var y = 2;
            var character = '6';
            var color = Color.AliceBlue;
            var entity = new EmptyEntity(x, y, character, color);

            Assert.That(entity.X, Is.EqualTo(x));
            Assert.That(entity.Y, Is.EqualTo(y));
            Assert.That(entity.Character, Is.EqualTo(character));
            Assert.That(entity.Color, Is.EqualTo(color));
        }
    }

    public class EmptyEntity : AbstractEntity
    {
        public EmptyEntity(int x, int y, char character, Color color) : base(x, y, character, color)
        {
        }
    }
}