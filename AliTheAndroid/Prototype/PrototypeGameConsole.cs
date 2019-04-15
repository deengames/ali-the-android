using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GoRogue.MapViews;
using Troschuetz.Random;
using Troschuetz.Random.Generators;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Events;
using Global = SadConsole.Global;
using AliTheAndroid.Prototype;
using AliTheAndroid.Enums;

namespace DeenGames.AliTheAndroid.Prototype
{
    public class PrototypeGameConsole : SadConsole.Console
    {
        public static readonly IGenerator GlobalRandom;

        private const int MaxRooms = 10;
        // These are exterior sizes (walls included)
        private const int MinRoomSize = 7;
        private const int MaxRoomSize = 10;
        private const int ExplosionRadius = 1;

        private static readonly int? GameSeed = null;


        private readonly Player player;
        private readonly List<Entity> monsters = new List<Entity>();
        private readonly List<AbstractEntity> walls = new List<AbstractEntity>();
        private readonly List<Effect> effectEntities = new List<Effect>();

        private readonly int mapHeight;

        private string latestMessage = "";
        private ArrayMap<bool> map;
        
        // Super hack. Key is "x, y", value is IsDiscovered.
        private Dictionary<string, bool> isTileDiscovered = new Dictionary<string, bool>();


        static PrototypeGameConsole() {
            if (!GameSeed.HasValue) {
                GameSeed = new Random().Next();
            }
            
            System.Console.WriteLine($"Universe #{GameSeed.Value}");
            GlobalRandom = new StandardGenerator(GameSeed.Value);
        }

        public PrototypeGameConsole(int width, int height) : base(width, height)
        {
            this.mapHeight = height - 2;
            this.player = new Player();

            this.map = this.GenerateWalls();
            this.GenerateMonsters();

            var emptySpot = this.FindEmptySpot();
            player.X = (int)emptySpot.X;
            player.Y = (int)emptySpot.Y;

            this.RedrawEverything();

            EventBus.Instance.AddListener(GameEvent.EntityDeath, (e) => {
                if (e == player)
                {
                    this.latestMessage = "YOU DIE!!!";
                    this.player.Character = '%';
                    this.player.Color = Enums.Palette.DarkBurgandyPurple;

                    this.RedrawEverything();
                }
                else
                {
                    this.monsters.Remove(e as Entity);
                }
            });
        }

        private Tuple<int, int> FindEmptyLocation(ArrayMap<bool> map, List<Entity> monsters, List<AbstractEntity> walls)
        {
            while (true) {
                var x = PrototypeGameConsole.GlobalRandom.Next(0, map.Width);
                var y = PrototypeGameConsole.GlobalRandom.Next(0, map.Height);

                if (map[x, y] == false && monsters.All(m => m.X != x || m.Y != y) && walls.All(w => w.X != x || w.Y != y)) {
                    return new Tuple<int, int>(x, y);
                }
            }
        }

        private ArrayMap<bool> GenerateWalls()
        {
            var map = new ArrayMap<bool>(this.Width, this.mapHeight);
            var rooms = GoRogue.MapGeneration.QuickGenerators.GenerateRandomRoomsMap(map, MaxRooms, MinRoomSize, MaxRoomSize);

            for (var y = 0; y < this.mapHeight; y++) {
                for (var x = 0; x < this.Width; x++) {
                    // Invert. We want an internal cave surrounded by walls.
                    map[x, y] = !map[x, y];
                    if (map[x, y]) {
                        this.walls.Add(new AbstractEntity(x, y, '#', Palette.LightGrey)); // FOV determines colour
                    }
                }
            }

            return map;
        }

