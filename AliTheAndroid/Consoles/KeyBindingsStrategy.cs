using System;
using DeenGames.AliTheAndroid.Accessibility;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure.Common;

namespace DeenGames.AliTheAndroid.Consoles.SubConsoleStrategies
{
    // Not a real, true, pure strategy; just a draw/update thing.
    public class KeyBindingsStrategy : AbstractConsole
    {
        private readonly SadConsole.Cell BorderCell = new SadConsole.Cell(Palette.White, Palette.White, ' ');        
        private readonly SadConsole.Cell BackgroundCell = new SadConsole.Cell(Palette.BlackAlmost, Palette.BlackAlmost, ' ');

        public void Draw(SadConsole.Console console)
        {
            console.Print(2, 2, $"{Options.KeyBindings[ConfigurableControl.MoveUp]}/{Options.KeyBindings[ConfigurableControl.MoveDown]} to move, {Options.KeyBindings[ConfigurableControl.SkipTurn]} to rebind, {Options.KeyBindings[ConfigurableControl.OpenMenu]} to go back", Palette.OffWhite);

            var nextY = 4;
            foreach (var value in Enum.GetValues(typeof(ConfigurableControl)))
            {
                var control = (ConfigurableControl)value;
                this.PrintBinding(console, control, Options.KeyBindings[control], nextY);
                nextY++;
            }
        }

        internal void ProcessInput(IKeyboard keyboard)
        {
        }

        private void PrintBinding(SadConsole.Console console, ConfigurableControl control, Key boundKey, int y)
        {
            console.Print(2, y, $"{control.ToString()}: ", Palette.Blue);
            console.Print(2 + control.ToString().Length + 2, y, boundKey.ToString(), Palette.OffWhite);
        }
    }
}