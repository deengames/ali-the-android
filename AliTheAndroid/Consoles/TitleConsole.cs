using System.Linq;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using Microsoft.Xna.Framework;
using Ninject;

namespace DeenGames.AliTheAndroid.Consoles
{
    class TitleConsole : SadConsole.Console
    {
        private readonly Color MainColour = Palette.Blue;
        private const int TitleY = 1;

        private readonly string[] titleText = new string[] {
            "### #  o   ### # # ###",
            "# # #  #    #  ### #- ",
            "# # ## #    #  # # ###",
            "",
            "### ## # ##  ##   ### o ## ",
            "# # # ## # # #.#  # # # # #",
            "# # #  # ##  #  # ### # ## ",
        };

        private readonly int MenuY;

        private IKeyboard keyboard;
        private MenuItem currentItem = MenuItem.NewGame;

        public TitleConsole(int width, int height) : base(width, height)
        {
            this.keyboard = DependencyInjection.kernel.Get<IKeyboard>();
            MenuY = this.Height - 8;

            this.DrawTitleText();
            this.DrawMenu();
        }

        override public void Update(System.TimeSpan delta)
        {
            
        }

        private void DrawTitleText()
        {
            var dark = Palette.DarkestBlue;

            for (int i = 0; i < this.titleText.Length; i++)
            {
                var line = this.titleText[i];
                var x = (this.Width - line.Length) / 2;
                var y = TitleY + i;
                var colour = (i == 1 || i == 5 ? MainColour : dark);
                this.Print(x, y, line, colour);
            }
        }

        private void DrawMenu()
        {
            this.Print((this.Width - 8) / 2, this.MenuY, "New Game", currentItem == MenuItem.NewGame ? MainColour : Palette.Grey);
            this.Print((this.Width - 8) / 2, this.MenuY + 1, "Options", currentItem == MenuItem.Options ? MainColour : Palette.Grey);
        }

        enum MenuItem {
            NewGame,
            Options
        }
    }
}