        public override void Update(System.TimeSpan delta)
        {
            bool playerPressedKey = this.ProcessPlayerInput();

            if (playerPressedKey)
            {
                this.ConsumePlayerTurn();
            }

            if (this.effectEntities.Any()) {
                // Move all effects.
                foreach (var effect in this.effectEntities)
                {
                    effect.OnUpdate();
                    // For out-of-sight effects, accelerate to the point that they destroy.
                    // This prevents the player from waiting, frozen, for out-of-sight shots.
                    if (!this.IsInPlayerFov(effect.X, effect.Y)) {
                        effect.OnAction();
                    }
                }

                var explosions = this.effectEntities.Where(e => e.Character == '*');
                if (explosions.Any(e => e.X == player.X && e.Y == player.Y)) {
                    player.Damage(CalculateDamage(Weapon.MiniMissile));
                }
                
                // Destroy any effect that hit something (wall/monster/etc.)
                // Force copy via ToList so we evaluate now. If we evaluate after damage, this is empty on monster kill.
                var destroyedEffects = this.effectEntities.Where((e) => !e.IsAlive || !this.IsWalkable(e.X, e.Y)).ToList();
                // If they hit a monster, damage it.
                var harmedMonsters = this.monsters.Where(m => destroyedEffects.Any(e => e.X == m.X && e.Y == m.Y)).ToArray(); // Create copy to prevent concurrent modification exception
                
                foreach (var monster in harmedMonsters) {
                    var hitBy = destroyedEffects.Single(e => e.X == monster.X && e.Y == monster.Y);
                    var type = CharacterToWeapon(hitBy.Character);
                    var damage = CalculateDamage(type);
                    monster.Damage(damage);
                }

                var missiles = destroyedEffects.Where(e => e.Character == '!');
                foreach (var missile in missiles) {
                    this.CreateExplosion(missile.X, missile.Y);
                }

                // Trim all dead effects
                this.effectEntities.RemoveAll(e => destroyedEffects.Contains(e));
            }
            
            if (!this.player.CanMove && !this.effectEntities.Any()) {
                this.player.Unfreeze();
                this.ConsumePlayerTurn();
            }

            // TODO: override Draw and put this in there. And all the infrastructure that requires.
            // Eg. Program.cs must call Draw on the console; and, changing consoles should work.
            this.RedrawEverything();
        }

        private void CreateExplosion(int centerX, int centerY) {
            for (var y = centerY - ExplosionRadius; y <= centerY + ExplosionRadius; y++) {
                for (var x = centerX - ExplosionRadius; x <= centerX + ExplosionRadius; x++) {
                    this.effectEntities.Add(new Explosion(x, y));
                }
            }
        }

        private int CalculateDamage(Weapon weapon)
        {
            switch(weapon) {
                case Weapon.Blaster: return player.Strength;
                case Weapon.MiniMissile: return player.Strength * 2;
                case Weapon.ShockZone: return player.Strength * 4;
                case Weapon.PlasmaCannon: return player.Strength * 3;
                default: return -1;
            }
        }

        private Weapon CharacterToWeapon(char display) {
            switch(display) {
                case '~': return Weapon.Blaster;
                case '!': return Weapon.MiniMissile;
                case '*': return Weapon.MiniMissile; // explosion
                case '%': return Weapon.ShockZone;
                case 'o': return Weapon.PlasmaCannon;
            }
            throw new InvalidOperationException($"{display} ???");
        }

        private void ConsumePlayerTurn()
        {
                this.ProcessMonsterTurns();
        }

        private void ProcessMonsterTurns()
        {
            foreach (var monster in this.monsters)
            {
                var distance = Math.Sqrt(Math.Pow(player.X - monster.X, 2) + Math.Pow(player.Y - monster.Y, 2));

                // Monsters who you can see, or hurt monsters, attack.
                if (!monster.IsDead && (distance <= monster.VisionRange || monster.CurrentHealth < monster.TotalHealth))
                {
                    // Process turn.
                    if (distance <= 1)
                    {
                        // ATTACK~!
                        var damage = monster.Strength - player.Defense;
                        player.Damage(damage);
                        this.latestMessage += $" {monster.Name} attacks for {damage} damage!";
                    }
                    else
                    {
                        // Move closer. Naively. Randomly.
                        var dx = player.X - monster.X;
                        var dy = player.Y - monster.Y;
                        var tryHorizontallyFirst = PrototypeGameConsole.GlobalRandom.Next(0, 100) <= 50;
                        if (tryHorizontallyFirst && dx != 0)
                        {
                            this.TryToMove(monster, monster.X + Math.Sign(dx), monster.Y);
                        }
                        else
                        {
                            this.TryToMove(monster, monster.X, monster.Y + Math.Sign(dy));
                        }
                    }
                }
            }
        }

