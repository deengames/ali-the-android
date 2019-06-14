using System;
using DeenGames.AliTheAndroid.Infrastructure.Sad;

namespace DeenGames.AliTheAndroid
{
    public static class Program
    {
        private const int GameWidthInTiles = 80;
        private const int GameHeightInTiles = 30;

        [STAThread]
        static void Main()
        {
            var program = new SadConsoleProgram(GameWidthInTiles, GameHeightInTiles);
            program.Run();
        }
    }
}
