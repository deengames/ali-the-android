using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure.Common;

namespace  DeenGames.AliTheAndroid.Consoles.SubConsoleStrategies
{
    public class OptionsMenuStrategy : AbstractConsole, ISubConsoleStrategy
    {
        private readonly SadConsole.Cell BorderCell = new SadConsole.Cell(Palette.White, Palette.White, ' ');        
        private readonly SadConsole.Cell BackgroundCell = new SadConsole.Cell(Palette.BlackAlmost, Palette.BlackAlmost, ' ');        

        public OptionsMenuStrategy(int width, int height) : base(width, height)
        {
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
            }
        }
    }
}