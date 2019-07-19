using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Model.Entities;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.Model.Entities
{
    [TestFixture]
    public class AmeerTests
    {
        [TestCase(13)]
        [TestCase(56)]
        [TestCase(999999)]
        [TestCase(-1)]
        [TestCase(-1000)]
        public void DamageDoesntChangeHealth(int damage)
        {
            var ameer = new Ameer();
            ameer.Damage(damage, Weapon.Undefined);
            Assert.That(ameer.CurrentHealth, Is.EqualTo(ameer.TotalHealth));
        }
    }
}