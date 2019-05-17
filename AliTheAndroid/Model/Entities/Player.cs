using System;
using System.Collections.Generic;
using AliTheAndroid.Enums;
using Microsoft.Xna.Framework;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Enums;

namespace AliTheAndroid.Model.Entities
{
    public class Player : Entity
    {
        public Direction DirectionFacing { get; private set; }
        public Weapon CurrentWeapon = Weapon.Blaster;
        public bool HasEnvironmentSuit = false;
        public bool CanFireGravityCannon { get; set; } = true;

        public Player() : base("You", '@', Color.White, 0, 0, 50, 70, 50, 4)
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

        internal void TurnClockwise()
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

        public void Absorb(PowerUp powerUp)
        {
            this.CurrentHealth += powerUp.HealthBoost;
            this.TotalHealth += powerUp.HealthBoost;
            this.Strength += powerUp.StrengthBoost;
            this.Defense += powerUp.DefenseBoost;
            this.VisionRange += powerUp.VisionBoost;
        }
    }
}