using DeenGames.AliTheAndroid.Enums;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class WeaponPickUp : AbstractEntity
    {
        public static readonly Color[] DisplayColors = new Color[] { Palette.Orange, Palette.YellowAlmost, Palette.White, Palette.YellowAlmost };
        public Weapon Weapon { get; private set; }

        public WeaponPickUp(int x, int y, Weapon weapon) : base(x, y, '&', Palette.White)
        {
            this.Weapon = weapon;
        }
    }
}