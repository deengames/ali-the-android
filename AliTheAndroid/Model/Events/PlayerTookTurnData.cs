using System.Collections;
using System.Collections.Generic;
using AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Model.Entities;

namespace DeenGames.AliTheAndroid.Model.Events
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