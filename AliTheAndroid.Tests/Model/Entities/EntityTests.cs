using System;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Prototype;
using DeenGames.AliTheAndroid.Prototype.Enums;
using DeenGames.AliTheAndroid.Tests.Helpers;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.Model.Entities
{
    [TestFixture]
    public class Entitytests : AbstractTest
    {
        [TestCase("Alien")]
        [TestCase("Slink")]
        [TestCase("Zug")]
        public void CreateFromTemplateCreatesKnownEntities(string name)
        {
            var actual = Entity.CreateFromTemplate(name, 13, 10);
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.Name, Is.EqualTo(name));
        }   

        [Test]
        public void CreateFromTemplateThrowsForUnknownTypes()
        {
            Assert.Throws<ArgumentException>(() => Entity.CreateFromTemplate("Mushroom", 13, 10));
        }

        [Test]
        public void ConstructorSetsAppropriateValues()
        {
            //string name, char character, Color color, int x, int y, int health, int strength, int defense, int visionRange = 5
            var expectedName = "Space Marine";
            var expectedCharacter = '@';
            var expectedColor = Color.Bisque;
            var expectedX = 10;
            var expectedY = 28;
            var expectedHealth = 100;
            var expectedStrength = 10;
            var expectedDefense = 5;
            var expectedVision = 7;

            var actual = new Entity(expectedName, expectedCharacter, expectedColor, expectedX, expectedY, expectedHealth, expectedStrength, expectedDefense, expectedVision);
            
            Assert.That(actual.Name, Is.EqualTo(expectedName));
            Assert.That(actual.Character, Is.EqualTo(expectedCharacter));
            Assert.That(actual.Color, Is.EqualTo(expectedColor));
            Assert.That(actual.X, Is.EqualTo(expectedX));
            Assert.That(actual.Y, Is.EqualTo(expectedY));
            Assert.That(actual.TotalHealth, Is.EqualTo(expectedHealth));
            Assert.That(actual.CurrentHealth, Is.EqualTo(expectedHealth));
            Assert.That(actual.Strength, Is.EqualTo(expectedStrength));
            Assert.That(actual.Defense, Is.EqualTo(expectedDefense));
            Assert.That(actual.VisionRange, Is.EqualTo(expectedVision));
        }

        [Test]
        public void DieSetsEntityHealthCharacterAndColor()
        {
            var e = new Entity("Fodder", 'f', Color.Green, 0, 0, 17, 3, 1);
            e.Die();
            
            Assert.That(e.CurrentHealth, Is.Zero);
            Assert.That(e.Character, Is.EqualTo('%'));
            Assert.That(e.Color, Is.EqualTo(Palette.DarkBurgandyPurple));
        }

        [Test]
        public void DamageDoesNothingIfDamageIsNotPositive()
        {
            var e = new Entity("LavaMan", 'l', Color.Red, 0, 0, 10, 3, 1);
            e.Damage(-155);
            e.Damage(0);
            
            Assert.That(e.CurrentHealth, Is.EqualTo(e.TotalHealth));
        }

        [Test]
        public void DamageDamagesEntityIfDamageIsPositive()
        {
            var e = new Entity("LavaGuy", 'l', Color.Red, 0, 0, 20, 3, 1);
            var damage = 6;
            e.Damage(damage);
            Assert.That(e.CurrentHealth, Is.EqualTo(e.TotalHealth - damage));
        }

        [Test]
        public void DamageBroadcastsEventDeathIfHealthDropsToZero()
        {
            var e = new Entity("LavaFluff", 'l', Color.Red, 0, 0, 2, 3, 1);
            bool eventCalled = false;
            EventBus.Instance.AddListener(GameEvent.EntityDeath, (data) => eventCalled = true);

            e.Damage(999);

            Assert.That(eventCalled, Is.True);
        }

        [Test]
        public void IsDeadReturnsTrueIfHealthIsPositive()
        {
            var e = new Entity("Slime", 's', Color.Blue, 0, 0, 17, 3, 1);
            Assert.That(e.IsDead, Is.False);

            e.Die();
            Assert.That(e.IsDead, Is.True);
        }
    }
}