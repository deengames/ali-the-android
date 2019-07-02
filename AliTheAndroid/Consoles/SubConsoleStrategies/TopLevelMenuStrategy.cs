using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure.Common;

namespace  DeenGames.AliTheAndroid.Consoles.SubConsoleStrategies
{
    public class TopLevelMenuStrategy : ISubConsoleStrategy
    {
        public void Draw(SadConsole.Console console)
        {
            console.Print(2, 2, "[1] Review data cubes", Palette.White);
            console.Print(2, console.Height - 4, "[ESC] Back to game", Palette.White);
            console.Print(2, console.Height - 3, "[Q] Quit", Palette.White);
        }

        public void ProcessInput(IKeyboard keyboard)
        {
            if (keyboard.IsKeyPressed(Key.Q))
            {
                System.Environment.Exit(0);    
            }
        }
    }
}