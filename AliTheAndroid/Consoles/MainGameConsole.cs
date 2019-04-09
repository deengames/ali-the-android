using DeenGames.AliTheAndroid.Ecs;
using Palette = DeenGames.AliTheAndroid.Enums.Palette;
using GoRogue.MapViews;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using Troschuetz.Random;
using Troschuetz.Random.Generators;
using DeenGames.AliTheAndroid.Events;
using DeenGames.AliTheAndroid.Enums;

namespace DeenGames.AliTheAndroid.Consoles
{
    public class MainGameConsole : SadConsole.Console
    {
        private static readonly int? GameSeed = null;

        public static readonly IGenerator GlobalRandom;

        private readonly Entity player = new Entity(0, 0, '@', Palette.White);
        private readonly List<Entity> walls = new List<Entity>();
        private readonly List<Entity> monsters = new List<Entity>();

        private readonly int mapHeight = 40;

        private string latestMessage = "";
        private ArrayMap<bool> map;

        static MainGameConsole() {
            if (!GameSeed.HasValue) {
                GameSeed = new Random().Next();
            }
            
            System.Console.WriteLine($"Universe #{GameSeed.Value}");
            GlobalRandom = new StandardGenerator(GameSeed.Value);
        }

        public MainGameConsole(int width, int height) : base(width, height)
        {
            this.map = this.GenerateWalls();
            var emptySpot = this.FindEmptySpot();
            player.X = (int)emptySpot.X;
            player.Y = (int)emptySpot.Y;

            this.RedrawEverything();

            EventBus.Instance.AddListener(GameEvent.EntityDeath, (e) => {
                if (e == player)
                {
                    this.latestMessage = "YOU DIE!!!";
                    this.player.Character = '%';
                    this.player.Color = Palette.Burgandy;

                    this.RedrawEverything();
                }
                else
                {
                    this.monsters.Remove(e as Entity);
                }
            });
        }

        private Tuple<int, int> FindEmptyLocation(ArrayMap<bool> map, List<Entity> monsters, List<Entity> walls)
        {
            while (true) {
                var x = MainGameConsole.GlobalRandom.Next(0, map.Width);
                var y = MainGameConsole.GlobalRandom.Next(0, map.Height);

                if (map[x, y] == false && monsters.All(m => m.X != x || m.Y != y) && walls.All(w => w.X != x || w.Y != y))  {
                    return new Tuple<int, int>(x, y);
                }
            }
        }

        private ArrayMap<bool> GenerateWalls()
        {
            var map = new ArrayMap<bool>(this.Width, this.mapHeight);
            GoRogue.MapGeneration.Generators.CellularAutomataGenerator.Generate(map, MainGameConsole.GlobalRandom, 40);
            var random = new Random();

            for (var y = 0; y < this.mapHeight; y++) {
                for (var x = 0; x < this.Width; x++) {
                    // Invert. We want an internal cave surrounded by walls.
                    var wallColour = random.Next(1, 100) <= 30 ? Palette.Blue : Palette.DarkestBlue;
                    map[x, y] = !map[x, y];

                    if (map[x, y]) {
                        this.walls.Add(new Entity(x, y, '#', wallColour)); // FOV determines colour
                    }
                }
            }

            return map;
        }

        public override void Update(System.TimeSpan delta)
        {
            bool playerPressedKey = this.ProcessPlayerInput();

            // TODO: override Draw and put this in there. And all the infrastructure that requires.
            // Eg. Program.cs must call Draw on the console; and, changing consoles should work.
            this.RedrawEverything();
        }


