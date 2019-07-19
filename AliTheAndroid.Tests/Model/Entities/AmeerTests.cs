using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Model.Events;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.Model.Entities
{
    [TestFixture]
    public class AmeerTests
    {
        [TestCase(13, Weapon.Blaster)]
        [TestCase(56, Weapon.GravityCannon)]
        [TestCase(999, Weapon.MiniMissile)]
        [TestCase(1323, Weapon.PlasmaCannon)]
        [TestCase(35413, Weapon.Undefined)]        
        [TestCase(145653, Weapon.Zapper)]        
        [TestCase(-1, Weapon.Undefined)]
        [TestCase(-1000, Weapon.Undefined)]
        public void DamageDoesntChangeHealth(int damage, Weapon source)
        {
            var ameer = new Ameer();
            ameer.Damage(damage, source);
            Assert.That(ameer.CurrentHealth, Is.EqualTo(ameer.TotalHealth));
        }

        [TestCase(1)]
        [TestCase(0)]
        [TestCase(-183)]
        [TestCase(999999999)]
        public void AmeerDiesIfTouchedByQuantumPlasma(int damage)
        {
            object whoDied = null;
            EventBus.Instance.AddListener(GameEvent.EntityDeath, (data) => whoDied = data);
            var ameer = new Ameer();
            ameer.Damage(damage, Weapon.QuantumPlasma);

            Assert.That(ameer.CurrentHealth, Is.LessThanOrEqualTo(0));
            Assert.That(whoDied, Is.EqualTo(ameer));
        }
    }
}