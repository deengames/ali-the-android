using AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Enums;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace DeenGames.AliTheAndroid.Prototype
{
    public abstract class Effect : AbstractEntity
    {
        public bool IsAlive {get; set;} = true;

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
        private Func<int, int, bool, bool> isWalkableCheck;

        public Shot(int x, int y, char character, Color color, Direction direction, Func<int, int, bool, bool> isWalkable) : base(x, y, character, Palette.Red, ShotUpdateTimeMs)
        {
            this.direction = direction;
            this.isWalkableCheck = isWalkable;
        }

        override internal void OnAction()
        {
            if (this.isWalkableCheck(this.X, this.Y, false))
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

    public class Explosion : Effect
    {
        private const int LifetimeMs = 100;

        public Explosion(int x, int y) : base(x, y, '*', Palette.Orange, LifetimeMs)
        {
        }

        internal override void OnAction()
        {
            this.IsAlive = false;
        }
    }

    public class Bolt : Explosion
    {
        public Bolt(int x, int y) : base(x, y)
        {
            this.Character = '$';
            this.Color = Palette.Blue;
        }
    }
}