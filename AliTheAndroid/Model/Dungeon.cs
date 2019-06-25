using System;
using System.Collections.Generic;
using System.Linq;
using DeenGames.AliTheAndroid.Model.Entities;
using Troschuetz.Random;
using Troschuetz.Random.Generators;
using DeenGames.AliTheAndroid.Loggers;
using System.Diagnostics;

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

        public readonly int? GameSeed = null; // null = random each time
        private readonly IGenerator globalRandom;
        private readonly List<Floor> floors = new List<Floor>(NumFloors);
        private readonly List<PowerUp> guaranteedPowerUps = new List<PowerUp>();

        public Dungeon(int widthInTiles, int heightInTiles, int? gameSeed = null)
        {
            Dungeon.Instance = this;

            if (widthInTiles <= 0)
            {
                throw new ArgumentException("Dungeon width must be positive.");
            }

            if (heightInTiles <= 0)
            {
                throw new ArgumentException("Dungeon height must be positive.");
            }

            this.Width = widthInTiles;
            this.Height = heightInTiles;
            
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

            this.GeneratePowerUpDistribution();

            for (var i = 0; i < NumFloors; i++)
            {
                this.floors.Add(new Floor(this.Width, this.Height, i, this.globalRandom, this.guaranteedPowerUps));
            }

            stopwatch.Stop();
            LastGameLogger.Instance.Log($"Generated in {stopwatch.Elapsed.TotalSeconds}s");
        }

        public void GoToNextFloor()
        {
            this.CurrentFloorNum++;
            this.CurrentFloor = this.floors[this.CurrentFloorNum];
            LastGameLogger.Instance.Log($"Descended to B{this.CurrentFloorNum + 1}");
            
            this.CurrentFloor.Player = this.Player;
            this.Player.X = this.CurrentFloor.StairsUpLocation.X;
            this.Player.Y = this.CurrentFloor.StairsUpLocation.Y;
            this.CurrentFloor.RecalculatePlayerFov();

            this.CurrentFloor.GeneratePowerUps();
        }

        public void GoToPreviousFloor()
        {
            this.CurrentFloorNum--;
            this.CurrentFloor = this.floors[this.CurrentFloorNum];
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
        
        private void GeneratePowerUpDistribution()
        {
            this.guaranteedPowerUps.Clear(); // Just in-case

            // Guarantee: two vision, two health, and one strength/defense; the rest are random.
            this.guaranteedPowerUps.Add(new PowerUp(0, 0, healthBoost: PowerUp.TypicalHealthBoost));
            this.guaranteedPowerUps.Add(new PowerUp(0, 0, healthBoost: PowerUp.TypicalHealthBoost));
            this.guaranteedPowerUps.Add(new PowerUp(0, 0, visionBoost: PowerUp.TypicalVisionBoost));
            this.guaranteedPowerUps.Add(new PowerUp(0, 0, visionBoost: PowerUp.TypicalVisionBoost));
            this.guaranteedPowerUps.Add(new PowerUp(0, 0, strengthBoost: PowerUp.TypicalStrengthBoost));
            this.guaranteedPowerUps.Add(new PowerUp(0, 0, defenseBoost: PowerUp.TypicalDefenseBoost));

            // Add +1 because you always have a choice of two power-ups; so we need one extra.
            while (this.guaranteedPowerUps.Count < NumFloors + 1)
            {
                // Vision is not useful after two boosts
                var next = globalRandom.Next(100);
                if (next < 33)
                {
                    this.guaranteedPowerUps.Add(new PowerUp(0, 0, healthBoost: PowerUp.TypicalHealthBoost));
                }
                else if (next >= 33 && next <= 66)
                {
                    this.guaranteedPowerUps.Add(new PowerUp(0, 0, strengthBoost: PowerUp.TypicalStrengthBoost));
                }
                else
                {
                    this.guaranteedPowerUps.Add(new PowerUp(0, 0, defenseBoost: PowerUp.TypicalDefenseBoost));
                }
            }

            // Shuffle
            this.guaranteedPowerUps.OrderBy(r => globalRandom.Next());
        }
    }
}