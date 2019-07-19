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

        [TestCase(33)]
        [TestCase(2)]
        [TestCase(1)]
        public void AmeerIsStunnedIfTurnsStunnedAreNonZero(int turnsStunned)
        {
            var ameer = new Ameer();
            ameer.turnsLeftStunned = turnsStunned;            
            Assert.That(ameer.IsStunned);
            Assert.That(ameer.CanMove, Is.False);
        }

        [Test]
        public void AmeerIsNotStunnedIfTrunsStunnedIsZero()
        {
            var ameer = new Ameer();
            Assert.That(ameer.turnsLeftStunned, Is.Zero);
            Assert.That(ameer.IsStunned, Is.False);
            Assert.That(ameer.CanMove);
        }

        [Test]
        public void OnPlayerMovedDecrementsTurnsStunned()
        {
            var ameer = new Ameer();
            ameer.turnsLeftStunned = 2;

            ameer.OnPlayerMoved();
            Assert.That(ameer.turnsLeftStunned, Is.EqualTo(1));

            ameer.OnPlayerMoved();
            Assert.That(ameer.turnsLeftStunned, Is.EqualTo(0));
            Assert.That(ameer.IsStunned, Is.False);
            Assert.That(ameer.CanMove);
        }

        [Test]
        public void DamageStunsIfSourceIsZapper()
        {
            var ameer = new Ameer();
            ameer.Damage(0, Weapon.Zapper);
            Assert.That(ameer.IsStunned);
            Assert.That(ameer.turnsLeftStunned, Is.GreaterThan(0));
        }
    }
}