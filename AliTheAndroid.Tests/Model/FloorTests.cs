using System;
using System.Linq;
using AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Tests.Helpers;
using NUnit.Framework;
using Troschuetz.Random.Generators;

namespace AliTheAndroid.Tests.Model
{
    [TestFixture]
    public class FloorTests : AbstractTest
    {
        public void GenerateGeneratesPowerUpNearStairs()
        {
            // Good choice of seed: stairs have no adjacent floors, but extended search finds one.
            var floor = new Floor(50, 40, new StandardGenerator(1234), new Player());
                        
            Assert.That(floor.PowerUps.Count, Is.GreaterThan(0));
            var powerUp = floor.PowerUps.First();
            var distance = Math.Sqrt(Math.Pow(powerUp.X - floor.StairsLocation.X, 2) + Math.Pow(powerUp.Y - floor.StairsLocation.Y, 2));
            Assert.That(distance, Is.LessThanOrEqualTo(10));
        }
    }
}