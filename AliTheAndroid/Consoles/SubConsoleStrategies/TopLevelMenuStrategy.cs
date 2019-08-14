using System.IO;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Model.Events;

namespace  DeenGames.AliTheAndroid.Consoles.SubConsoleStrategies
{
    public class TopLevelMenuStrategy : AbstractConsole, ISubConsoleStrategy
    {
        private bool justSaved = false; // used to print "saved!" message
        private Player player; 

        public TopLevelMenuStrategy(Player player)
        {
            this.player = player;
        }

        public void Draw(SadConsole.Console console)
        {
            console.Print(2, 2, "[D] Review data cubes", Palette.White);
            console.Print(2, 3, "[O] Options", Palette.White);
            console.Print(2, 4, "[S] Save game", Palette.White);

            if (this.justSaved)
            {
                console.Print(2, 6, "Game saved!", Palette.Blue);
            }
            
            console.Print(2, console.Height - 4, "[ESC] Back to game", Palette.White);
            console.Print(2, console.Height - 3, "[Q] Quit", Palette.White);
        }

        public void ProcessInput(IKeyboard keyboard)
        {
            if (this.ShouldProcessInput())
            {
                if (keyboard.IsKeyPressed(Key.Q))
                {
                    InGameSubMenuConsole.IsOpen = false;
                    SadConsole.Global.CurrentScreen = new TitleConsole(Program.GameWidthInTiles, Program.GameHeightInTiles);
                    this.justSaved = false;
                }
                else if (keyboard.IsKeyPressed(Key.O))
                {
                    EventBus.Instance.Broadcast(GameEvent.ChangeSubMenu, typeof(OptionsMenuStrategy));
                    this.justSaved = false;
                }
                else if (keyboard.IsKeyPressed(Key.D))
                {
                    EventBus.Instance.Broadcast(GameEvent.ChangeSubMenu, typeof(ShowDataCubesStrategy));
                    this.justSaved = false;
                }
                else if (keyboard.IsKeyPressed(Key.S))
                {
                    SaveManager.SaveGame();
                    this.justSaved = true;
                }
            }
        }
    }
}