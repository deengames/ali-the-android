using System;
using System.Collections.ObjectModel;
using System.Linq;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Consoles.SubConsoleStrategies;
using Microsoft.Xna.Framework;
using Ninject;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using DeenGames.AliTheAndroid.Accessibility;
using DeenGames.AliTheAndroid.Infrastructure;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Model.Events;

namespace DeenGames.AliTheAndroid.Consoles
{
    class TitleConsole : AbstractConsole
    {
        // Player started a new game at this time. Null if not yet.  This is to work-around
        // a bug where anything we print right before switching scenes, doesn't draw.
        private DateTime? launchedOn = null;
        private bool loadGame = false;
        private readonly int gameSeed = new Random().Next();

        private readonly Color MainColour = Palette.Blue;
        private const int TitleY = 2;
        private ReadOnlyCollection<string> tips = new ReadOnlyCollection<string>(new string[] {
            "View your most recent game run details on disk in LastGame.txt.",
            "You can switch to oldstyle ASCII graphics mode in Options.",
            "You can change object colours to a high-saturation palette in Options.",
            "You can rebind keys for all in-game actions (minus menus) in Options.",
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

        private MenuItem GetCurrentItem() {
            if (File.Exists(Serializer.SaveGameFileName)) {
                switch (currentItemIndex) {
                    case 0:
                        return MenuItem.NewGame;
                    case 1:
                        return MenuItem.LoadGame;
                    case 2:
                        return MenuItem.Options;
                    case 3: 
                        return MenuItem.Quit;
                    default:
                        throw new InvalidOperationException();
                }
            } else {
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
            }
        }

        private OptionsMenuStrategy optionsMenu = null;

        public TitleConsole(int width, int height) : base(width, height)
        {
            this.keyboard = DependencyInjection.kernel.Get<IKeyboard>();
            EventBus.Reset();

            MenuY = (this.Height / 2) + 1;

            this.DrawTitleText();
            this.DrawMenu();

            var hint = $"Tip: {this.tips.ElementAt(new Random().Next(this.tips.Count))}";
            var x = (this.Width - hint.Length) / 2;
            this.Print(x, this.Height - 2, hint, Palette.Blue);

            this.LoadOptionsFromDisk();
        }

        override public void Update(System.TimeSpan delta)
        {
            if (this.launchedOn != null && (DateTime.Now - this.launchedOn.Value).TotalMilliseconds >= 100)
            {
                Dungeon dungeon;

                if (this.loadGame)
                {
                    var serialized = File.ReadAllText(Serializer.SaveGameFileName);
                    dungeon = Serializer.Deserialize<Dungeon>(serialized);
                    // Go in and re-pair power-ups which are not paired any more
                    foreach (var floor in dungeon.Floors)
                    {
                        floor.PairPowerUps();
                        floor.InitializeMapAndFov();
                        floor.RecreateSpawners();
                    }
                    dungeon.CurrentFloor.RecalculatePlayerFov();
                    Dungeon.Instance = dungeon;
                }
                else
                {
                    dungeon = new Dungeon(this.Width, this.Height, gameSeed);
                    dungeon.GoToNextFloor();
                }

                SadConsole.Global.CurrentScreen = new CoreGameConsole(this.Width, this.Height, dungeon);
            }

            if (optionsMenu == null)
            {
                if (this.keyboard.IsKeyPressed(Key.Escape))
                {
                    this.Quit();
                }
                else if (this.keyboard.IsKeyPressed(Key.N))
                {
                    this.StartNewGame();   
                }
                else if (this.keyboard.IsKeyPressed(Key.L))
                {
                    this.LoadGame();
                }
                else if (this.keyboard.IsKeyPressed(Key.O))
                {
                    this.ShowOptions();
                }

                if (this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.MoveUp]))
                {
                    this.currentItemIndex -= 1;
                    if (this.currentItemIndex == -1) {
                        this.currentItemIndex = Enum.GetValues(typeof(MenuItem)).Length - 1;
                    }

                    this.DrawMenu();
                }
                else if (this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.MoveDown]))
                {
                    var numItems = File.Exists(Serializer.SaveGameFileName) ? 4 : 3;
                    this.currentItemIndex = (this.currentItemIndex + 1) % numItems;
                    this.DrawMenu();
                }

