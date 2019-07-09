using System.Collections.Generic;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Model.Events;
using DeenGames.AliTheAndroid.Tests.Helpers;
using NUnit.Framework;

namespace DeenGames.AliTheAndroid.Tests.Model.Entities
{
    [TestFixture]
    public class EggTests : AbstractTest
    {
        [Test]
        public void TryToHatchBroadcastsHatchedEvent()
        {
            var egg = new Egg(10, 10, Options.CurrentPalette.Monster3Colour);
            bool isHatched = false;
            var player = new Player();
            var monsters = new List<Entity>();

            EventBus.Instance.AddListener(GameEvent.EggHatched, (e) => isHatched = true);
            int iterationsLeft = 100;
            while (iterationsLeft-- > 0 && !isHatched)
            {
                EventBus.Instance.Broadcast(GameEvent.PlayerTookTurn, new PlayerTookTurnData(player, monsters));
            }

            Assert.That(iterationsLeft > 0);
            Assert.That(isHatched);
        }
    }
}