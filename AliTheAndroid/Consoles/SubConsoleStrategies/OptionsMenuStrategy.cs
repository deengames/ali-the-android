using System;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using Ninject;

namespace  DeenGames.AliTheAndroid.Consoles.SubConsoleStrategies
{
    public class OptionsMenuStrategy : AbstractConsole, ISubConsoleStrategy
    {
        private readonly SadConsole.Cell BorderCell = new SadConsole.Cell(Palette.White, Palette.White, ' ');        
        private readonly SadConsole.Cell BackgroundCell = new SadConsole.Cell(Palette.BlackAlmost, Palette.BlackAlmost, ' ');
        private Action onCloseCallback;

        public OptionsMenuStrategy(int width, int height, Action onCloseCallback) : base(width, height)
        {
            this.onCloseCallback = onCloseCallback;
        }

        public void Draw(SadConsole.Console console)
        {
            this.DrawBox(new Microsoft.Xna.Framework.Rectangle(0, 0, this.Width, this.Height), BorderCell, BackgroundCell);
            this.Print(2, 2, "Options", Palette.OffWhite);
        }

        public void ProcessInput(IKeyboard keyboard)
        {
            if (this.ShouldProcessInput())
            {
                if (keyboard.IsKeyPressed(Key.Escape))
                {
                    this.onCloseCallback();
                }
            }
        }
    }
}