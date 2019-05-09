using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Model.Entities;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class Plasma : AbstractEntity
    {
        private const int Lifespan = 10; // turns
        private int lifeLeft = Lifespan;

        public Plasma(int x, int y) : base(x, y, '.', Palette.Red)
        {
        }

        public void Degenerate()
        {
            if (this.lifeLeft > 0) {
                this.lifeLeft -= 1;
            }
        }

        public bool IsAlive { get { return this.lifeLeft > 0; } }
    }
}