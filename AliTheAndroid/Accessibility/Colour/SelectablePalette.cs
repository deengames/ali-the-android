using DeenGames.AliTheAndroid.Enums;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Accessibility.Colour
{
    public class SelectablePalette
    {
        public static SelectablePalette StandardPalette = new SelectablePalette() {
            Monster1Colour = Palette.Blue,
            Monster2Colour = Palette.Aqua,
            Monster3Colour = Palette.Cyan,
            Monster4Colour = Palette.CyanMuted,
        };

        public static SelectablePalette SaturatedPalette = new SelectablePalette() {
            Monster1Colour = Palette.YellowAlmost,
            Monster2Colour = Palette.Aqua,
            Monster3Colour = Palette.Purple,
            Monster4Colour = Palette.Red,
        };

        public Color Monster1Colour { get; private set; }
        public Color Monster2Colour { get; private set; }
        public Color Monster3Colour { get; private set; }
        public Color Monster4Colour { get; private set; }
    }
}