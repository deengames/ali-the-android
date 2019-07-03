using System.Collections.Generic;
using System.Linq;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Model.Events;

namespace  DeenGames.AliTheAndroid.Consoles.SubConsoleStrategies
{
    public class ShowDataCubesStrategy : AbstractConsole, ISubConsoleStrategy
    {
        private Player player;
        private DataCube cubeShown = null; // null = none
        
        public ShowDataCubesStrategy(int width, int height, Player player) : base(width, height)
        {
            this.player = player;
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

            console.Print(2, console.Height - 4, "[ESC] Back to main menu", Palette.White);
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
                        var keyPressed = keyboard.GetKeysReleased().Select(k => k.ToString().Substring(k.ToString().Length - 1)).SingleOrDefault();
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
                    if (keyboard.GetKeysReleased().Any())
                    {
                        this.cubeShown = null;
                    }
                }
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
            console.Print(2, 2, cubeShown.Title, Palette.White);
            console.Print(2, 4, cubeShown.Text, Palette.OffWhite);
        }
    }
}