using System;
using System.Collections.Generic;
using System.IO;
using DeenGames.AliTheAndroid.Accessibility;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Model.Events;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ninject;

namespace  DeenGames.AliTheAndroid.Consoles.SubConsoleStrategies
{
    public class OptionsMenuStrategy : AbstractConsole, ISubConsoleStrategy
    {
        private readonly Color EnabledColour = Palette.Cyan;
        private readonly Color DisabledColour = Palette.Grey;

        private readonly SadConsole.Cell BorderCell = new SadConsole.Cell(Palette.White, Palette.White, ' ');        
        private readonly SadConsole.Cell BackgroundCell = new SadConsole.Cell(Palette.BlackAlmost, Palette.BlackAlmost, ' ');

        // This class operates in two modes: when called from the main menu, and when called from the in-game menu.
        // For the main menu, this value is non-null. The choice of state also changes how we draw stuff.
        private Action onCloseCallback;
        private KeyBindingsStrategy keyBindingsConsole = null;


        // Used by in-game menu
        public OptionsMenuStrategy(Player player)
        {
            
        }

        // Used by title screen
        public OptionsMenuStrategy(Action onCloseCallback)
        {
            this.onCloseCallback = onCloseCallback;
        }

        public void Draw(SadConsole.Console console)
        {
            var target = this.onCloseCallback != null ? this : console;
            target.DrawBox(new Microsoft.Xna.Framework.Rectangle(0, 0, this.Width, this.Height), BorderCell, BackgroundCell);

            if (keyBindingsConsole != null)
            {
                keyBindingsConsole.Draw(target);
            }
            else
            {
                target.Print(2, 2, "Options", Palette.OffWhite);

                this.PrintOption(target, 2, 4, "[1] Display characters", Options.DisplayOldStyleAsciiCharacters, "ASCII", "Extended");
                this.PrintOption(target, 2, 5, "[2] Colour palette", Options.CurrentPalette == SelectablePalette.StandardPalette, "Standard", "Saturated");
                this.PrintOption(target, 2, 6, "[3] Display mode", Options.IsFullScreen, "Fullscreen", "Windowed");
                target.Print(2, 7, $"[4] Effects display time: {Options.EffectsDelayMultiplier}x", Palette.Blue);
                target.Print(2, 8, $"[5] View or change key bindings", Palette.Blue);

                target.Print(2, this.Height - 3, $"Number keys to toggle options, {Options.KeyBindings[ConfigurableControl.OpenMenu]} to close", Palette.OffWhite);
            }
        }

        public void ProcessInput(IKeyboard keyboard)
        {
            if (this.keyBindingsConsole != null)
            {
                if (keyboard.IsKeyPressed(Options.KeyBindings[ConfigurableControl.OpenMenu]))
                {
                    this.keyBindingsConsole.StopBinding();
                }
                else
                {
                    this.keyBindingsConsole.ProcessInput(keyboard);
                }
            }
            else
            {
                if (this.ShouldProcessInput() && this.keyBindingsConsole == null)
                {
                    // TODO: process space/enter
                    if (keyboard.IsKeyPressed(Key.NumPad1))
                    {
                        Options.DisplayOldStyleAsciiCharacters = !Options.DisplayOldStyleAsciiCharacters;
                        this.SaveOptionsToDisk();
                    }
                    if (keyboard.IsKeyPressed(Key.NumPad2))
                    {
                        Options.CurrentPalette = (Options.CurrentPalette == SelectablePalette.StandardPalette ? SelectablePalette.SaturatedPalette : SelectablePalette.StandardPalette);
                        this.SaveOptionsToDisk();
                        Entity.ResetPalette(); // rebuild map of monster name => colour
                    }
                    if (keyboard.IsKeyPressed(Key.NumPad3))
                    {
                        Options.IsFullScreen = !Options.IsFullScreen;
                        SadConsole.Settings.ToggleFullScreen();
                        this.SaveOptionsToDisk();
                    }
                    if (keyboard.IsKeyPressed(Key.NumPad4))
                    {
                        Options.EffectsDelayMultiplier = Math.Max(1, (Options.EffectsDelayMultiplier + 1) % (Options.MaxEffectsDelayMultiplier + 1));
                        this.SaveOptionsToDisk();
                    }
                    if (keyboard.IsKeyPressed(Key.NumPad5))
                    {
                        this.keyBindingsConsole = new KeyBindingsStrategy();
                    }
                    if (keyboard.IsKeyPressed(Options.KeyBindings[ConfigurableControl.OpenMenu]))
                    {
                        this.SaveOptionsToDisk();
                        if (this.onCloseCallback != null)
                        {
                            this.onCloseCallback();
                        }
                        else
                        {
                            EventBus.Instance.Broadcast(GameEvent.ChangeSubMenu, typeof(TopLevelMenuStrategy));
                        }
                    }
                }
            }
        }

        private void PrintOption(SadConsole.Console target, int x, int y, string caption, bool isEnabled, string onLabel, string offLabel)
        {
            target.Print(x, y, caption, Palette.Blue);

            if (isEnabled)
            {
                target.Print(x + caption.Length + 1, y, $"[{onLabel}]", EnabledColour);
                target.Print(x + caption.Length + onLabel.Length + 4, y, offLabel, DisabledColour);
            }
            else
            {
                target.Print(x + caption.Length + 1, y, onLabel, DisabledColour);
                target.Print(x + caption.Length + onLabel.Length + 2, y, $"[{offLabel}]", EnabledColour);
            }
        }

        private void SaveOptionsToDisk()
        {
            var data = new Dictionary<string, string>() {
                // Strings are replicated in TitleConsole.cs
                { "Display", Options.DisplayOldStyleAsciiCharacters ? "ASCII" : "Extended" },
                { "Palette", Options.CurrentPalette == SelectablePalette.SaturatedPalette ? "Saturated" : "Standard" },
                { "FullScreen", Options.IsFullScreen.ToString() },
                { "EffectsDisplayMultiplier", Options.EffectsDelayMultiplier.ToString() },
                { "KeyBindings", JsonConvert.SerializeObject(Options.KeyBindings) },
                { "FirstRun", "false"},
            };

            var json = JsonConvert.SerializeObject(data);
            
            File.WriteAllText(Options.FileName, json);
        }
    }
}