using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Model.Entities;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.Model.Entities
{
    [TestFixture]
    public class PlayerTests
    {
        [Test]
        public void AbsorbAbsorbsPowerUpStats()
        {
            var powerUp = new PowerUp(0, 0, false, 1, 2, 3, 5);
            
            var player = new Player();
            var startHealth = player.TotalHealth;
            var startStrength = player.Strength;
            var startDefense = player.Defense;
            var startVision = player.VisionRange;

            // Act
            player.Absorb(powerUp);

            // Assert
            Assert.That(player.TotalHealth, Is.EqualTo(startHealth + powerUp.HealthBoost));
            Assert.That(player.CurrentHealth, Is.EqualTo(startHealth + powerUp.HealthBoost));
            Assert.That(player.Strength, Is.EqualTo(startStrength + powerUp.StrengthBoost));
            Assert.That(player.Defense, Is.EqualTo(startDefense + powerUp.DefenseBoost));
            Assert.That(player.VisionRange, Is.EqualTo(startVision + powerUp.VisionBoost));
        }

        [Test]
        public void PlayerStartsWithBlaster()
        {
            var player = new Player();
            Assert.That(player.Has(Weapon.Blaster), Is.True);
        }

        [Test]
        public void HasReturnsTrueForAcquiredWeapons()
        {
            var player = new Player();
            player.Acquire(Weapon.PlasmaCannon);

            Assert.That(player.Has(Weapon.PlasmaCannon), Is.True);
            Assert.That(player.Has(Weapon.GravityCannon), Is.False);
        }
    }

}