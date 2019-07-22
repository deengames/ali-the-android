using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Model.Events;

namespace  DeenGames.AliTheAndroid.Consoles.SubConsoleStrategies
{
    public class TopLevelMenuStrategy : AbstractConsole, ISubConsoleStrategy
    {
        private Player player; 
        public TopLevelMenuStrategy(int width, int height, Player player) : base(width, height)
        {
            this.player = player;
        }

        public void Draw(SadConsole.Console console)
        {
            console.Print(2, 2, "[1] Review data cubes", Palette.White);
            console.Print(2, console.Height - 4, "[ESC] Back to game", Palette.White);
            console.Print(2, console.Height - 3, "[Q] Quit", Palette.White);
        }

        public void ProcessInput(IKeyboard keyboard)
        {
            if (this.ShouldProcessInput())
            {
                if (keyboard.IsKeyPressed(Key.Q))
                {
                    SadConsole.Global.CurrentScreen = new TitleConsole(Program.GameWidthInTiles, Program.GameHeightInTiles);
                }
                else if (keyboard.IsKeyPressed(Key.NumPad1))
                {
                    EventBus.Instance.Broadcast(GameEvent.ChangeSubMenu, typeof(ShowDataCubesStrategy));
                }
            }
        }
    }
}