                if (this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.SkipTurn]))
                {
                    var currentItem = this.GetCurrentItem();
                    switch (currentItem) {
                        case MenuItem.NewGame:
                            this.StartNewGame();
                            break;
                        case MenuItem.LoadGame:
                            this.LoadGame();
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
            else
            {
                optionsMenu.Draw(this);
                optionsMenu.ProcessInput(this.keyboard);
            }
        }

        private void LoadOptionsFromDisk()
        {
            if (File.Exists(Options.FileName))
            {
                var json = File.ReadAllText(Options.FileName);
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                // Strings are replicated in OptionsMenuStrategy.cs
                // Default if missing/invalid/etc.
                Options.CurrentPalette = SelectablePalette.StandardPalette;
                if (data.ContainsKey("Palette") && data["Palette"] == "Saturated")
                {
                    Options.CurrentPalette = SelectablePalette.SaturatedPalette;
                }

                // Default if missing/invalid/etc.
                Options.DisplayOldStyleAsciiCharacters = false;
                if (data.ContainsKey("Display") && data["Display"] == "ASCII")
                {
                    Options.DisplayOldStyleAsciiCharacters = true;
                }

                Options.EffectsDelayMultiplier = 1;
                if (data.ContainsKey("EffectsDisplayMultiplier"))
                {
                    int parsedValue;
                    if (int.TryParse(data["EffectsDisplayMultiplier"], out parsedValue) && parsedValue >= 1 && parsedValue <= 4)
                    {
                        Options.EffectsDelayMultiplier = parsedValue;
                    }
                }

                if (data.ContainsKey("KeyBindings"))
                {
                    Options.KeyBindings = JsonConvert.DeserializeObject<SortedDictionary<GameAction, Key>>(data["KeyBindings"]);
                }

                if (data.ContainsKey("DeleteSaveGameOnDeath"))
                {
                    Options.DeleteSaveGameOnDeath = Boolean.Parse(data["DeleteSaveGameOnDeath"]);
                }

                if (!data.ContainsKey("FirstRun") || data["FirstRun"] == "true")
                {
                    data["FirstRun"] = "false";
                    data["FullScreen"] = "true";
                    this.DoFirstRunStuff();

                    // Re-save
                    File.WriteAllText(Options.FileName, JsonConvert.SerializeObject(data));
                }

                if (!data.ContainsKey("SoundEffectsVolume"))
                {
                    data["SoundEffectsVolume"] = "100";
                }
                else
                {
                    var volume = int.Parse(data["SoundEffectsVolume"]);
                    if (volume < 0)
                    {
                        data["SoundEffectsVolume"] = "0";
                    }
                    else if (volume > 100)
                    {
                        data["SoundEffectsVolume"] = "100";
                    }
                }
                
                Options.SoundEffectsVolume = int.Parse(data["SoundEffectsVolume"]);
                Microsoft.Xna.Framework.Audio.SoundEffect.MasterVolume = (Options.SoundEffectsVolume / 100f) * Options.GlobalSfxVolumeNerf;
            }
            else
            {
                // Do first run stuff
                this.DoFirstRunStuff();
            }
        }

        private void DoFirstRunStuff()
        {
            SadConsole.Settings.ToggleFullScreen();                    
            Options.IsFullScreen = true;

            this.ShowOptions();
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
            this.Print(plainX, plainY, "Ali the Android", MainColour);
            this.Print(plainX + 4, plainY, "the", Palette.OffWhite);

            var deenGamesX = (this.Width - "By Deen Games".Length) / 2;
            var deenGamesY = plainY + 1;
            this.Print(deenGamesX, deenGamesY, "By Deen Games", Palette.OffWhite);
        }

        private void DrawMenu()
        {
            var currentItem = this.GetCurrentItem();
            this.PrintText("[N] New Game", 0, currentItem == MenuItem.NewGame ? MainColour : Palette.Grey);
            if (File.Exists(Serializer.SaveGameFileName))
            {
                this.PrintText("[L] Load Game", 1, currentItem == MenuItem.LoadGame ? MainColour : Palette.Grey);   
            }
            this.PrintText("[O] Options", 3, currentItem == MenuItem.Options ? MainColour : Palette.Grey);
            this.PrintText("[ESC] Quit", 4, currentItem == MenuItem.Quit ? MainColour : Palette.Grey);

            this.PrintText("Arrow keys or WASD to move, enter/space to select an item", 6, Palette.OffWhite);
        }

        private void PrintText(string text, int yOffset, Color colour)
        {
            this.Print((this.Width - text.Length) / 2, this.MenuY  + yOffset, text, colour);
        }

        private void StartNewGame()
        {
            var message = $"Generating game #{this.gameSeed}.";

            var x = (this.Width - message.Length) / 2;
            this.Print(x, this.MenuY - 2, message, Palette.White);

            this.launchedOn = DateTime.Now;
        }

        private void LoadGame()
        {
            if (File.Exists(Serializer.SaveGameFileName))
            {
                var message = "Loading game ...";

                var x = (this.Width - message.Length) / 2;
                this.Print(x, this.MenuY - 2, message, Palette.White);

                this.loadGame = true;
                this.launchedOn = DateTime.Now;
            }
        }

        private void ShowOptions()
        {
            if (this.optionsMenu == null)
            {
                this.optionsMenu = new OptionsMenuStrategy(() => 
                {
                    this.Children.Remove(this.optionsMenu);
                    this.optionsMenu = null;
                });

                this.optionsMenu.Position = new Point((this.Width - this.optionsMenu.Width) / 2, (this.Height - this.optionsMenu.Height) / 2);
                this.Children.Add(this.optionsMenu);
            }
        }

        private void Quit()
        {
            System.Environment.Exit(0);
        }

        enum MenuItem {
            NewGame,
            LoadGame,
            Options,
            Quit,
        }
    }
}