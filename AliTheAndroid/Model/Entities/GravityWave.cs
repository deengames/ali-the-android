using System;
using System.Collections.Generic;
using System.Linq;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Model.Events;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class GravityWave : AbstractEntity
    {
        private static Random random = new Random();
        private const char GravityCharacter = (char)247;
        private Func<int, int, bool> isWalkableCheck;
        private int floorNum;

        public GravityWave(int x, int y, int floorNum, Func<int, int, bool> isWalkableCheck) : base(x, y, GravityCharacter, Palette.LightLilacPink)
        {
            this.floorNum = floorNum;
            this.isWalkableCheck = isWalkableCheck;
            EventBus.Instance.AddListener(GameEvent.PlayerTookTurn, this.PerturbOccupantOnMove);
        }

        private void PerturbOccupantOnMove(object obj)
        {
            var data = obj as PlayerTookTurnData;
            var monsters = data.Monsters;
            var player = data.Player;

            if (this.floorNum == Dungeon.Instance.CurrentFloorNum)
            {

                var myMonster = monsters.SingleOrDefault(m => m.X == this.X && m.Y == this.Y);
                if (myMonster != null)
                {
                    var moves = this.WhereCanIMove(myMonster);
                    if (moves.Any())
                    {
                        var move = moves[random.Next(moves.Count)];
                        myMonster.X = move.X;
                        myMonster.Y = move.Y;
                    }
                }

                if (player.X == this.X && player.Y == this.Y)
                {
                    var moves = this.WhereCanIMove(player);
                    if (moves.Any()) {
                        var move = moves[random.Next(moves.Count)];
                        player.X = move.X;
                        player.Y = move.Y;
                    }
                }
            }
        }
        
        private List<GoRogue.Coord> WhereCanIMove(Entity e)
        {
            var toReturn = new List<GoRogue.Coord>();

            if (isWalkableCheck(e.X - 1, e.Y)) { toReturn.Add(new GoRogue.Coord(e.X - 1, e.Y)); }
            if (isWalkableCheck(e.X +1, e.Y)) { toReturn.Add(new GoRogue.Coord(e.X + 1, e.Y)); }
            if (isWalkableCheck(e.X, e.Y - 1)) { toReturn.Add(new GoRogue.Coord(e.X, e.Y - 1)); }
            if (isWalkableCheck(e.X, e.Y + 1)) { toReturn.Add(new GoRogue.Coord(e.X, e.Y + 1)); }

            return toReturn;
        }

        public void StopReactingToPlayer()
        {
            EventBus.Instance.RemoveListener(GameEvent.PlayerTookTurn, this);
        }
    }
}