using DeenGames.AliTheAndroid.Model.Entities;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.Model.Entities
{
    [TestFixture]
    public class PowerUpTests
    {
        [Test]
        public void PickUpInvokesOnPickUpCallback()
        {
            var powerUp = new PowerUp(10, 17, 103);
            var wasCalled = false;
            powerUp.OnPickUp(() => wasCalled = true);

            powerUp.PickUp();

            Assert.That(wasCalled, Is.True);
        }

        [Test]
        public void PickUpDoesNothingIfCallbackIsNull()
        {
            var powerUp = new PowerUp(0, 0, 10);
            Assert.DoesNotThrow(() => powerUp.PickUp());
        }

        [Test]
        public void PairSymmetricallyPairsPowerUps()
        {
            var p1 = new PowerUp(0, 0, 100);
            var p2 = new PowerUp(0, 0, 50);
            PowerUp.Pair(p1, p2);

            Assert.That(p1.PairedTo, Is.EqualTo(p2));
            Assert.That(p2.PairedTo, Is.EqualTo(p1));

            p1.OnPickUp(() => p1.PairedTo.Character = 'X');
            p1.PickUp();
            
            Assert.That(p2.Character, Is.EqualTo('X'));
        }
    }
}