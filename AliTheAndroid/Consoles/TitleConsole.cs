using System;
using System.Collections.ObjectModel;
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
        private ReadOnlyCollection<string> hints = new ReadOnlyCollection<string>(new string[] {
            "View your most recent game run details on disk in LastGame.txt.",
            "You can switch to oldstyle ASCII graphics mode in Options.",
            "You can change object colours to a high-saturation palette in Options."
        });

        private readonly string[] titleText = new string[] {
            "#=# #  *   =#= # # #==",
            "#=# #  #    #  ### #=",
            "# # #= #    #  # # #==",
            "",
            "#=# ## # ##  #=#  #=# * ## ",
            "#=# # ## # # #=#  # # # # #",
            "# # #  # ##  #  # #=# # ## ",
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

            var hint = $"Hint: {this.hints.ElementAt(new Random().Next(this.hints.Count))}";
            var x = (this.Width - hint.Length) / 2;
            this.Print(x, this.Height - 2, hint, Palette.Blue);
        }

        override public void Update(System.TimeSpan delta)
        {
            if (this.keyboard.IsKeyPressed(Key.Escape))
            {
                this.Quit();
            }
            else if (this.keyboard.IsKeyPressed(Key.N))
            {
                this.StartNewGame();   
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
                        this.StartNewGame();
                        break;
                    case MenuItem.Options:
                        this.ShowOptions();
                        break;
                    case MenuItem.Quit:
                        this.Quit();
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

            var plainX = (this.Width - "Ali the Android".Length) / 2;
            var plainY = TitleY + this.titleText.Length + 2;
            this.Print(plainX, plainY, "Ali the Android", dark);
            this.Print(plainX + 4, plainY, "the", MainColour);
        }

        private void DrawMenu()
        {
            this.PrintText("[N]ew Game", 0, CurrentItem == MenuItem.NewGame ? MainColour : Palette.Grey);
            this.PrintText("[O]ptions", 1, CurrentItem == MenuItem.Options ? MainColour : Palette.Grey);
            this.PrintText("[ESC] Quit", 2, CurrentItem == MenuItem.Quit ? MainColour : Palette.Grey);

            this.PrintText("Arrow keys or WASD to move, enter/space to select an item", 5, Palette.OffWhite);
        }

        private void PrintText(string text, int yOffset, Color colour)
        {
            this.Print((this.Width - text.Length) / 2, this.MenuY  + yOffset, text, colour);
        }

        private void StartNewGame()
        {
            SadConsole.Global.CurrentScreen = new CoreGameConsole(this.Width, this.Height);
        }

        private void ShowOptions()
        {
            // TODO
        }

        private void Quit()
        {
            System.Environment.Exit(0);
        }

        enum MenuItem {
            NewGame,
            Options,
            Quit,
        }
    }
}