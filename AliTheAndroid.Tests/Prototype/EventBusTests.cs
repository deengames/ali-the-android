using System;
using DeenGames.AliTheAndroid.Prototype;
using DeenGames.AliTheAndroid.Prototype.Enums;
using DeenGames.AliTheAndroid.Tests.Helpers;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.Prototype
{
    [TestFixture]
    public class EventBusTests : AbstractTest
    {
        [Test]
        public void BroadcastEventBroadcastsToListeners()
        {
            // Arrange
            var timesCalled = 0;            
            EventBus.Instance.AddListener(GameEvent.PlayerTookTurn, (data) => timesCalled++);

            // Act
            EventBus.Instance.Broadcast(GameEvent.EntityDeath, null);
            EventBus.Instance.Broadcast(GameEvent.PlayerTookTurn, null);

            Assert.That(timesCalled, Is.EqualTo(1));
        }

        [Test]
        public void RemoveEventListenerRemovesListener()
        {
            // Arrange
            var timesCalled = 0;            
            Action<object> incrementMethod = (data) => timesCalled++;
            EventBus.Instance.AddListener(GameEvent.PlayerTookTurn, incrementMethod);
            EventBus.Instance.Broadcast(GameEvent.PlayerTookTurn, null); // timesCalled = 1

            // Act
            EventBus.Instance.RemoveListener(GameEvent.PlayerTookTurn, incrementMethod);
            EventBus.Instance.Broadcast(GameEvent.PlayerTookTurn, null); // timesCalled = 1
            EventBus.Instance.Broadcast(GameEvent.PlayerTookTurn, null); // timesCalled = 1
            EventBus.Instance.Broadcast(GameEvent.PlayerTookTurn, null); // timesCalled = 1

            Assert.That(timesCalled, Is.EqualTo(1));
        }
    }
}