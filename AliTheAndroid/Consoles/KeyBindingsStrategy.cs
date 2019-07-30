using System;
using System.Collections.Generic;
using DeenGames.AliTheAndroid.Accessibility;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Consoles.SubConsoleStrategies
{
    // Not a real, true, pure strategy; just a draw/update thing.
    public class KeyBindingsStrategy : AbstractConsole
    {
        private readonly Color SelectedColour = Palette.Blue;
        private readonly Color SelectedValueColour = Palette.OffWhite;
        private readonly Color UnselectedColour = Palette.DarkBlueMuted;
        private readonly Color UnselectedValueColour = Palette.Grey;

        private readonly SadConsole.Cell BorderCell = new SadConsole.Cell(Palette.White, Palette.White, ' ');        
        private readonly SadConsole.Cell BackgroundCell = new SadConsole.Cell(Palette.BlackAlmost, Palette.BlackAlmost, ' ');
        private int selectedIndex = 0;
        private List<ConfigurableControl> controls = new List<ConfigurableControl>();

        public KeyBindingsStrategy()
        {
            var enumerator = Options.KeyBindings.Keys.GetEnumerator();

            do
            {
                controls.Add(enumerator.Current);
            } while (enumerator.MoveNext());
            
        }

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
            if (keyboard.IsKeyPressed(Options.KeyBindings[ConfigurableControl.MoveUp]))
            {
                this.selectedIndex--;
                if (this.selectedIndex == -1)
                {
                    this.selectedIndex = this.controls.Count - 1;
                }
            }
            if (keyboard.IsKeyPressed(Options.KeyBindings[ConfigurableControl.MoveDown]))
            {
                this.selectedIndex = (this.selectedIndex + 1) % this.controls.Count;
            }
        }

        private void PrintBinding(SadConsole.Console console, ConfigurableControl control, Key boundKey, int y)
        {
            var selectedItem = this.controls[this.selectedIndex];
            var nameColour = control == selectedItem ? SelectedColour : UnselectedColour;
            var keyColour = control == selectedItem ? SelectedValueColour : UnselectedValueColour;

            console.Print(2, y, $"{control.ToString()}: ", nameColour);
            console.Print(2 + control.ToString().Length + 2, y, boundKey.ToString(), keyColour);
        }
    }
}