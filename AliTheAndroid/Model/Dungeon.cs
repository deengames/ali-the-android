using System;
using System.Collections.Generic;
using System.Linq;
using DeenGames.AliTheAndroid.Model.Entities;
using Troschuetz.Random;
using Troschuetz.Random.Generators;
using DeenGames.AliTheAndroid.Loggers;
using System.Diagnostics;
using DeenGames.AliTheAndroid.Accessibility;
using Newtonsoft.Json;
using DeenGames.AliTheAndroid.Infrastructure;

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

        public readonly int? GameSeed = null ; // null = random each time
        public readonly List<Floor> Floors = new List<Floor>(NumFloors);

        private readonly IGenerator globalRandom;

        [JsonConstructor]
        public Dungeon(int width, int height, int currentFloorNum, Floor currentFloor, Player player) : this(width, height)
        {
            this.CurrentFloorNum = currentFloorNum;
            this.CurrentFloor = currentFloor;
            this.Player = player;
        }

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
            this.Floors[0].LatestMessage = $"You beam onto the deep-space research station. No lights or life-support.    Use {arrowKeys} to move. Press {Options.KeyBindings[GameAction.Fire]} to fire, {rotateKeys} to turn.";

            stopwatch.Stop();
            LastGameLogger.Instance.Log($"Generated in {stopwatch.Elapsed.TotalSeconds}s");
        }

        public void GoToNextFloor()
        {
            this.CurrentFloorNum++;
            this.CurrentFloor = this.Floors[this.CurrentFloorNum];
            LastGameLogger.Instance.Log($"Descended to B{this.CurrentFloorNum + 1}");
            if (this.CurrentFloorNum == 9)
            {
                this.CurrentFloor.LatestMessage = "You detect an abnormal life-form. Mad laughter echoes from afar.";
            }
            
            this.CurrentFloor.Player = this.Player;
            this.Player.X = this.CurrentFloor.StairsUpLocation.X;
            this.Player.Y = this.CurrentFloor.StairsUpLocation.Y;
            this.CurrentFloor.RecalculatePlayerFov();
        }

        public void GoToPreviousFloor()
        {
            this.CurrentFloorNum--;
            this.CurrentFloor = this.Floors[this.CurrentFloorNum];
            LastGameLogger.Instance.Log($"Ascended to B{this.CurrentFloorNum + 1}");

            this.CurrentFloor.Player = this.Player;
            this.Player.X = this.CurrentFloor.StairsDownLocation.X;
            this.Player.Y = this.CurrentFloor.StairsDownLocation.Y;
            this.CurrentFloor.RecalculatePlayerFov();
        }

        internal void Update(TimeSpan delta)
        {
            this.CurrentFloor.Update(delta);
        }
    }
}