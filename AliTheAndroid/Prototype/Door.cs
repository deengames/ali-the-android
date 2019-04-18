using DeenGames.AliTheAndroid.Enums;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Prototype {
    public class Door : AbstractEntity
    {
        private bool isLocked = false;
        private bool isOpened = false;

        public bool IsOpened { get { return this.isOpened; }
        set {
            this.isOpened = value;
            this.Character = this.IsOpened ? '-' : '+';
        }}

        public Door(int x, int y, bool isLocked = false) : base(x, y, '+', Palette.YellowAlmost)
        {
            this.isLocked = isLocked;
            this.Color = isLocked ? Palette.Orange : Palette.YellowAlmost;
        }
    }
}