        private bool TryToMove(Entity entity, int targetX, int targetY)
        {
            // Assuming targetX/targetY are adjacent, or entity can fly/teleport, etc.
            if (this.IsWalkable(targetX, targetY))
            {
                entity.X = targetX;
                entity.Y = targetY;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ProcessPlayerInput()
        {            
            var processedInput = false;

            if (Global.KeyboardState.IsKeyPressed(Keys.Escape) || Global.KeyboardState.IsKeyPressed(Keys.Q))
            {
                Environment.Exit(0);
            }
            
            var destinationX = this.player.X;
            var destinationY = this.player.Y;
            
            if ((Global.KeyboardState.IsKeyPressed(Keys.W) || Global.KeyboardState.IsKeyPressed(Keys.Up)))
            {
                destinationY -= 1;
            }
            else if ((Global.KeyboardState.IsKeyPressed(Keys.S) || Global.KeyboardState.IsKeyPressed(Keys.Down)))
            {
                destinationY += 1;
            }

            if ((Global.KeyboardState.IsKeyPressed(Keys.A) || Global.KeyboardState.IsKeyPressed(Keys.Left)))
            {
                destinationX -= 1;
            }
            else if ((Global.KeyboardState.IsKeyPressed(Keys.D) || Global.KeyboardState.IsKeyPressed(Keys.Right)))
            {
                destinationX += 1;
            }
            
            if (this.TryToMove(player, destinationX, destinationY))
            {
                processedInput = true;
                this.latestMessage = "";
            }
            else if (Global.KeyboardState.IsKeyPressed(Keys.OemPeriod) || Global.KeyboardState.IsKeyPressed(Keys.Space))
            {
                // Skip turn
                processedInput = true;
            }

            return processedInput;
        }

        private void RedrawEverything()
        {
            // One day, I will do better. One day, I will efficiently draw only what changed!
            for (var y = 0; y < this.mapHeight; y++)
            {
                for (var x = 0; x < this.Width; x++)
                {
                    var colour = Palette.Grey;
                    if (IsInPlayerFov(x, y))
                    {
                        colour = Palette.LightGrey;
                    }
                    this.DrawCharacter(x, y, '.', colour);
                }
            }

            foreach (var wall in this.walls)
            {
                var colour = Palette.DarkBlueMuted;
                if (IsInPlayerFov((int)wall.X, (int)wall.Y))
                {
                    colour = wall.Color;
                }
                this.DrawCharacter(wall.X, wall.Y, wall.Character, colour);
            }

            this.DrawCharacter(player.X, player.Y, player.Character, player.Color);

            this.DrawLine(new Point(0, this.Height - 2), new Point(this.Width, this.Height - 2), null, Palette.BlackAlmost, ' ');
            this.DrawLine(new Point(0, this.Height - 1), new Point(this.Width, this.Height - 1), null, Palette.BlackAlmost, ' ');
            this.DrawHealthIndicators();
            this.Print(0, this.Height - 1, this.latestMessage, Palette.White);
        }

        private void DrawHealthIndicators()
        {
            string message = $"You: 20/20";
            
            // foreach (var monster in this.monsters)
            // {
            //     var distance = Math.Sqrt(Math.Pow(monster.X - player.X, 2) + Math.Pow(monster.Y - player.Y, 2));
            //     if (distance <= 1)
            //     {
            //         // compact
            //         message = $"{message} {monster.Character}: {monster.CurrentHealth}/{monster.TotalHealth}"; 
            //     }
            // }

            this.Print(1, this.Height - 2, message, Palette.White);
        }

        private bool IsInPlayerFov(int x, int y)
        {
            // Doesn't use LoS calculations, just simple range check
            var distance = Math.Sqrt(Math.Pow(player.X - x, 2) + Math.Pow(player.Y - y, 2));
            return distance <= player.VisionRange;
        }

        private Vector2 FindEmptySpot()
        {
            int targetX = 0;
            int targetY = 0;
            
            do 
            {
                targetX = MainGameConsole.GlobalRandom.Next(0, this.Width);
                targetY = MainGameConsole.GlobalRandom.Next(0, this.mapHeight);
            } while (!this.IsWalkable(targetX, targetY));

            return new Vector2(targetX, targetY);
        }

        private Entity GetMonsterAt(int x, int y)
        {
            // BUG: (secondary?) knockback causes two monsters to occupy the same space!!!
            return this.monsters.FirstOrDefault(m => m.X == x && m.Y == y);
        }

        private bool IsWalkable(int x, int y)
        {
            if (this.walls.Any(w => w.X == x && w.Y == y))
            {
                return false;
            }

            if (this.GetMonsterAt(x, y) != null)
            {
                return false;
            }

            if (this.player.X == x && this.player.Y == y)
            {
                return false;
            }

            return true;
        }
    }
}