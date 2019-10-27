using System;
using System.Collections.Generic;
using DeenGames.AliTheAndroid.Model.Entities;
using Troschuetz.Random;
using Troschuetz.Random.Generators;
using DeenGames.AliTheAndroid.Loggers;
using System.Diagnostics;
using DeenGames.AliTheAndroid.Accessibility;
using Newtonsoft.Json;
using DeenGames.AliTheAndroid.Infrastructure;
using DeenGames.AliTheAndroid.IO;

namespace DeenGames.AliTheAndroid.Model
{
    public class Dungeon
    {
        public const int NumFloors = 10;
        public static Dungeon Instance;

        public Floor CurrentFloor { get; private set; }

        public readonly Player Player;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int CurrentFloorNum { get; private set; } = -1;

        public int? GameSeed = null; // null = random each time
        public readonly List<Floor> Floors = new List<Floor>(NumFloors);

        private readonly IGenerator globalRandom;

        [JsonConstructor]
        // JSON/deserialization constructor
        public Dungeon(int width, int height, int currentFloorNum, Floor currentFloor, Player player) : this(width, height)
        {
            this.CurrentFloorNum = currentFloorNum;
            this.CurrentFloor = currentFloor;
            this.Player = player;
        }

        // Common constructor
        public Dungeon(int width, int height)
        {
            Dungeon.Instance = this;

            if (width <= 0)
            {
                throw new ArgumentException("Dungeon width must be positive.");
            }

            if (height <= 0)
            {
                throw new ArgumentException("Dungeon height must be positive.");
            }

            this.Width = width;
            this.Height = height;
        }

        // Production-code constructor
        public Dungeon(int widthInTiles, int heightInTiles, int? gameSeed = null)
        : this(widthInTiles, heightInTiles)
        {
            if (!gameSeed.HasValue)
            {
                gameSeed = new Random().Next();
            }
            if (!GameSeed.HasValue)
            {
                this.GameSeed = gameSeed;
            }
            
            Console.WriteLine($"Generating dungeon {this.GameSeed} ...");
            LastGameLogger.Instance.Log($"Generating dungeon {this.GameSeed} ...");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            this.globalRandom = new StandardGenerator(GameSeed.Value);
            this.Player = new Player();

            for (var i = 0; i < NumFloors; i++)
            {
                this.Floors.Add(new Floor(this.Width, this.Height, i, this.globalRandom));
            }

            // Intro events/message
            var arrowKeys = $"{Options.KeyBindings[GameAction.MoveUp]}{Options.KeyBindings[GameAction.MoveLeft]}{Options.KeyBindings[GameAction.MoveDown]}{Options.KeyBindings[GameAction.MoveRight]}";
            var rotateKeys = $"{Options.KeyBindings[GameAction.TurnCounterClockWise]} and {Options.KeyBindings[GameAction.TurnClockWise]}";
            this.Floors[0].LatestMessage = $"You beam to the deep-space research station. No lights or life-support. Use {arrowKeys} to move. Press {Options.KeyBindings[GameAction.Fire]} to fire, {rotateKeys} to turn.";

            stopwatch.Stop();
            LastGameLogger.Instance.Log($"Generated in {stopwatch.Elapsed.TotalSeconds}s");
        }

        public void GoToNextFloor()
        {
            if (this.CurrentFloorNum > -1)
            {
                AudioManager.Instance.Play("Descend", true);
            }

            this.CurrentFloorNum++;
            this.CurrentFloor = this.Floors[this.CurrentFloorNum];
            LastGameLogger.Instance.Log($"Descended to B{this.CurrentFloorNum + 1}");

            // End game floor
            if (this.CurrentFloorNum == 9)
            {
                this.CurrentFloor.LatestMessage += "You detect an abnormal life-form. Mad laughter echoes from afar.";
                Options.EnableOmniSight = true;
            }
            
            this.CurrentFloor.Player = this.Player;
            this.Player.X = this.CurrentFloor.StairsUpLocation.X;
            this.Player.Y = this.CurrentFloor.StairsUpLocation.Y;
            this.CurrentFloor.RecalculatePlayerFov();
            this.CurrentFloor.MarkCurrentFovAsSeen();

            SaveManager.SaveGame();
            // Don't show on B1 and obliterate our tutorial!
            if (this.CurrentFloorNum > 0)
            {
                this.CurrentFloor.LatestMessage = "Game saved.";
            }
        }

        public void GoToPreviousFloor()
        {
            Options.EnableOmniSight = false;
            AudioManager.Instance.Play("Ascend", true);
            this.CurrentFloorNum--;
            this.CurrentFloor = this.Floors[this.CurrentFloorNum];
            LastGameLogger.Instance.Log($"Ascended to B{this.CurrentFloorNum + 1}");
            this.CurrentFloor.LatestMessage = "Game saved.";

            this.CurrentFloor.Player = this.Player;
            this.Player.X = this.CurrentFloor.StairsDownLocation.X;
            this.Player.Y = this.CurrentFloor.StairsDownLocation.Y;
            this.CurrentFloor.RecalculatePlayerFov();
            SaveManager.SaveGame();
        }

        internal void Update(TimeSpan delta)
        {
            this.CurrentFloor.Update(delta);
        }
    }
}