using System;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Model.Events;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class Egg : Entity
    {
        private static readonly Random random = new Random();
        private const int hatchProbability = 25; // 20 => 20%

        const char DisplayCharacter = (char)233; // Θ
        public Egg(int x, int y, Color tenLegsColour) : base("Egg", DisplayCharacter, tenLegsColour, x, y, 1, 0, 0, 0)
        {
            EventBus.Instance.AddListener(GameEvent.PlayerTookTurn, this.TryToHatch);
        }

        public void TryToHatch(object obj)
        {
            if (random.Next(0, 100) <= hatchProbability)
            {
                var data = obj as PlayerTookTurnData;
                var monsters = data.Monsters;
                var player = data.Player;

                EventBus.Instance.Broadcast(GameEvent.EggHatched, new GoRogue.Coord(this.X, this.Y));
                EventBus.Instance.RemoveListener(GameEvent.PlayerTookTurn, this);
            }
        }

        override public void Die()
        {
            base.Die();
            EventBus.Instance.RemoveListener(GameEvent.PlayerTookTurn, this);
        }
    }
}