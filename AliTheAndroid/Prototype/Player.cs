using System;
using AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Prototype;
using Microsoft.Xna.Framework;

namespace AliTheAndroid.Prototype
{
    public class Player : Entity
    {
        public static float ChargedShotTimeSeconds = 0.25f;

        public Direction DirectionFacing { get; private set; }
        public bool IsCharging { get; private set; }
        public DateTime? ChargeStartTime { get; private set; } = null;
        public bool CanMove {get; private set;} = true;

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

        public void Charge() {
            if (!this.IsCharging) {
                this.ChargeStartTime = DateTime.Now;
            }

            this.IsCharging = true;
        }

        public void Discharge() {
            this.IsCharging = false;
            this.ChargeStartTime = null;
        }

        public void Freeze() {
            this.CanMove = false;
        }

        public void Unfreeze() {
            this.CanMove = true;
        }

        internal void TurnCounterClockwise()
        {
            switch (this.DirectionFacing) {
                case Direction.Up:
                    this.DirectionFacing = Direction.Right;
                    break;
                case Direction.Right:
                    this.DirectionFacing = Direction.Down;
                    break;
                case Direction.Down:
                    this.DirectionFacing = Direction.Left;
                    break;
                case Direction.Left:
                    this.DirectionFacing = Direction.Up;
                    break;
            }
        }

        internal void TurnClockwise()
        {
            switch (this.DirectionFacing) {
                case Direction.Up:
                    this.DirectionFacing = Direction.Left;
                    break;
                case Direction.Left:
                    this.DirectionFacing = Direction.Down;
                    break;
                case Direction.Down:
                    this.DirectionFacing = Direction.Right;
                    break;
                case Direction.Right:
                    this.DirectionFacing = Direction.Up;
                    break;
            }
        }
    }
}