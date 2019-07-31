using System;
using System.Linq;
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
        
        internal bool IsBindingKey = false;

        private int selectedIndex = 0;
        private List<ConfigurableControl> controls = new List<ConfigurableControl>();
        // State within a state within a state... are we changing a key?

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
            var nextY = 4;
            foreach (var value in Enum.GetValues(typeof(ConfigurableControl)))
            {
                var control = (ConfigurableControl)value;
                this.PrintBinding(console, control, Options.KeyBindings[control], nextY);
                nextY++;
            }

            if (IsBindingKey)
            {
                this.DrawBox(new Rectangle(1, 1, this.Width - 2, 3), BackgroundCell, BackgroundCell);
                var currentKey = this.controls[this.selectedIndex];
                console.Print(2, 2, $"Rebinding ", SelectedColour);
                console.Print(2 + "Rebinding".Length + 1, 2, currentKey.ToString(), SelectedValueColour);
                console.Print(2 + "Rebinding".Length + currentKey.ToString().Length + 1, 2, $" => {Options.KeyBindings[currentKey]}", SelectedColour);
                console.Print(2, 3, $"Press key or {Options.KeyBindings[ConfigurableControl.OpenMenu]} to cancel", SelectedColour);
            }
            else
            {
                console.Print(2, 2, $"{Options.KeyBindings[ConfigurableControl.MoveUp]}/{Options.KeyBindings[ConfigurableControl.MoveDown]} to move, {Options.KeyBindings[ConfigurableControl.SkipTurn]} to rebind, {Options.KeyBindings[ConfigurableControl.OpenMenu]} to go back", Palette.OffWhite);
            }
        }

        internal void ProcessInput(IKeyboard keyboard)
        {
            if (IsBindingKey)
            {
                if (keyboard.IsKeyPressed(Options.KeyBindings[ConfigurableControl.OpenMenu]))
                {
                    this.IsBindingKey = false;
                    // Don't allow the parent (keybindings console) to abort back to the options menu
                    keyboard.Clear();
                }
                else
                {
                    var keys = keyboard.GetKeysPressed();
                    if (keys.Any())
                    {
                        // Rebind
                        var selectedItem = this.controls[selectedIndex];
                        Options.KeyBindings[selectedItem] = keys.First();
                        this.IsBindingKey = false;
                    }
                }
            }
            else
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
                if (keyboard.IsKeyPressed(Options.KeyBindings[ConfigurableControl.SkipTurn]))
                {
                    this.IsBindingKey = true;
                }
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