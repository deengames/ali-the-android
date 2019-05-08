using System;
using AliTheAndroid.Prototype;
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

        private readonly int? GameSeed = null; // null = random each time
        private readonly IGenerator globalRandom;
        private readonly IKeyboard keyboard;

        public Dungeon(int widthInTiles, int heightInTiles)
        {
            this.Width = widthInTiles;
            this.Height = heightInTiles;
            
            if (!GameSeed.HasValue)
            {
                GameSeed = new Random().Next();
            }
            
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