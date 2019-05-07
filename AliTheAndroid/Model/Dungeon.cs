using System;
using Troschuetz.Random;
using Troschuetz.Random.Generators;

namespace DeenGames.AliTheAndroid.Model
{
    public class Dungeon
    {
        public Floor CurrentFloor { get; private set; }

        private readonly int? GameSeed = null; // null = random each time
        private readonly IGenerator globalRandom;

        public int Width { get; private set; }
        public int Height { get; private set; }
        private int currentFloorNum = 0;


        public Dungeon(int widthInTiles, int heightInTiles)
        {
            if (!GameSeed.HasValue)
            {
                GameSeed = new Random().Next();
            }
            
            System.Console.WriteLine($"Universe #{GameSeed.Value}");
            this.globalRandom = new StandardGenerator(GameSeed.Value);
        }

        public void Generate()
        {
            this.currentFloorNum++;
            this.CurrentFloor = new Floor(this.Width, this.Height, globalRandom);
        }

        internal void Update(TimeSpan delta)
        {
            this.CurrentFloor.Update(delta);
        }
    }
}