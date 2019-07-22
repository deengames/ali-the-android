using System;
using System.Linq;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using Microsoft.Xna.Framework;
using Ninject;

namespace DeenGames.AliTheAndroid.Consoles
{
    class TitleConsole : AbstractConsole
    {
        private readonly Color MainColour = Palette.Blue;
        private const int TitleY = 2;

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
        private int currentItemIndex = 0;

        private MenuItem CurrentItem { get { 
            switch (currentItemIndex) {
                case 0:
                    return MenuItem.NewGame;
                case 1:
                    return MenuItem.Options;
                case 2: 
                    return MenuItem.Quit;
                default:
                    throw new InvalidOperationException();
            }
        }}


        public TitleConsole(int width, int height) : base(width, height)
        {
            this.keyboard = DependencyInjection.kernel.Get<IKeyboard>();
            MenuY = this.Height / 2;

            this.DrawTitleText();
            this.DrawMenu();
        }

        override public void Update(System.TimeSpan delta)
        {
            if (this.keyboard.IsKeyPressed(Key.Escape))
            {
                System.Environment.Exit(0);
            }

            if (this.keyboard.IsKeyPressed(Key.Up) || this.keyboard.IsKeyPressed(Key.W))
            {
                this.currentItemIndex -= 1;
                if (this.currentItemIndex == -1) {
                    this.currentItemIndex = Enum.GetValues(typeof(MenuItem)).Length - 1;
                }

                this.DrawMenu();
            }
            else if (this.keyboard.IsKeyPressed(Key.Down) || this.keyboard.IsKeyPressed(Key.S))
            {
                this.currentItemIndex = (this.currentItemIndex + 1) % Enum.GetValues(typeof(MenuItem)).Length;
                this.DrawMenu();
            }

            if (this.keyboard.IsKeyPressed(Key.Space) || this.keyboard.IsKeyPressed(Key.Enter))
            {
                switch (this.CurrentItem) {
                    case MenuItem.NewGame:
                        break;
                    case MenuItem.Options:
                        break;
                    case MenuItem.Quit:
                        System.Environment.Exit(0);
                        break;
                }
            }
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
            this.PrintText("[N]ew Game", 0, CurrentItem == MenuItem.NewGame ? MainColour : Palette.Grey);
            this.PrintText("[O]ptions", 1, CurrentItem == MenuItem.Options ? MainColour : Palette.Grey);
            this.PrintText("[ESC] Quit", 2, CurrentItem == MenuItem.Quit ? MainColour : Palette.Grey);
        }

        private void PrintText(string text, int yOffset, Color colour)
        {
            this.Print((this.Width - text.Length) / 2, this.MenuY  + yOffset, text, colour);
        }

        enum MenuItem {
            NewGame,
            Options,
            Quit,
        }
    }
}