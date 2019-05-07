using System;
using DeenGames.AliTheAndroid.Infrastructure.Sad;

namespace DeenGames.AliTheAndroid
{
    public static class Program
    {
        private const int GameWidthInTiles = 80;
        private const int GameHeightInTiles = 34;

        [STAThread]
        static void Main()
        {
            var console = new SadConsoleProgram(GameWidthInTiles, GameHeightInTiles);
            console.Run();
        }
    }
}
