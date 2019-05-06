using DeenGames.AliTheAndroid.Prototype.Enums;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class Wall : AbstractEntity
    {
        public Wall(int x, int y) : base(x, y, '#', Palette.LightGrey)
        {
        }
    }
}