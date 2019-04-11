using AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Prototype;
using Microsoft.Xna.Framework;

namespace AliTheAndroid.Prototype
{
    public class Player : Entity
    {
        public Direction DirectionFacing { get; private set; }

        public Player() : base("You", '@', Color.White, 50, 7, 5, 4)
        {
            this.DirectionFacing = Direction.Up;
        }

        public void OnMove(int previousX, int previousY)
        {
            var dx = this.X - previousX;
            var dy = this.Y - previousY;;

            // Naive and not error-proof; correct if we only move in one direction at a time
            if (dx == 0) {
                this.DirectionFacing = dy < 0 ? Direction.Up : Direction.Down;
            } else {
                this.DirectionFacing = dx < 0 ? Direction.Left : Direction.Right;
            }
        }
    }
}