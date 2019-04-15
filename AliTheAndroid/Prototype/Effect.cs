using AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Enums;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace DeenGames.AliTheAndroid.Prototype
{
    public abstract class Effect : AbstractEntity
    {
        private int tickEveryMilliseconds = 0;
        private DateTime lastTickOn = DateTime.Now;

        public Effect(int x, int y, char character, Color color, int tickEveryMs) : base(x, y, character, color)
        {
            this.tickEveryMilliseconds = tickEveryMs;
        }

        public void OnUpdate()
        {
            var now = DateTime.Now;
            var elapsed = now - this.lastTickOn;
            if (elapsed.TotalMilliseconds >= this.tickEveryMilliseconds) {
                this.OnAction();
                this.lastTickOn = now;
            }
        }

        internal abstract void OnAction();
    }

    public class Shot : Effect
    {
        private const int ShotUpdateTimeMs = 100; // 100ms
        private Direction direction;

        public Shot(int x, int y, char character, Color color, Direction direction) : base(x, y, character, Palette.Red, ShotUpdateTimeMs)
        {
            this.direction = direction;
        }

        override internal void OnAction()
        {
            switch (this.direction) {
                case Direction.Up:
                    this.Y -= 1;
                    break;
                case Direction.Right:
                    this.X += 1;
                    break;
                case Direction.Down:
                    this.Y += 1;
                    break;
                case Direction.Left:
                    this.X -= 1;
                    break;
            }
        }
    }
}