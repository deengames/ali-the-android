using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Model.Entities
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
    }
}