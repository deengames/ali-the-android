using System.Collections;
using System.Collections.Generic;
using AliTheAndroid.Prototype;
using DeenGames.AliTheAndroid.Model.Entities;

namespace DeenGames.AliTheAndroid.EventData
{
    public class PlayerTookTurnData
    {
        public Player Player { get; private set; }
        public IList<Entity> Monsters { get; private set; }

        public PlayerTookTurnData(Player player, IList<Entity> monsters)
        {
            this.Player = player;
            this.Monsters = monsters;
        }
    }
}