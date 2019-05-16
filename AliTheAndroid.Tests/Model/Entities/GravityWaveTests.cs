using System;
using System.Collections.Generic;
using AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Model.Events;
using DeenGames.AliTheAndroid.Tests.Helpers;
using NUnit.Framework;

namespace AliTheAndroid.Tests.Model.Entities
{
    [TestFixture]
    public class GravityWaveTests : AbstractTest
    {
        [Test]
        public void GravityWaveMovesMonstersOnItToWalkableLocationOnPlayerTookTurnEvent()
        {
            new Dungeon(40, 30, gameSeed: 98723).GoToNextFloor(); // Start on 1B / floor 0

            // Arrange
            var wave = new GravityWave(3, 3, 0, FakeIsWalkable);
            var zug = Entity.CreateFromTemplate("Zug", wave.X, wave.Y);
            var monsters = new List<Entity>() { zug };
            var player = new Player();

            // Act
            EventBus.Instance.Broadcast(GameEvent.PlayerTookTurn, new PlayerTookTurnData(player, monsters));

            // Assert
            Assert.That(zug.X >= 2 && zug.X <= 4 && zug.Y >= 2 && zug.Y <= 4, $"Zug on gravity wave at (3, 3) moved to invalid spot: {zug.X}, {zug.Y}");
            Assert.That(zug.X != wave.X || zug.Y != wave.Y, "Zug on gravity wave at (3, 3) didn't move");
        }

        public void GravityWaveMovesPlayerOnItToWalkableLocationOnPlayerTookTurnEvent()
        {
            // Arrange
            var wave = new GravityWave(3, 3, 1, FakeIsWalkable);
            var monsters = new List<Entity>();
            var player = new Player();
            player.X = wave.X;
            player.Y = wave.Y;

            // Act
            EventBus.Instance.Broadcast(GameEvent.PlayerTookTurn, new PlayerTookTurnData(player, monsters));

            // Assert
            Assert.That(player.X >= 2 && player.X <= 4 && player.Y >= 2 && player.Y <= 4, $"player on gravity wave at (3, 3) moved to invalid spot: {player.X}, {player.Y}");
            Assert.That(player.X != wave.X || player.Y != wave.Y, "player on gravity wave at (3, 3) didn't move");
        }

        private bool FakeIsWalkable(int x, int y)
        {
            // Pretend it's a 10x5 map with a 1-cell wall border
            // Map is (0, 0) to (10, 5); walkable is (1, 1) to (9, 4)

            return x > 0 && x < 9 && y > 0 && y < 4;
        }
    }
}