using System;
using DeenGames.AliTheAndroid.Infrastructure.Sad;

namespace DeenGames.AliTheAndroid
{
    public static class Program
    {
        internal const int GameWidthInTiles = 80;
        internal const int GameHeightInTiles = 28;

        [STAThread]
        static void Main()
        {
            var program = new SadConsoleProgram(GameWidthInTiles, GameHeightInTiles);
            program.Run();
        }
    }
}
