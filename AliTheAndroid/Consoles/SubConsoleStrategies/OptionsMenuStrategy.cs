using System;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Model.Events;
using Microsoft.Xna.Framework;
using Ninject;

namespace  DeenGames.AliTheAndroid.Consoles.SubConsoleStrategies
{
    public class OptionsMenuStrategy : AbstractConsole, ISubConsoleStrategy
    {
        private const int DisplayWidth = 60;
        private const int DisplayHeight = 25;

        private readonly Color EnabledColour = Palette.Cyan;
        private readonly Color DisabledColour = Palette.Grey;

        private readonly SadConsole.Cell BorderCell = new SadConsole.Cell(Palette.White, Palette.White, ' ');        
        private readonly SadConsole.Cell BackgroundCell = new SadConsole.Cell(Palette.BlackAlmost, Palette.BlackAlmost, ' ');

        // This class operates in two modes: when called from the main menu, and when called from the in-game menu.
        // For the main menu, this value is non-null. The choice of state also changes how we draw stuff.
        private Action onCloseCallback;


        // Used by in-game menu
        public OptionsMenuStrategy(int width, int hieght, Player player) : base(DisplayWidth, DisplayHeight)
        {
            
        }

        // Used by title screen
        public OptionsMenuStrategy(Action onCloseCallback) : base(DisplayWidth, DisplayHeight)
        {
            this.onCloseCallback = onCloseCallback;
        }

        public void Draw(SadConsole.Console console)
        {
            var target = this.onCloseCallback != null ? this : console;

            target.DrawBox(new Microsoft.Xna.Framework.Rectangle(0, 0, this.Width, this.Height), BorderCell, BackgroundCell);
            target.Print(2, 2, "Options", Palette.OffWhite);

            this.PrintOption(2, 4, "[1] Display oldstyle ASCII characters instead", Options.DisplayOldStyleAsciiCharacters);

            target.Print(2, this.Height - 3, "Press number keys to toggle options", Palette.OffWhite);
        }

        private void PrintOption(int x, int y, string caption, bool isEnabled)
        {
            this.Print(x, y, caption, Palette.Blue);

            if (isEnabled)
            {
                this.Print(x + caption.Length + 1, y, "[On]", EnabledColour);
                this.Print(x + caption.Length + 6, y, "Off", DisabledColour);
            }
            else
            {
                this.Print(x + caption.Length + 1, y, "On", DisabledColour);
                this.Print(x + caption.Length + 4, y, "[Off]", EnabledColour);
            }
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