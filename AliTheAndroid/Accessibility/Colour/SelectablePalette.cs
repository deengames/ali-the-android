using System.Collections.Generic;
using System.Collections.Immutable;
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
            Monster4Colour = Palette.LimeGreen,
            PowerUpColours =  ImmutableList.Create(Palette.White, Palette.LightLilacPink, Palette.LilacPinkPurple, Palette.Purple),
            WeaponColours = ImmutableList.Create(Palette.Orange, Palette.YellowAlmost, Palette.White, Palette.YellowAlmost),
            DataCubeColours = ImmutableList.Create(Palette.White, Palette.Cyan, Palette.Blue),            
        };

        public static SelectablePalette SaturatedPalette = new SelectablePalette() {
            Monster1Colour = Palette.YellowAlmost,
            Monster2Colour = Palette.Aqua,
            Monster3Colour = Palette.Purple,
            Monster4Colour = Palette.Red,
            PowerUpColours =  ImmutableList.Create(Palette.White, Palette.Purple),
            WeaponColours = ImmutableList.Create(Palette.White, Palette.Orange),
            DataCubeColours = ImmutableList.Create(Palette.Blue, Palette.White),
        };

        public Color Monster1Colour { get; private set; }
        public Color Monster2Colour { get; private set; }
        public Color Monster3Colour { get; private set; }
        public Color Monster4Colour { get; private set; }
        public ImmutableList<Color> PowerUpColours { get; private set; }
        public ImmutableList<Color> WeaponColours { get; private set; }
        public ImmutableList<Color> DataCubeColours { get; private set; }
    }
}