        private bool TryToMove(Entity entity, int targetX, int targetY)
        {
            // Assuming targetX/targetY are adjacent, or entity can fly/teleport, etc.
            if (this.IsWalkable(targetX, targetY))
            {
                var previousX = entity.X;
                var previousY = entity.Y;

                entity.X = targetX;
                entity.Y = targetY;

                if (entity == player)
                {
                    player.OnMove(previousX, previousY);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ProcessPlayerInput()
        {            
            if (player.IsDead) {
                return false; // don't pass time
            }

            if (!player.CanMove) {
                return false;
            }

            var processedInput = false;

            if (Global.KeyboardState.IsKeyPressed(Keys.Escape))
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
            else if ((Global.KeyboardState.IsKeyPressed(Keys.Q)))
            {
                player.TurnCounterClockwise();
            }
            else if ((Global.KeyboardState.IsKeyPressed(Keys.E)))
            {
                player.TurnClockwise();
            }
            else if (Global.KeyboardState.IsKeyPressed(Keys.F))
            {
                this.FireShot();
            }
            else if (Global.KeyboardState.IsKeyPressed(Keys.NumPad1))
            {
                player.CurrentWeapon = Weapon.Blaster;
            }
            else if (Global.KeyboardState.IsKeyPressed(Keys.NumPad2))
            {
                player.CurrentWeapon = Weapon.MiniMissile;
            }
            else if (Global.KeyboardState.IsKeyPressed(Keys.NumPad3))
            {
                player.CurrentWeapon = Weapon.ShockZone;
            }
            else if (Global.KeyboardState.IsKeyPressed(Keys.NumPad4))
            {
                player.CurrentWeapon = Weapon.PlasmaCannon;
            }
            
            if (this.TryToMove(player, destinationX, destinationY))
            {
                // This is too late - player already moved. For the prototype, we can live with this.
                int viewRadius = (int)Math.Ceiling(player.VisionRange / 2.0);
                for (var y = player.Y - viewRadius; y <= player.Y + viewRadius; y++)
                {
                    for (var x = player.X - viewRadius; x <= player.X + viewRadius; x++)
                    {
                        // Just to be sure
                        if (IsInPlayerFov(x, y))
                        {
                            this.MarkAsSeen(x, y);
                        }
                    }
                }

                processedInput = true;
                this.latestMessage = "";
            }
            else if (this.GetMonsterAt(destinationX, destinationY) != null)
            {
                var monster = this.GetMonsterAt(destinationX, destinationY);
                processedInput = true;

                var damage = player.Strength - monster.Defense;
                monster.Damage(damage);
                this.latestMessage = $"You hit {monster.Name} for {damage} damage!";
            }
            else if (Global.KeyboardState.IsKeyPressed(Keys.OemPeriod) || Global.KeyboardState.IsKeyPressed(Keys.Space))
            {
                // Skip turn
                processedInput = true;
            }

            if (player.CurrentHealth <= 0)
            {
                this.latestMessage = "YOU DIE!!!!";
            }

            return processedInput;
        }

        private void FireShot()
        {
            // Blaster: ~
            // Missle: !
            // Shock: $
            // Plasma: o
            var character = '~';
            switch (player.CurrentWeapon) {
                case Weapon.Blaster:
                    character = '~';
                    break;
                case Weapon.MiniMissile:
                    character = '!';
                    break;
                case Weapon.ShockZone:
                    character = '%';
                    break;
                case Weapon.PlasmaCannon:
                    character = 'o';
                    break;
            }

            var dx = 0;
            var dy = 0;

            switch(player.DirectionFacing) {
                case Direction.Up: dy = -1; break;
                case Direction.Down: dy = 1; break;
                case Direction.Left: dx = -1; break;
                case Direction.Right: dx += 1; break;
                default: throw new InvalidOperationException(nameof(player.DirectionFacing));
            }

            var shot = new Shot(player.X + dx, player.Y + dy, character, Palette.Red, player.DirectionFacing, this.IsWalkable);
            effectEntities.Add(shot);

            this.player.Freeze();
        }

        private void RedrawEverything()
        {
            this.Fill(Palette.BlackAlmost, Palette.BlackAlmost, ' ');

            // One day, I will do better. One day, I will efficiently draw only what changed!
            for (var y = 0; y < this.mapHeight; y++)
            {
                for (var x = 0; x < this.Width; x++)
                {
                    if (IsInPlayerFov(x, y))
                    {
                        this.SetGlyph(x, y, '.', Palette.LightGrey);
                    }
                    else if (IsSeen(x, y))
                    {
                        this.SetGlyph(x, y, '.', Palette.Grey);
                    }
                }
            }

            foreach (var wall in this.walls)
            {
                var x = (int)wall.X;
                var y = (int)wall.Y;

                var colour = Palette.Grey;
                if (IsInPlayerFov(x, y))
                {
                    this.SetGlyph(wall.X, wall.Y, wall.Character, Palette.LightGrey);
                }
                else if (IsSeen(x, y))
                {
                    this.SetGlyph(wall.X, wall.Y, wall.Character, Palette.Grey);
                }
            }

            foreach (var monster in this.monsters)
            {                
                if (IsInPlayerFov(monster.X, monster.Y))
                {
                    var character = monster.Character;

                    this.SetGlyph(monster.X, monster.Y, character, monster.Color);
                    
                    if (monster.CurrentHealth < monster.TotalHealth) {
                        this.SetGlyph(monster.X, monster.Y, character, Palette.Orange);
                    }
                }
            }

            foreach (var effect in this.effectEntities) {
                if (IsInPlayerFov(effect.X, effect.Y)) {
                    this.SetGlyph(effect.X, effect.Y, effect.Character, effect.Color);
                }
            }

            this.SetGlyph(player.X, player.Y, player.Character, player.Color);

            this.DrawLine(new Point(0, this.Height - 2), new Point(this.Width, this.Height - 2), null, Palette.BlackAlmost, ' ');
            this.DrawLine(new Point(0, this.Height - 1), new Point(this.Width, this.Height - 1), null, Palette.BlackAlmost, ' ');
            this.DrawHealthIndicators();
            this.Print(0, this.Height - 1, this.latestMessage, Palette.White);
        }

        private void DrawHealthIndicators()
        {
            string message = $"You: {player.CurrentHealth}/{player.TotalHealth} (facing {player.DirectionFacing.ToString()}) Equipped: {player.CurrentWeapon}";
            
            foreach (var monster in this.monsters)
            {
                var distance = Math.Sqrt(Math.Pow(monster.X - player.X, 2) + Math.Pow(monster.Y - player.Y, 2));
                if (distance <= 1)
                {
                    // compact
                    message = $"{message} {monster.Character}: {monster.CurrentHealth}/{monster.TotalHealth}"; 
                }
            }

            this.Print(1, this.Height - 2, message, Palette.White);
        }

        private bool IsInPlayerFov(int x, int y)
        {
            // Doesn't use LoS calculations, just simple range check
            var distance = Math.Sqrt(Math.Pow(player.X - x, 2) + Math.Pow(player.Y - y, 2));
            return distance <= player.VisionRange;
        }

        private void GenerateMonsters()
        {
            var numMonsters = PrototypeGameConsole.GlobalRandom.Next(8, 9); // 8-9
            while (this.monsters.Count < numMonsters)
            {
                var spot = this.FindEmptySpot();
                var monster = Entity.CreateFromTemplate("Alien");
                monster.X = (int)spot.X;
                monster.Y = (int)spot.Y;
                this.monsters.Add(monster);
            }
        }

        private Vector2 FindEmptySpot()
        {
            int targetX = 0;
            int targetY = 0;
            
            do 
            {
                targetX = PrototypeGameConsole.GlobalRandom.Next(0, this.Width);
                targetY = PrototypeGameConsole.GlobalRandom.Next(0, this.mapHeight);
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

        private bool IsSeen(int x, int y)
        {
            string key = $"{x}, {y}";
            return isTileDiscovered.ContainsKey(key) && isTileDiscovered[key] == true;
        }

        private void MarkAsSeen(int x, int y)
        {
            string key = $"{x}, {y}";
            isTileDiscovered[key] = true;
        }
    }
}