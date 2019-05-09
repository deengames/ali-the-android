using System;
using AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using Ninject;
using Troschuetz.Random;
using Troschuetz.Random.Generators;

namespace DeenGames.AliTheAndroid.Model
{
    public class Dungeon
    {
        public Floor CurrentFloor { get; private set; }

        public readonly Player Player;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int CurrentFloorNum { get; private set; } = 0;

        public readonly int? GameSeed = null; // null = random each time
        private readonly IGenerator globalRandom;

        public Dungeon(int widthInTiles, int heightInTiles, int? gameSeed = null)
        {
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

            this.GameSeed = gameSeed;
            
            System.Console.WriteLine($"Universe #{GameSeed.Value}");
            this.globalRandom = new StandardGenerator(GameSeed.Value);
            this.Player = new Player();
        }

        public void Generate()
        {
            this.CurrentFloorNum++;
            this.CurrentFloor = new Floor(this.Width, this.Height, globalRandom, this.Player);
        }

        internal void Update(TimeSpan delta)
        {
            this.CurrentFloor.Update(delta);
        }
    }
}