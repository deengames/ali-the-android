using DeenGames.AliTheAndroid.Model.Entities;
using NUnit.Framework;
using Troschuetz.Random.Generators;
using System.Linq;
using System.Collections.Generic;

namespace DeenGames.AliTheAndroid.Tests.Model.Entities
{
    [TestFixture]
    public class PowerUpTests
    {
        [Test]
        public void PickUpInvokesOnPickUpCallback()
        {
            var powerUp = new PowerUp(10, 17, '^', false, 103);
            var wasCalled = false;
            powerUp.OnPickUp(() => wasCalled = true);

            powerUp.PickUp();

            Assert.That(wasCalled, Is.True);
        }

        [Test]
        public void PickUpDoesNothingIfCallbackIsNull()
        {
            var powerUp = new PowerUp(0, 0, '^', false, 10);
            Assert.DoesNotThrow(() => powerUp.PickUp());
        }

        [Test]
        public void PairSymmetricallyPairsPowerUps()
        {
            var p1 = new PowerUp(0, 0, '^', false, 100);
            var p2 = new PowerUp(0, 0, '^', false, 50);
            PowerUp.Pair(p1, p2);

            Assert.That(p1.PairedTo, Is.EqualTo(p2));
            Assert.That(p2.PairedTo, Is.EqualTo(p1));

            p1.OnPickUp(() => p1.PairedTo.Character = 'X');
            p1.PickUp();
            
            Assert.That(p2.Character, Is.EqualTo('X'));
        }

        // https://trello.com/c/EwrmFvYA/100-paired-power-ups-are-always-the-same-type
        [Test]
        public void GenerateGeneratesDifferentPowerUps()
        {
            var powerUps = new List<PowerUp>();
            var random = new StandardGenerator(98465865);

            for (int i = 0; i < 10; i++)
            {
                powerUps.Add(PowerUp.Generate(random));
            }

            var distinct = powerUps.Select(p => p.Message).Distinct();
            Assert.That(distinct.Count() == 3 || distinct.Count() == 4, // 4 types, should get 3-4 distinct types
                $"Expected 3-4 types of power-ups but got {distinct.Count()}: {System.String.Join(", ", distinct)}"); 
        }

        // https://trello.com/c/XU4p02Lk/135-power-ups-behind-a-chasm-arent-paired-if-you-load-game
        // Root cause: PowerUp.ctor assignes PairedTo assymetrically; apply it symmetrically.
        [Test]
        public void ConstructorSymmetricallyPairsPowerUps()
        {
            var generator = new StandardGenerator(12311);
            var p1 = PowerUp.Generate(generator);
            var p2 = new PowerUp(0, 0, 'P', true, 99, 88, 77, 66, p1);
            Assert.That(p2.PairedTo, Is.EqualTo(p1));
            Assert.That(p1.PairedTo, Is.EqualTo(p2));
        }
    }
}