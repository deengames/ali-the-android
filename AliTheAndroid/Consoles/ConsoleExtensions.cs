using Microsoft.Xna.Framework;
using SadConsole;

namespace DeenGames.AliTheAndroid.Consoles {
    public static class ConsoleExtensions
    {
        public static void DrawCharacter(this SadConsole.Console console, float x, float y, char character, Color color)
        {
            console.DrawCharacter(x, y, character, color, DeenGames.AliTheAndroid.Enums.Palette.BlackAlmost);
        }

        public static void DrawCharacter(this SadConsole.Console console, float x, float y, char character, Color color, Color backgroundColour)
        {
            var intX = (int)x;
            var intY = (int)y;
            // TODO: we should probably cache Cell instances, I'm sure this will hit the GC hard.
            console.SetCellAppearance(intX, intY, new Cell() { Background = backgroundColour, Foreground = color });
            console.SetGlyph(intX, intY, character);
        }
    }
}