using System.Collections.Immutable;
using DeenGames.AliTheAndroid.Enums;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class PlasmaDrive : AbstractEntity
    {
        internal const char DriveCharacter = (char)206; // ╬
        internal static readonly ImmutableArray<Color> Colours = ImmutableArray.Create(Palette.White, Palette.Aqua, Palette.Blue, Palette.Aqua);
        public PlasmaDrive(int x, int y) : base(x, y, DriveCharacter, Colours.ItemRef(0))
        {
        }
    }
}