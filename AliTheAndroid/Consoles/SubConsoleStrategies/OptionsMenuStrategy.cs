using System;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Model.Events;
using Ninject;

namespace  DeenGames.AliTheAndroid.Consoles.SubConsoleStrategies
{
    public class OptionsMenuStrategy : AbstractConsole, ISubConsoleStrategy
    {
        private readonly SadConsole.Cell BorderCell = new SadConsole.Cell(Palette.White, Palette.White, ' ');        
        private readonly SadConsole.Cell BackgroundCell = new SadConsole.Cell(Palette.BlackAlmost, Palette.BlackAlmost, ' ');

        // This class operates in two modes: when called from the main menu, and when called from the in-game menu.
        // For the main menu, this value is non-null. The choice of state also changes how we draw stuff.
        private Action onCloseCallback;


        // Used by in-game menu
        public OptionsMenuStrategy(int width, int height, Player player) : base(width, height)
        {

        }

        // Used by title screen
        public OptionsMenuStrategy(int width, int height, Action onCloseCallback) : base(width, height)
        {
            this.onCloseCallback = onCloseCallback;
        }

        public void Draw(SadConsole.Console console)
        {
            var target = this.onCloseCallback != null ? this : console;

            target.DrawBox(new Microsoft.Xna.Framework.Rectangle(0, 0, this.Width, this.Height), BorderCell, BackgroundCell);
            target.Print(2, 2, "Options", Palette.OffWhite);
        }

        public void ProcessInput(IKeyboard keyboard)
        {
            if (this.ShouldProcessInput())
            {
                if (keyboard.IsKeyPressed(Key.Escape))
                {
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
}