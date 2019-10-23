using System.Collections.Generic;
using DeenGames.AliTheAndroid.Model.Entities;
using GoRogue.MapViews;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.Model.Entities
{
    [TestFixture]
    public class KlogborgShieldTests
    {
        [Test]
        public void OnMoveLeavesShieldValueIntactIfMonstersAreInFov()
        {
            // Arrange
            var shield = new KlogborgShield();
            shield.Damage(24);

            var fov = new GoRogue.FOV(new ArrayMap<bool>(10, 10));
            fov.Calculate(5, 5, 5);

            var monsters = new List<Entity>() { Entity.CreateFromTemplate("Zug", 6, 6) };
            
            // Act
            shield.OnMove(fov, monsters);

            // Assert
            Assert.That(shield.CurrentShield, Is.Not.EqualTo(Shield.MaxShield));
        }

        [Test]
        public void OnMoveResetsShieldToMaxIfNoMonstersInFov()
        {
            // Arrange
            var shield = new KlogborgShield();
            shield.Damage(24);

            var fov = new GoRogue.FOV(new ArrayMap<bool>(10, 10));
            fov.Calculate(5, 5, 5);

            var monsters = new List<Entity>() { Entity.CreateFromTemplate("Zug", 0, 2), Entity.CreateFromTemplate("Slink", 9, 8) };
            
            // Act
            shield.OnMove(fov, monsters);

            // Assert
            Assert.That(shield.CurrentShield, Is.EqualTo(Shield.MaxShield));
        }
    }
}