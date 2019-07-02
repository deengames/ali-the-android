using DeenGames.AliTheAndroid.Infrastructure.Common;

namespace  DeenGames.AliTheAndroid.Consoles.SubConsoleStrategies
{
    public interface ISubConsoleStrategy
    {
        void Draw(SadConsole.Console console);
        void ProcessInput(IKeyboard keyboard);
    }
}