using DeenGames.AliTheAndroid.Infrastructure.Common;

namespace  DeenGames.AliTheAndroid.Consoles.SubConsoleStrategies
{
    public class TopLevelMenuStrategy : ISubConsoleStrategy
    {
        public void Draw(SadConsole.Console console)
        {
            console.Print(2, 2, "[1] data cubes");
            console.Print(2, console.Height - 2, "[Q] quit");
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