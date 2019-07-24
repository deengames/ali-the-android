using System;
using System.Collections.Generic;
using System.Linq;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Model.Events;
using Microsoft.Xna.Framework;

namespace  DeenGames.AliTheAndroid.Consoles.SubConsoleStrategies
{
    public class ShowDataCubesStrategy : AbstractConsole, ISubConsoleStrategy
    {
        private Player player;
        private DataCube cubeShown = null; // null = none
        
        public ShowDataCubesStrategy(Player player)
        {
            this.player = player;
            EventBus.Instance.AddListener(GameEvent.ShowDataCube, (data) => this.cubeShown = data as DataCube);
        }

        public void Draw(SadConsole.Console console)
        {
            if (this.cubeShown == null)
            {
                this.ShowCubesList(console);
            }
            else
            {
                this.ShowSelectedCube(console);
            }

            console.Print(2, console.Height - 4, "[ESC] Go back", Palette.White);
        }

        public void ProcessInput(IKeyboard keyboard)
        {
            if (this.ShouldProcessInput())
            {
                if (this.cubeShown == null)
                {
                    if (keyboard.IsKeyPressed(Key.Escape))
                    {
                        EventBus.Instance.Broadcast(GameEvent.ChangeSubMenu, typeof(TopLevelMenuStrategy));
                    }
                    else
                    {
                        // My, this is a hack. Convert keys to strings ("boxed" ints) and if we have that floor's cube, show it.
                        // Expect just one key. If you press more, /shrug
                        var keyPressed = GetNumberKeyPressed(keyboard);
                        if (keyPressed != null)
                        {
                            var matchingCube = this.player.DataCubes.SingleOrDefault(c => c.FloorNumber.ToString() == keyPressed);
                            if (matchingCube != null)
                            {
                                this.cubeShown = matchingCube;
                            }
                        }
                    }
                }
                else
                {
                    if (keyboard.IsKeyPressed(Key.Escape))
                    {
                        this.cubeShown = null;
                    }
                }
            }
        }

        private string GetNumberKeyPressed(IKeyboard keyboard)
        {
            if (keyboard.IsKeyPressed(Key.NumPad1)) {
                return "1";
            }
            else if (keyboard.IsKeyPressed(Key.NumPad2)) {
                return "2";
            }
            else if (keyboard.IsKeyPressed(Key.NumPad3)) {
                return "3";
            }
            else if (keyboard.IsKeyPressed(Key.NumPad4)) {
                return "4";
            }
            else if (keyboard.IsKeyPressed(Key.NumPad5)) {
                return "5";
            }
            else if (keyboard.IsKeyPressed(Key.NumPad6)) {
                return "6";
            }
            else if (keyboard.IsKeyPressed(Key.NumPad7)) {
                return "7";
            }
            else if (keyboard.IsKeyPressed(Key.NumPad8)) {
                return "8";
            }
            else if (keyboard.IsKeyPressed(Key.NumPad9)) {
                return "9";
            }
            else {
                return null;
            }
        }

        private void ShowCubesList(SadConsole.Console console)
        {
            // Print data cubes in order of floor acquired, so the user sees any gaps.
            // Because of padding/border, print floor Bn on (x, y=n).
            var cubesByFloor = new Dictionary<int, DataCube>();
            for (var floor = DataCube.FirstDataCubeFloor; floor < DataCube.FirstDataCubeFloor + DataCube.NumCubes; floor++)
            {
                var cube = player.DataCubes.SingleOrDefault(d => d.FloorNumber == floor);
                if (cube != null)
                {
                    console.Print(2, floor + 1, $"[{cube.FloorNumber}] {cube.Title}", Palette.White);        
                }
            }
        }

        private void ShowSelectedCube(SadConsole.Console console)
        {
            console.Print(2, 2, cubeShown.Title, Palette.LightRed);
            this.PrettyPrint(console, 2, 4, cubeShown.Text, Palette.OffWhite);
        }

        // Prints a continuous text, breaking at word-boundaries instead of mid-word,
        // and taking into account the border and padding around the edge of the window.
        private void PrettyPrint(SadConsole.Console console, int x, int y, string text, Color colour)
        {
            var words = text.Split(' ');
            const int StartX = 2; // border + padding
            var maxX = this.Width - 2; // border + padding

            var currentX = x;
            var currentY = y;

            foreach (var word in words)
            {
                var stopX = currentX + 1 + word.Length;
                if (stopX > maxX)
                {
                    currentY += 1;
                    currentX = StartX;
                }

                var toPrint = $"{word} ";
                console.Print(currentX, currentY, toPrint, colour);
                currentX += toPrint.Length;
            }
        }
    }
}