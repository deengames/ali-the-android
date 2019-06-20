using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GoRogue.MapViews;
using Troschuetz.Random;
using DeenGames.AliTheAndroid.Enums;
using GoRogue.Pathing;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using Ninject;
using DeenGames.AliTheAndroid.Model.Events;
using DeenGames.AliTheAndroid.Loggers;

namespace DeenGames.AliTheAndroid.Model
{
    public class Floor
    {
        internal const int MinimumDistanceFromMonsterToStairs = 3; // Close, but not too close
        private const int MaxRooms = 10;
        // These are exterior sizes (walls included)
        private const int MinRoomSize = 7;
        private const int MaxRoomSize = 10;
        private const int ExplosionRadius = 2;
        private const int NumberOfLockedDoors = 3;
        private const int PlasmaResidueDamage = 10;
        private const int GravityRadius = 3;
        private const int ExtraGravityWaveRooms = 1;
        private const int NumChasms = 5;

        private const char GravityCannonShot = (char)246; 
        private const char InstaTeleporterShot = '?';
        private const int MinimumDistanceFromPlayerToStairs = 10; // be more than MaxRoomSize so they're not in the same room
        private const int MinimumChasmDistance = 3;
        

        public readonly List<Plasma> PlasmaResidue = new List<Plasma>();
        public readonly List<AbstractEntity> Walls = new List<AbstractEntity>();
        public readonly List<FakeWall> FakeWalls = new List<FakeWall>();
        public readonly List<Door> Doors = new List<Door>();
        public GoRogue.Coord StairsDownLocation = GoRogue.Coord.NONE;
        public GoRogue.Coord StairsUpLocation = GoRogue.Coord.NONE;
        public readonly List<Effect> EffectEntities = new List<Effect>();
        public readonly List<GravityWave> GravityWaves = new List<GravityWave>();
        public readonly List<AbstractEntity> Chasms = new  List<AbstractEntity>();
        public readonly IList<Entity> Monsters = new List<Entity>();
        public readonly IList<PowerUp> PowerUps = new List<PowerUp>();
        public Player Player;
        public IList<PowerUp> GuaranteedPowerUps = new List<PowerUp>();
        public WeaponPickUp WeaponPickUp = null;
        
        // Internal for unit testing
        internal ArrayMap<bool> map; // Initial map ONLY: no secret rooms, monsters, locked doors, etc. true = walkable
        internal IList<GoRogue.Rectangle> rooms = new List<GoRogue.Rectangle>();

        private int floorNum = 0;
        private int width = 0;
        private int height = 0;
        // Used for deterministic things like dungeon generation
        private IGenerator globalRandom;
        // Used for non-deterministic things, like monster movement
        private Random random = new Random(); 


        // Super hack. Key is "x, y", value is IsDiscovered.
        private Dictionary<string, bool> isTileDiscovered = new Dictionary<string, bool>();


        private string lastMessage = "";
        private IKeyboard keyboard;
        private bool generatedPowerUps = false;
        private GoRogue.FOV playerFieldOfView;

        // 2 = B2
        private static readonly IDictionary<string, int> monsterFloors = new Dictionary<string, int>() {
            { "slink", 2 },
            { "tenlegs", 4 },
            { "zug", 6 },
            { "boss", 10 },
        };

        // 2 = B2
        private static readonly IDictionary<Weapon, int> weaponPickUpFloors = new Dictionary< Weapon, int > {
            { Weapon.MiniMissile, 2 },
            { Weapon.Zapper, 4 },
            { Weapon.GravityCannon, 6 }, // Guaranteed to be between player and gravity waves
            // Generates on B8, first chasm is B9
            { Weapon.InstaTeleporter, 8 },
            { Weapon.PlasmaCannon, 9 }, // In the final, darkest floor
        };

        // TODO: should not be publically settable
        public string LatestMessage { 
            get {
                return this.lastMessage;
            }
            set {
                if (!string.IsNullOrWhiteSpace(value)) {
                    Console.WriteLine(value);
                }
                this.lastMessage = value;
            }
        }

        public Floor(int width, int height, int floorNum, IGenerator globalRandom, IList<PowerUp> guaranteedPowerUps)
        {
            this.width = width;
            this.height = height;
            this.floorNum = floorNum;
            this.globalRandom = globalRandom;
            this.GuaranteedPowerUps = guaranteedPowerUps;
            this.keyboard = DependencyInjection.kernel.Get<IKeyboard>();

            this.PlasmaResidue = new List<Plasma>();

            this.GenerateMap();
            this.playerFieldOfView = new GoRogue.FOV(map);

            var eventBus = EventBus.Instance;

            eventBus.AddListener(GameEvent.PlayerTookTurn, (obj) =>
            {
                if (Dungeon.Instance.CurrentFloorNum == this.floorNum)
                {
                    this.PlayerTookTurn();
                }
            });

            eventBus.AddListener(GameEvent.EntityDeath, (e) =>
            {
                if (Dungeon.Instance.CurrentFloorNum == this.floorNum && e == Player)
                {
                    LastGameLogger.Instance.Log($"Player died!!!");
                    this.LatestMessage = "YOU DIE!!! Press ESC to quit.";
                    this.Player.Character = '%';
                    this.Player.Color = Palette.DarkBurgandyPurple;
                }
                else
                {
                    this.Monsters.Remove(e as Entity);
                }
            });

            eventBus.AddListener(GameEvent.EggHatched, (e) => {
                if (Dungeon.Instance.CurrentFloorNum == this.floorNum)
                {
                    var position = (GoRogue.Coord)e;
                    // Remove egg
                    var egg = this.Monsters.SingleOrDefault(m => m.X == position.X && m.Y == position.Y && m is Egg);
                    // Null if it gets killed before it hatches. I removed the event listener in Die() but, alas.
                    if (egg != null)
                    {
                        this.Monsters.Remove(egg);
                    }

                    // Add monster
                    this.Monsters.Add(Entity.CreateFromTemplate("Fuseling", position.X, position.Y));
                }
            });
        }

        public void Update(System.TimeSpan delta)
        {
            bool playerTookTurn = this.ProcessPlayerInput();

            if (playerTookTurn)
            {
                EventBus.Instance.Broadcast(GameEvent.PlayerTookTurn, new PlayerTookTurnData(Player, this.Monsters));
                this.RecalculatePlayerFov();
            }

            if (this.EffectEntities.Any()) {
                // Process all effects.
                foreach (var effect in this.EffectEntities)
                {
                    effect.OnUpdate();
                    // For out-of-sight effects, accelerate to the point that they destroy.
                    // This prevents the player from waiting, frozen, for out-of-sight shots.
                    if (!this.IsInPlayerFov(effect.X, effect.Y) && !Options.EnableOmniSight) {
                        effect.OnAction();
                    }
                }

                // Harm the player from explosions/zaps.
                var backlashes = this.EffectEntities.Where(e => e.Character == '*' || e.Character == '$');
                var playerBacklashes = (backlashes.Where(e => e.X == Player.X && e.Y == Player.Y));

                foreach (var backlash in playerBacklashes) {
                    var damage = this.CalculateDamage(backlash.Character);
                    Console.WriteLine("Player damaged by backlash for " + damage + " damage!");
                    Player.Damage(damage);
                }

                // Unlock doors hit by bolts
                foreach (var bolt in backlashes.Where(b => b.Character == '$')) {
                    foreach (var door in Doors.Where(d => d.IsLocked && d.X == bolt.X && d.Y == bolt.Y)) {
                        door.IsLocked = false;
                        this.LatestMessage = "You unlock the door!";
                    }
                }

                // Find and destroy fake walls
                var destroyedFakeWalls = new List<AbstractEntity>();
                this.FakeWalls.ForEach(f => {
                    if (backlashes.Any(e => e.X == f.X && e.Y == f.Y && e.Character == '*')) {
                        destroyedFakeWalls.Add(f);
                    }
                });

                if (destroyedFakeWalls.Any()) {
                    this.LatestMessage = "You discovered a secret room!";
                }
                this.FakeWalls.RemoveAll(e => destroyedFakeWalls.Contains(e));

                // Process if the player shot a plasma shot. 
                var plasmaShot = this.EffectEntities.SingleOrDefault(e => e.Character == 'o') as Shot;
                if (plasmaShot != null) {
                    // If we moved, make sure there's plasma residue behind us
                    if (plasmaShot.HasMoved) {
                        var previousX = plasmaShot.X;
                        var previousY = plasmaShot.Y;

                        switch (plasmaShot.Direction) {
                            case Direction.Up:
                                previousY += 1;
                                break;
                            case Direction.Right:
                                previousX -= 1;
                                break;
                            case Direction.Down:
                                previousY -= 1;
                                break;
                            case Direction.Left:
                                previousX += 1;
                                break;
                        }

                        if (!PlasmaResidue.Any(f => f.X == previousX && f.Y == previousY))
                        {
                            this.AddNonDupeEntity(new Plasma(previousX, previousY), this.PlasmaResidue);
                        }
                    }
                }

                // Destroy any effect that hit something (wall/monster/etc.)
                // Force copy via ToList so we evaluate now. If we evaluate after damage, this is empty on monster kill.
                var destroyedEffects = this.EffectEntities.Where((e) => !e.IsAlive || (!(e is TeleporterShot) && !this.IsFlyable(e.X, e.Y))).ToList();
                // If they hit a monster, damage it.
                var harmedMonsters = this.Monsters.Where(m => destroyedEffects.Any(e => e.X == m.X && e.Y == m.Y)).ToArray(); // Create copy to prevent concurrent modification exception
                
                foreach (var monster in harmedMonsters) {
                    var hitBy = destroyedEffects.Single(e => e.X == monster.X && e.Y == monster.Y);
                    var type = CharacterToWeapon(hitBy.Character);
                    var damage = CalculateDamage(type);

                    monster.Damage(damage);

                    // Thunder damage hits adjacent monsters. Spawn more bolts~!
                    if (hitBy.Character == '$') {
                        // Crowded areas can cause multiple bolts on the same monster.
                        // This is not intended. A .Single call above will fail.
                        this.AddNonDupeEntity(new Bolt(monster.X - 1, monster.Y), this.EffectEntities);
                        this.AddNonDupeEntity(new Bolt(monster.X + 1, monster.Y), this.EffectEntities);
                        this.AddNonDupeEntity(new Bolt(monster.X, monster.Y - 1), this.EffectEntities);
                        this.AddNonDupeEntity(new Bolt(monster.X, monster.Y + 1), this.EffectEntities);
                    }
                }

                // Find destroyed gravity shots and perturb stuff appropriately
                var gravityShot = destroyedEffects.SingleOrDefault(e => e.Character == GravityCannonShot) as Shot;
                if (gravityShot != null) {                    
                    foreach (var monster in this.Monsters) {
                        var distance = (int)Math.Ceiling(Math.Sqrt(Math.Pow(monster.X - gravityShot.X, 2) + Math.Pow(monster.Y - gravityShot.Y, 2)));
                        if (distance <= GravityRadius) {
                            int moveBy = GravityRadius - distance;
                            this.ApplyKnockbacks(monster, gravityShot.X, gravityShot.Y, moveBy, gravityShot.Direction);
                        }
                    }

                    var playerDistance = (int)Math.Ceiling(Math.Sqrt(Math.Pow(Player.X - gravityShot.X, 2) + Math.Pow(Player.Y - gravityShot.Y, 2)));
                    if (playerDistance <= GravityRadius) {
                        int moveBy = GravityRadius - playerDistance;
                        this.ApplyKnockbacks(Player, gravityShot.X, gravityShot.Y, moveBy, gravityShot.Direction);
                    }
                }
                
                // Find active gravity shots and destroy rooms full of gravity waves appropriately
                gravityShot = EffectEntities.SingleOrDefault(e => e.Character == GravityCannonShot) as Shot;
                if (gravityShot != null) {
                    var room = this.rooms.SingleOrDefault(r => r.Contains(new GoRogue.Coord(gravityShot.X, gravityShot.Y)));
                    if (room != GoRogue.Rectangle.EMPTY) {
                        var waves = this.GravityWaves.Where(g => room.Contains(new GoRogue.Coord(g.X, g.Y)));
                        waves.ToList().ForEach(w => w.StopReactingToPlayer());
                        this.GravityWaves.RemoveAll(w => waves.Contains(w));
                    }
                }

                var teleporterShot = destroyedEffects.SingleOrDefault(s => s.Character == InstaTeleporterShot) as TeleporterShot;
                if (teleporterShot != null) {
                    Player.X = teleporterShot.TeleportTo.X;
                    Player.Y = teleporterShot.TeleportTo.Y;
                    this.OnPlayerMoved();
                }

                // Missiles explode
                var missiles = destroyedEffects.Where(e => e.Character == '!');
                foreach (var missile in missiles) {
                    this.CreateExplosion(missile.X, missile.Y);
                }

                // Trim all dead effects
                this.EffectEntities.RemoveAll(e => destroyedEffects.Contains(e));
            }
            
            if (!this.Player.CanMove && !this.EffectEntities.Any()) {
                this.Player.Unfreeze();
                EventBus.Instance.Broadcast(GameEvent.PlayerTookTurn, new PlayerTookTurnData(Player, this.Monsters));
            }
        }
        
        public bool IsInPlayerFov(int x, int y)
        {
            if (x < 0 || y < 0 || x >= this.width || y >= this.height)
            {
                return false; // Out of bounds = not visible
            }

#pragma warning disable
            if (Options.EnableOmniSight) {
                return true;
            }
#pragma warning restore

            return playerFieldOfView.BooleanFOV[x, y] == true;
        }

        public bool IsSeen(int x, int y)
        {
            string key = $"{x}, {y}";
            return isTileDiscovered.ContainsKey(key) && isTileDiscovered[key] == true;
        }

        public bool IsWalkable(int x, int y)
        {
            if (this.Chasms.Any(c => c.X == x && c.Y == y))
            {
                return false;
            }

            return this.IsFlyable(x, y);
        }

        public void RecalculatePlayerFov()
        {
            // Recalculate FOV
            playerFieldOfView.Calculate(Player.X, Player.Y, Player.VisionRange);
        }

        // Get the set of tiles spanning a path from the stairs up to the stairs down. Get all rooms that encompass those tiles.
        private List<GoRogue.Rectangle> RoomsInPathFromStairsToStairs()
        {
            // Plot a path from the player to the stairs. Pick one of those rooms in that path, and fill it with gravity.            
            var pathFinder = new AStar(map, GoRogue.Distance.EUCLIDEAN);
            var path = pathFinder.ShortestPath(StairsUpLocation, StairsDownLocation, true);

            var roomsInPath = new List<GoRogue.Rectangle>();

            foreach (var step in path.StepsWithStart)
            {
                var stepRoom = this.rooms.SingleOrDefault(r => r.Contains(step));
                if (stepRoom != GoRogue.Rectangle.EMPTY && !roomsInPath.Contains(stepRoom))
                {
                    roomsInPath.Add(stepRoom);
                }
            }

            return roomsInPath;
        }

        private void GenerateGravityWaves()
        {
            this.GravityWaves.Clear();

            var playerRoom = this.rooms.SingleOrDefault(r => r.Contains(new GoRogue.Coord(StairsUpLocation.X, StairsUpLocation.Y)));
            var roomsInPath = this.RoomsInPathFromStairsToStairs();
            roomsInPath.Remove(playerRoom);

            // If there are no rooms between us (stairs is in a hallway), we don't generate waves in this room.
            // If there's just one room - the stairs room - it will be full of gravity.
            // If there are two or more, pick one and gravity-fill it.
            GoRogue.Rectangle gravityRoom = GoRogue.Rectangle.EMPTY;

            if (roomsInPath.Any())
            {
                // Guaranteed not to be the player room. If there's only one room between these two, could be the exit room.
                gravityRoom = roomsInPath[this.globalRandom.Next(roomsInPath.Count)];
                this.FillWithGravity(gravityRoom);            
            }

            var extraRooms = ExtraGravityWaveRooms;
            var stairsUpCoordinates = new GoRogue.Coord(StairsUpLocation.X, StairsUpLocation.Y);
            var candidateRooms = rooms.Where(r => r != gravityRoom && !r.Contains(stairsUpCoordinates)).ToList();

            while (extraRooms > 0 && candidateRooms.Any())
            {
                var nextRoom = candidateRooms[this.globalRandom.Next(candidateRooms.Count)];
                this.FillWithGravity(nextRoom);
                candidateRooms.Remove(nextRoom);
                extraRooms -= 1;
            }
        }

        private void FillWithGravity(GoRogue.Rectangle room, bool isBacktrackingWave = false)
        {
            for (var y = room.MinExtentY; y <= room.MaxExtentY; y++)
            {
                for (var x = room.MinExtentX; x <= room.MaxExtentX; x++)
                {
                    this.GravityWaves.Add(new GravityWave(x, y, isBacktrackingWave, this.floorNum, this.IsWalkable));
                }
            }
        }

        // TODO: move this, and all generator functions, into a map-generator class. But, these mutate the existing floor.
        private void GenerateMap()
        {
            this.lastMessage = "";
            this.isTileDiscovered.Clear();
            this.PlasmaResidue.Clear();

            this.GenerateMapRooms();
            this.GenerateDoors();

            // Stairs before monsters because monsters don't generate close to stairs!
            this.GenerateStairs();
            this.GenerateMonsters();

            var actualFloorNum = this.floorNum + 1;
            if (actualFloorNum >= weaponPickUpFloors[Weapon.MiniMissile])
            {
                // Add one more fake wall cluster between the player and the stairs down.
                var pathFinder = new AStar(map, GoRogue.Distance.EUCLIDEAN);
                var path = pathFinder.ShortestPath(StairsUpLocation, StairsDownLocation, true);
                var middle = globalRandom.Next((int)(path.Length * 0.25), (int)(path.Length * 0.75));
                var midPath = path.GetStep(middle);
                this.CreateFakeWallClusterAt(midPath);
            }

            if (actualFloorNum >= weaponPickUpFloors[Weapon.GravityCannon])
            {
                this.GenerateGravityWaves();
            }
            
            if (actualFloorNum >= weaponPickUpFloors[Weapon.InstaTeleporter])
            {
                this.GenerateChasms();
            }

            this.GenerateBacktrackingObstacles();

            if (actualFloorNum == Dungeon.NumFloors)
            {
                this.GenerateBosses();
            }

            // Appropriately, remove stairs here, after we no longer need it for path-finding
            if (actualFloorNum == Dungeon.NumFloors)
            {
                this.StairsDownLocation = GoRogue.Coord.NONE;
            }

            this.GenerateWeaponPickUp();
        }

        // Generates things out-of-depth (eg. fake walls before the missile launcher pick-up or gaps before the teleporter pick-up).
        // Each one generates just one floor back, for simplicity and user experience (backtracking 2-4 floors is painful).

        // TODO: instead of using an existing room, generate a new room that connects to the rest on only one point. Makes it feel
        // more like a secret/intentional room that way. Instead, now, it can be a room with multiple paths/exits.
        private void GenerateBacktrackingObstacles()
        {            
            var actualFloorNumber = this.floorNum + 1; // 0 => B1, 8 => B9
            GoRogue.Rectangle room;

            if (actualFloorNumber == weaponPickUpFloors[Weapon.MiniMissile] - 1)
            {
                var secretRooms = this.GenerateSecretRooms(rooms, 1, true);
                if (secretRooms.Any())
                {
                    room = secretRooms.First();
                }
                else
                {
                    // Some dungeon layouts leave no space for secret rooms, because every room
                    // is either close to the edge or has a hallway poking out of the other side.
                    // https://trello.com/c/3xHowpLR/42-dungeon-crashes-because-it-cant-have-secret-rooms
                    room = this.CreateIsolatedRoom();
                    for (var y = room.MinExtentY; y <= room.MaxExtentY; y++) {
                        for (var x = room.MinExtentX; x <= room.MaxExtentX; x++) {
                            this.FakeWalls.Add(new FakeWall(x, y, true));
                        }
                    }
                }
                this.GeneratePowerUpsInRoom(room);
            }
            else if (actualFloorNumber == weaponPickUpFloors[Weapon.Zapper] - 1)
            {
                // Find a room NOT in the path from player to stairs. Lock it on all sides. DONE.
                var nonCriticalRoom = this.CreateIsolatedRoom();
                for (var x = nonCriticalRoom.MinExtentX; x <= nonCriticalRoom.MaxExtentX; x++)
                {
                    this.Doors.Add(new Door(x, nonCriticalRoom.MinExtentY, true, true));
                    this.Doors.Add(new Door(x, nonCriticalRoom.MaxExtentY, true, true));
                }
                // Don't create duplicates on the top/bottom, ignore min/max y
                for (var y = nonCriticalRoom.MinExtentY + 1; y <= nonCriticalRoom.MaxExtentY - 1; y++)
                {
                    this.Doors.Add(new Door(nonCriticalRoom.MinExtentX, y, true, true));
                    this.Doors.Add(new Door(nonCriticalRoom.MaxExtentX, y, true, true));
                }
                this.GeneratePowerUpsInRoom(nonCriticalRoom);
            }
            else if (actualFloorNumber == weaponPickUpFloors[Weapon.GravityCannon] - 1)
            {
                // We need a 9x9 to guarantee the player can't get these by brute-force.
                // Well, anyway, let them if they choose to. Some maps don't have a patch
                // of 9x9 walls (eg. seed 1234, B4).
                var nonCriticalRoom = this.CreateIsolatedRoom();
                this.FillWithGravity(nonCriticalRoom, true);
                this.GeneratePowerUpsInRoom(nonCriticalRoom);
            }
            else if (actualFloorNumber == weaponPickUpFloors[Weapon.InstaTeleporter] - 1)
            {
                // Fill the interior with chasms, so you can't get in without teleporting.
                var nonCriticalRoom = this.CreateIsolatedRoom();
                for (var x = nonCriticalRoom.MinExtentX; x <= nonCriticalRoom.MaxExtentX; x++)
                {
                    this.Chasms.Add(Entity.Create(SimpleEntity.Chasm, x, nonCriticalRoom.MinExtentY));
                    this.Chasms.Add(Entity.Create(SimpleEntity.Chasm, x, nonCriticalRoom.MaxExtentY));
                }
                // Don't create duplicates on the top/bottom, ignore min/max y
                for (var y = nonCriticalRoom.MinExtentY + 1; y <= nonCriticalRoom.MaxExtentY - 1; y++)
                {
                    this.Chasms.Add(Entity.Create(SimpleEntity.Chasm, nonCriticalRoom.MinExtentX, y));
                    this.Chasms.Add(Entity.Create(SimpleEntity.Chasm, nonCriticalRoom.MaxExtentX, y));
                }
                this.GeneratePowerUpsInRoom(nonCriticalRoom);
            }
        }

        private void GeneratePowerUpsInRoom(GoRogue.Rectangle room)
        {
            // Always generate them horizontally, just off-center.
            var types = new string[] {"health", "strength", "defense", "vision" };
            var picked = types.OrderBy(r => globalRandom.Next()).Take(2).ToList();

            var powerups = new List<PowerUp>();

            foreach (var type in picked)
            {
                switch (type) {
                    case "health":
                        powerups.Add(new PowerUp(0, room.Center.Y, true, healthBoost: PowerUp.TypicalHealthBoost));
                        break;
                     case "strength":
                        powerups.Add(new PowerUp(0, room.Center.Y, true, strengthBoost: PowerUp.TypicalStrengthBoost));
                        break;
                     case "defense":
                        powerups.Add(new PowerUp(0, room.Center.Y, true, defenseBoost: PowerUp.TypicalDefenseBoost));
                        break;
                     case "vision":
                        powerups.Add(new PowerUp(0, room.Center.Y, true, visionBoost: PowerUp.TypicalVisionBoost));
                        break;
                }
            }

            PowerUp.Pair(powerups[0], powerups[1]);
            powerups[0].X = room.Center.X - 1;
            powerups[1].X = room.Center.X + 1;

            foreach (var powerUp in powerups)
            {
                powerUp.OnPickUp(() => {
                    this.PowerUps.Remove(powerUp); // Redundant but easier to test
                    this.PowerUps.Remove(powerUp.PairedTo);
                });
            }

            powerups.ForEach(p => this.PowerUps.Add(p));    
        }

        // Creates an isolated 5x5 room by locating and then tunnelling out a 5x5 area of walls.
        // Then, finds the nearest room, and connects it to that naively (L-shaped tunnel).
        // BUG: generates a 1-tile larger room. If you say 7x7, it generates an 8x8. Dunno why.
        private GoRogue.Rectangle CreateIsolatedRoom(int width = 5, int height = 5)
        {
            var startSpot = new GoRogue.Coord(globalRandom.Next(this.width), globalRandom.Next(this.height));
            while (!this.IsWallRegion(startSpot, width, height))
            {
                startSpot = new GoRogue.Coord(globalRandom.Next(this.width), globalRandom.Next(this.height));
            }

            var toReturn = new GoRogue.Rectangle(startSpot.X, startSpot.Y, width, height);

            var innerWalls = this.Walls.Where(w => w.X >= startSpot.X && w.X < startSpot.X  + width && 
                w.Y >= startSpot.Y && w.Y < startSpot.Y + height);

            this.Walls.RemoveAll(w => innerWalls.Contains(w));

            // Find the nearest room and naively connect to it
            var nearestRoom = this.rooms.OrderBy(r => Math.Sqrt(Math.Pow(r.Center.X - toReturn.Center.X, 2) + Math.Pow(r.Center.Y - toReturn.Center.Y, 2))).First();

            if (globalRandom.NextBoolean())
            {
                // Horizontal, then vertical
                this.DigTunnel(toReturn.Center.X, toReturn.Center.Y, nearestRoom.Center.X, toReturn.Center.Y);
                this.DigTunnel(nearestRoom.Center.X, toReturn.Center.Y, nearestRoom.Center.X, nearestRoom.Center.Y);
            }
            else
            {
                // Vertical, then horizontal
                this.DigTunnel(toReturn.Center.X, toReturn.Center.Y, toReturn.Center.X, nearestRoom.Center.Y);
                this.DigTunnel(toReturn.Center.X, nearestRoom.Center.Y, nearestRoom.Center.X, nearestRoom.Center.Y);
            }

            return toReturn;
        }

        private void DigTunnel(int startX, int startY, int stopX, int stopY)
        {
            var minX = Math.Min(startX, stopX);
            var maxX = minX == startX ? stopX : startX;
            var minY = Math.Min(startY, stopY);
            var maxY = minY == startY ? stopY : startY;

            // I know it's only one direction, but it's easier to just iterate two-dimensionally
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var wall = this.Walls.SingleOrDefault(w => w.X == x && w.Y == y);
                    if (wall != null)
                    {
                        this.Walls.Remove(wall);
                    }
                }
            }
        }

        private bool IsWallRegion(GoRogue.Coord topLeftCorner, int width, int height)
        {
            return this.Walls.Where(w => w.X >= topLeftCorner.X && w.X < topLeftCorner.X  + width && 
                w.Y >= topLeftCorner.Y && w.Y < topLeftCorner.Y + height).Count() == width * height;
        }

        private void GenerateWeaponPickUp()
        {
            var actualFloorNumber = this.floorNum + 1; // 0 => B1, 8 => B9
            var weaponFloorNumbers = weaponPickUpFloors.Values;

            if (weaponFloorNumbers.Contains(actualFloorNumber))
            {
                var weaponType = weaponPickUpFloors.Single(w => w.Value == actualFloorNumber).Key;
                
                var floorTiles = this.GetTilesAccessibleFromStairsWithoutWeapons();

                var target = floorTiles.OrderByDescending(c => 
                    // Order by farthest to closest (compared to stairs)
                    Math.Sqrt(Math.Pow(c.X - this.StairsUpLocation.X, 2) + Math.Pow(c.Y - this.StairsUpLocation.Y, 2)))
                    // Pick randomly from the first 10
                    .Take(10).OrderBy(c => globalRandom.Next()).First();

                this.WeaponPickUp = new WeaponPickUp(target.X, target.Y, weaponType);
            }
        }

        // Start at the stairs-up. Flood fill floor tiles. Return the floor tiles that you can reach, without
        // wandering through locked doors, gravity waves, fake walls, or across chasms.
        private List<GoRogue.Coord> GetTilesAccessibleFromStairsWithoutWeapons()
        {
            var toExplore = new List<GoRogue.Coord>();
            var explored = new List<GoRogue.Coord>();
            var reachable = new List<GoRogue.Coord>();

            toExplore.Add(this.StairsUpLocation);

            while (toExplore.Any())
            {
                var check = toExplore.First();
                if (IsWalkable(check.X, check.Y) && !GravityWaves.Any(g => g.X == check.X && g.Y == check.Y))
                {
                    reachable.Add(check);

                    var left = new GoRogue.Coord(check.X - 1, check.Y);
                    var right = new GoRogue.Coord(check.X + 1, check.Y);
                    var up = new GoRogue.Coord(check.X, check.Y - 1);
                    var down = new GoRogue.Coord(check.X, check.Y + 1);

                    if (!toExplore.Contains(left) && !explored.Contains(left)) { toExplore.Add(left); }
                    if (!toExplore.Contains(right) &&!explored.Contains(right)) { toExplore.Add(right); }
                    if (!toExplore.Contains(up) &&!explored.Contains(up)) { toExplore.Add(up); }
                    if (!toExplore.Contains(down) &&!explored.Contains(down)) { toExplore.Add(down); }
                }

                explored.Add(check);
                toExplore.Remove(check);
            }

            return reachable;
        }

        private void GenerateBosses()
        {
            var bossLocation = this.StairsDownLocation;
        }

        private void GenerateChasms()
        {
            this.Chasms.Clear();
            
            // Pick hallways and fill them with chasms. Make sure they're far from each other.
            var hallwayTiles = new List<GoRogue.Coord>();
            for (var y = 0; y < this.height; y++)
            {
                for (var x = 0; x < this.width; x++)
                {
                    // Not 100% accurate since we have monsters, ec.
                    var coordinates = new GoRogue.Coord(x, y);
                    if (IsWalkable(x, y) && !this.rooms.Any(r => r.Contains(coordinates)))
                    {
                        hallwayTiles.Add(coordinates);
                    }
                }
            }

            var iterations = 0;  // Iterations because: hard to tell if we ran out of hallway tiles.
            var candidates = hallwayTiles.Where(h => this.IsChasmCandidate(h)).OrderBy(c => this.globalRandom.Next()).ToList();
            var defaultCoord = new GoRogue.Coord(0, 0);

            // Make sure we don't generate chasms too close to each other. This can make hallways impossible to traverse.
            // https://trello.com/c/HxpLSDMt/3-map-generates-a-stuck-map-seed-740970391
            while (iterations++ < 10000 && this.Chasms.Count < NumChasms) {
                var candidate = candidates.FirstOrDefault();
                candidates.Remove(candidate);
                // Coord is a struct, so we get (0, 0) (not Coord.NONE, strangely) sometimes...
                if (candidate != defaultCoord)
                {
                    if (!this.Chasms.Any())
                    {
                        this.GenerateChasmAt(candidate);
                    }
                    else
                    {
                        var isGenerated = this.GenerateChasmIfNotTooClose(candidate);
                        if (isGenerated)
                        {
                            candidates.Remove(candidate);
                        }
                    }
                }
            };

            // Fill rooms with chasms too
            while (this.Chasms.Count < NumChasms) 
            {
                var spot = this.FindEmptySpot();
                // Changing this to IsChasmCandidate causes tests to hang, because we don't have enough chasm-candidate
                // spots in some dungeons/maps.
                if (spot != StairsUpLocation && spot != StairsDownLocation)
                {
                    this.GenerateChasmIfNotTooClose(spot);
                }
            }
        }

        private bool GenerateChasmIfNotTooClose(GoRogue.Coord spot)
        {
            // Calculate the distance to the closest chasm, and make sure it's distant enough.
            var minDistance = this.Chasms.Any() ? this.Chasms.Select(c => Math.Sqrt(Math.Pow(c.X - spot.X, 2) + Math.Pow(c.Y - spot.Y, 2))).Min() : int.MaxValue;
            if (minDistance >= MinimumChasmDistance) {
                this.GenerateChasmAt(spot);
                return true;
            }
            
            return false;
        }

        private bool IsChasmCandidate(GoRogue.Coord spot)
        {
            if (this.CountAdjacentFloors(spot) != 2)
            {
                return false;
            }

            if (spot == StairsDownLocation || spot == StairsUpLocation)
            {
                return false;
            }

            if (this.IsWalkable(spot.X - 1, spot.Y) && this.IsWalkable(spot.X + 1, spot.Y))
            {
                return true;
            }

            if (this.IsWalkable(spot.X, spot.Y - 1) && this.IsWalkable(spot.X, spot.Y + 1))
            {
                return true;
            }

            return false;
        }

        ///
        // This tricky method is used for two things: generating if a spot is a legitimate chasm (spans a hallway -
        // meaning it has walkable tiles left/right or up/down), and filling in any square as a chasm (so we generate 'em).
        // In the former case, we check using IsChasmCandidate, which checks for up/down or left/right. If not, we check
        // using just a simple stairs-block check, so we don't generate chasms on the stairs, which is a bug
        // (see: https://trello.com/c/fmynV9Qa/41-test-fails-because-chasm-generates-on-stairs-up).
        private void GenerateChasmAt(GoRogue.Coord location) {
            if (location != StairsUpLocation && location != StairsDownLocation)
            {
                this.Chasms.Add(AbstractEntity.Create(SimpleEntity.Chasm, location.X, location.Y));
            }

            foreach (var adjacency in this.GetAdjacentFloors(location))
            {
                if (adjacency != StairsUpLocation && adjacency != StairsDownLocation)
                {
                    this.Chasms.Add(AbstractEntity.Create(SimpleEntity.Chasm, adjacency.X, adjacency.Y));
                }
            }
        }

        private void GenerateStairs()
        {
            // Stairs up generate under the player. Available on all floors (start location), but only usable/visible on floor > 0.
            this.StairsUpLocation = this.FindEmptySpot();

            // Stairs down generate far from the player.
            var spot = new GoRogue.Coord(StairsUpLocation.X, StairsUpLocation.Y);
            var distance = 0d;

            do {
                spot = this.FindEmptySpot();
                distance = Math.Sqrt(Math.Pow(spot.X - StairsUpLocation.X, 2)  + Math.Pow(spot.Y - StairsUpLocation.Y, 2));
            } while (distance <= MinimumDistanceFromPlayerToStairs);

            this.StairsDownLocation = spot;
        }

        // Called outside of the generation process because power-ups can't be determined ahead of time; they depend on
        // the player's choice. So we pass the list to each floor, and let each floor consume/update it appropriately.
        // TODO: put this back in GenerateMap
        public void GeneratePowerUps()
        {
            if (!this.generatedPowerUps)
            {
                var floorsNearStairs = this.GetAdjacentFloors(StairsDownLocation).Where(f => this.IsWalkable(f.X, f.Y)).ToList();
                if (floorsNearStairs.Count < 2)
                {
                    // No nearby floors? Look harder. This happens when you generate a floor with seed=1234
                    var aboveStairs = new GoRogue.Coord(StairsDownLocation.X, StairsDownLocation.Y - 1);
                    var belowStairs = new GoRogue.Coord(StairsDownLocation.X, StairsDownLocation.Y + 1);
                    var leftOfStairs = new GoRogue.Coord(StairsDownLocation.X - 1, StairsDownLocation.Y);
                    var rightOfStairs = new GoRogue.Coord(StairsDownLocation.X + 1, StairsDownLocation.Y);

                    var moreTiles = this.GetAdjacentFloors(aboveStairs);
                    moreTiles.AddRange(this.GetAdjacentFloors(belowStairs));
                    moreTiles.AddRange(this.GetAdjacentFloors(leftOfStairs));
                    moreTiles.AddRange(this.GetAdjacentFloors(rightOfStairs));

                    floorsNearStairs = moreTiles.Where(f => this.IsWalkable(f.X, f.Y)).ToList();
                }

                // Use Distinct here because we may get duplicate floors (probably if we have only <= 2 tiles next to stairs)
                // https://trello.com/c/Cp7V5SWW/43-dungeon-generates-with-two-power-ups-on-the-same-spot
                var locations = floorsNearStairs.Distinct().OrderBy(f => globalRandom.Next()).Take(2).ToArray();
                var powerUps = this.GuaranteedPowerUps.Take(2).ToArray();

                // TODO: link the power-ups so that: a) picking up one destroys the other, and b) remove the picked one from this.guaranteedPowerUps
                var choicePowerUps = new List<PowerUp>();

                for (var i = 0; i < locations.Count(); i++)
                {
                    var powerUp = this.GuaranteedPowerUps[i];
                    var location = locations[i];

                    powerUp.X = location.X;
                    powerUp.Y = location.Y;

                    choicePowerUps.Add(powerUp);
                    this.PowerUps.Add(powerUp);
                }

                PowerUp.Pair(powerUps[0], powerUps[1]);

                foreach (var powerUp in choicePowerUps)
                {
                    powerUp.OnPickUp(() => {
                        this.GuaranteedPowerUps.Remove(powerUp);
                        this.PowerUps.Remove(powerUp);
                        this.PowerUps.Remove(powerUp.PairedTo);
                    });
                }
                
                this.generatedPowerUps = true;
            }
        }

        private void GenerateMapRooms() {
            var actualFloorNum = this.floorNum + 1;

            this.rooms = this.GenerateWalls();            
            this.GenerateFakeWallClusters();

            if (actualFloorNum >= weaponPickUpFloors[Weapon.MiniMissile])
            {
                this.GenerateSecretRooms(rooms);
            }            
        }

        private IList<GoRogue.Rectangle> GenerateWalls()
        {
            this.Walls.Clear();
            this.map = new ArrayMap<bool>(this.width, this.height);
            // true = passable, check GoRogue docs.
            var rooms = GoRogue.MapGeneration.QuickGenerators.GenerateRandomRoomsMap(map, this.globalRandom, MaxRooms, MinRoomSize, MaxRoomSize);
            
            for (var y = 0; y < this.height; y++) {
                for (var x = 0; x < this.width; x++) {
                    if (!map[x, y]) {
                        this.Walls.Add(AbstractEntity.Create(SimpleEntity.Wall, x, y));
                    }
                }
            }

            return rooms.ToList();
        }

        private void GenerateFakeWallClusters()
        {
            this.FakeWalls.Clear();

            var actualFloorNum = this.floorNum + 1;
            if (actualFloorNum >= weaponPickUpFloors[Weapon.MiniMissile])
            {
                // Throw in a few fake walls in random places. Well, as long as that tile doesn't have more than 4 adjacent empty spaces.
                var numFakeWallClusters = 3;
                while (numFakeWallClusters > 0) {
                    var spot = this.FindEmptySpot();
                    var numFloors = this.CountAdjacentFloors(spot);
                    if (numFloors <= 4) {
                        // Make a plus-shaped cluster. It's cooler.
                        this.CreateFakeWallClusterAt(spot);
                        numFakeWallClusters -= 1;
                    }
                }
            }
        }

        private void CreateFakeWallClusterAt(GoRogue.Coord spot)
        {
            this.AddNonDupeEntity(new FakeWall(spot.X, spot.Y), this.FakeWalls);
            this.AddNonDupeEntity(new FakeWall(spot.X - 1, spot.Y), this.FakeWalls);
            this.AddNonDupeEntity(new FakeWall(spot.X + 1, spot.Y), this.FakeWalls);
            this.AddNonDupeEntity(new FakeWall(spot.X, spot.Y - 1), this.FakeWalls);
            this.AddNonDupeEntity(new FakeWall(spot.X, spot.Y + 1), this.FakeWalls);
        }

        private IEnumerable<GoRogue.Rectangle> GenerateSecretRooms(IEnumerable<GoRogue.Rectangle> rooms, int numRooms = 2, bool flagWallsAsBacktracking = false)
        {
            var actualFloorNum = this.floorNum + 1;

            var secretRooms = this.FindPotentialSecretRooms(rooms).Take(numRooms);
            foreach (var room in secretRooms) {
                // Fill the interior with fake walls. Otherwise, FOV gets complicated.
                // Trim perimeter by 1 tile so we get an interior only
                for (var y = room.Rectangle.Y + 1; y < room.Rectangle.Y + room.Rectangle.Height - 1; y++) {
                    for (var x = room.Rectangle.X + 1; x < room.Rectangle.X + room.Rectangle.Width - 1; x++) {
                        var wall = this.Walls.SingleOrDefault(w => w.X == x && w.Y == y);
                        if (wall != null) {
                            this.Walls.Remove(wall);
                        }

                        // Mark as "secret floor" if not perimeter
                        this.FakeWalls.Add(new FakeWall(x, y, flagWallsAsBacktracking));
                    }
                }

                // Hollow out the walls between us and the real room and fill it with fake walls
                var secretX = room.ConnectedOnLeft ? room.Rectangle.X + room.Rectangle.Width - 1 : room.Rectangle.X;
                for (var y = room.Rectangle.Y + 1; y < room.Rectangle.Y + room.Rectangle.Height - 1; y++) {
                    var wall = this.Walls.SingleOrDefault(w => w.X == secretX && w.Y == y);
                    if (wall != null) {
                        this.Walls.Remove(wall);
                    }

                    this.FakeWalls.Add(new FakeWall(secretX, y, flagWallsAsBacktracking));
                }
            }

            return secretRooms.Select(r => r.Rectangle);
        }

        private void GenerateDoors()
        {
            // Generate regular doors: any time we have a room, look at the perimeter tiles around that room.
            // If any of them have two ground tiles (including tiles with doors on them already), add a door.
            foreach (var room in rooms) {
                this.AddDoorsToRoom(room);
            }

            // Generate locked doors: random spots with only two surrounding ground tiles.
            var actualFloorNum = this.floorNum + 1;
            if (actualFloorNum >= weaponPickUpFloors[Weapon.Zapper])
            {
                var leftToGenerate = NumberOfLockedDoors;
                while (leftToGenerate > 0) {
                    var spot = this.FindEmptySpot();
                    if (IsDoorCandidate(spot))
                    {
                        this.Doors.Add(new Door(spot.X, spot.Y, true));
                        leftToGenerate--;
                    }
                }
            }
        }

        private void AddDoorsToRoom(GoRogue.Rectangle room, bool isLocked = false, bool isBacktrackingDoor = false)
        {
            var startX = room.X;
            var stopX = room.X + room.Width - 1;
            var startY = room.Y;
            var stopY = room.Y + room.Height - 1;

            for (var x = startX; x <= stopX; x++) {
                if (this.IsDoorCandidate(x, room.Y - 1)) {
                    this.Doors.Add(new Door(x, room.Y - 1, isLocked, isBacktrackingDoor));
                }
                if (this.IsDoorCandidate(x, room.Y + room.Height - 1)) {
                    this.Doors.Add(new Door(x, room.Y + room.Height - 1, isLocked, isBacktrackingDoor));
                }
            }

            for (var y = startY; y <= stopY; y++) {
                if (this.IsDoorCandidate(room.X, y)) {
                    this.Doors.Add(new Door(room.X, y, isLocked, isBacktrackingDoor));
                }
                if (this.IsDoorCandidate(room.X + room.Width - 1, y)) {
                    this.Doors.Add(new Door(room.X + room.Width - 1, y, isLocked, isBacktrackingDoor));
                }
            }
        }        

        private bool IsDoorCandidate(int x, int y)
        {
            return IsDoorCandidate(new GoRogue.Coord(x, y));
        }

        private bool IsDoorCandidate(GoRogue.Coord coordinates)
        {
            var x = coordinates.X;
            var y = coordinates.Y;
            // Walkable, in a somewhat enclosed area. 2 tiles for things that are hallways/corners, 4 tiles
            // for generating locked doors where rooms meet hallways - we get three or four floor tiles here
            return this.IsWalkable(x, y) && this.CountAdjacentFloors(coordinates) <= 4 &&
            // not near any other doors
            !this.Doors.Any(d => Math.Abs(d.X - x) + Math.Abs(d.Y - y) <= 3) &&
            // is walkable on two sides
            ((IsWalkable(x - 1, y) && IsWalkable(x + 1, y)) || (IsWalkable(x, y - 1) && IsWalkable(x, y + 1)));
        }

        // Only used for generating rock clusters and doors; ignores doors (they're considered walkable)
        internal int CountAdjacentFloors(GoRogue.Coord coordinates) {
            return GetAdjacentFloors(coordinates).Count;
        }

        internal List<GoRogue.Coord> GetAdjacentFloors(GoRogue.Coord coordinates) {
            return this.GetAdjacentFloors(coordinates.X, coordinates.Y);
        }

        private IEnumerable<ConnectedRoom> FindPotentialSecretRooms(IEnumerable<GoRogue.Rectangle> rooms)
        {
            // rooms has a strange invariant. It claims the room is 7x7 even though the interior is 5x5.
            // Must be because it generates the surrouding walls. Well, we subtract -2 because we just want interior sizes.
            // This is also why start coordinates sometimes have +1 (like Y) -- that's the interior.
            // We return candidate rooms that are *just* interior size, inclusive.
            var candidateRooms = new List<ConnectedRoom>();

            // All this +1 -1 +2 -2 is to make rooms line up perfectly
            foreach (var room in rooms) {
                // Check if the space immediately beside (left/right) of the room is vacant (all walls)
                // If so, hollow it out, and mark the border with fake walls.

                // LEFT
                if (IsAreaWalled(room.X - room.Width + 3, room.Y, room.X - 2, room.Y + room.Height - 2))
                {
                    candidateRooms.Add(new ConnectedRoom(room.X - room.Width + 3, room.Y + 1, room.Width - 2, room.Height - 2, true, room));
                }
                // Else here: don't want two secret rooms from the same one room
                // RIGHT
                else if (IsAreaWalled(room.X + room.Width - 1, room.Y, room.X + 2 * (room.Width - 2), room.Y + room.Height - 2))
                {
                    candidateRooms.Add(new ConnectedRoom(room.X + room.Width - 1, room.Y + 1, room.Width - 2, room.Height - 2, false, room));
                }
            }

            return candidateRooms;
        }

        private bool IsAreaWalled(int startX, int startY, int stopX, int stopY) {
            for (var y = startY; y < stopY; y++) {
                for (var x = startX; x < stopX; x++) {
                    if (!this.Walls.Any(w => w.X == x && w.Y == y)) {
                        return false;
                    }
                }
            }

            return true;
        }
        
        private void ApplyKnockbacks(Entity entity, int centerX, int centerY, int distance, Direction optionalDirection)
        {
            // Primary knockback in the direction of entity => cemter
            var dx = entity.X - centerX;
            var dy = entity.Y - centerY;
            if (dx == 0 && dy == 0) {
                // Special case of sorts: gravity shot hit dead-center on the entity; use directtion.
                switch (optionalDirection) {
                    case Direction.Down: dy += 1; break;
                    case Direction.Right: dx += 1; break;
                    case Direction.Up: dy -= 1; break;
                    case Direction.Left: dx -= 1; break;
                }
            }
            
            int startX = entity.X;
            int startY = entity.Y;
            int stopX = startX + distance * Math.Sign(dx);
            int stopY = startY + distance * Math.Sign(dy);

            // Horrible method but iterates in one direction only, guaranteed
            var minX = Math.Min(startX, stopX);
            var maxX = Math.Max(startX, stopX);
            var minY = Math.Min(startY, stopY);
            var maxY = Math.Max(startY, stopY);

            // Move if spaces are clear
            for (var y = minY; y <= maxY; y++) {
                for (var x = minX; x <= maxX; x++) {
                    // Check all spaces and move the entity one by one if the space is empty.
                    if (this.IsWalkable(entity.X + dx, entity.Y + dy))
                    {
                        // One of these is zero so we're really just moving in one direction.
                        entity.X += dx;                            
                        entity.Y += dy;
                    }
                }
            }
        }

        internal List<GoRogue.Coord> GetAdjacentFloors(int centerX, int centerY) {
            var toReturn = new List<GoRogue.Coord>();

            for (var y = centerY - 1; y <= centerY + 1; y++) {
                for (var x = centerX - 1; x <= centerX + 1; x++) {
                    if (IsWalkable(x, y))
                    {
                        toReturn.Add(new GoRogue.Coord(x, y));
                    }
                }
            }

            toReturn.Remove(toReturn.Find(c => c.X == centerX && c.Y == centerY));
            return toReturn;
        }

        private void AddNonDupeEntity<T>(T entity, List<T> collection) where T : AbstractEntity {
            if (!collection.Any(e => e.X == entity.X && e.Y == entity.Y)) {
                collection.Add(entity);
            }
        }

        private void CreateExplosion(int centerX, int centerY) {
            for (var y = centerY - ExplosionRadius; y <= centerY + ExplosionRadius; y++) {
                for (var x = centerX - ExplosionRadius; x <= centerX + ExplosionRadius; x++) {
                    // Skip: don't create an explosion on the epicenter itself. Double damage.
                    if (x == centerX && y == centerY) { continue; }
                    var distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                    if (distance <= ExplosionRadius) {
                        this.EffectEntities.Add(new Explosion(x, y));
                    }
                }
            }
        }


        private int CalculateDamage(char weaponCharacter)
        {
            if (weaponCharacter == '*') {
                return (int)Math.Ceiling(this.CalculateDamage(Weapon.MiniMissile) * 0.75); // 1.5x
            }

            switch (weaponCharacter) {
                case '!': return this.CalculateDamage(Weapon.MiniMissile);
                case '$': return this.CalculateDamage(Weapon.Zapper);
                case 'o': return this.CalculateDamage(Weapon.PlasmaCannon);
                case GravityCannonShot: return this.CalculateDamage(Weapon.GravityCannon);
                case InstaTeleporterShot: return this.CalculateDamage(Weapon.InstaTeleporter);
                default: return 0;
            }
        }
        
        private int CalculateDamage(Weapon weapon)
        {
            switch(weapon) {
                case Weapon.Blaster: return Player.Strength;
                case Weapon.MiniMissile: return Player.Strength * 3;
                case Weapon.Zapper: return Player.Strength * 2;
                case Weapon.PlasmaCannon: return Player.Strength * 4;
                case Weapon.GravityCannon: return Player.Strength * 4;
                case Weapon.InstaTeleporter: return 0;
                default: return -1;
            }
        }

        private Weapon CharacterToWeapon(char display) {
            switch(display) {
                case '+': return Weapon.Blaster;
                case '!': return Weapon.MiniMissile;
                case '$': return Weapon.Zapper;
                case 'o': return Weapon.PlasmaCannon;
                case InstaTeleporterShot: return Weapon.InstaTeleporter;
                case GravityCannonShot: return Weapon.GravityCannon;

                case '*': return Weapon.MiniMissile; // explosion
            }
            throw new InvalidOperationException($"{display} ???");
        }

        private void PlayerTookTurn()
        {
            this.ProcessMonsterTurns();
            
            var deadPlasma = this.PlasmaResidue.Where(p => !p.IsAlive);
            this.PlasmaResidue.RemoveAll(p => deadPlasma.Contains(p));
        }

        private void ProcessMonsterTurns()
        {
            var plasmaBurnedMonsters = new List<Entity>();

            // Eggs' turns create more monsters (modify enumeration during iteration).
            // Just use ToArray here to create a copy.
            foreach (var monster in this.Monsters.Where(m => m.CanMove).ToArray())
            {
                var distance = Math.Sqrt(Math.Pow(Player.X - monster.X, 2) + Math.Pow(Player.Y - monster.Y, 2));

                // Monsters who you can see, or hurt monsters, attack.
                if (!monster.IsDead && (distance <= monster.VisionRange || monster.CurrentHealth < monster.TotalHealth))
                {
                    var spawner = monster as Spawner;
                    if (spawner != null)
                    {
                        var floors = this.GetAdjacentFloors(new GoRogue.Coord(monster.X, monster.Y));
                        if (floors.Any())
                        {
                            var floor = floors.OrderBy(f => random.Next()).First();
                            this.Monsters.Add(Entity.CreateFromTemplate("Egg", floor.X, floor.Y));
                        }
                    }
                    
                    // Process turn.
                    if (distance <= 1)
                    {
                        // ATTACK~!
                        var damage = monster.Strength - Player.Defense;
                        Player.Damage(damage);
                        this.LatestMessage += $" {monster.Name} attacks for {damage} damage!";
                    }
                    else
                    {
                        // Move closer. Naively. Randomly.
                        var dx = Player.X - monster.X;
                        var dy = Player.Y - monster.Y;
                        var tryHorizontallyFirst = this.random.Next(0, 100) <= 50;
                        var moved = false;

                        if (tryHorizontallyFirst && dx != 0)
                        {
                            moved = this.TryToMove(monster, monster.X + Math.Sign(dx), monster.Y);
                        }
                        else
                        {
                            moved = this.TryToMove(monster, monster.X, monster.Y + Math.Sign(dy));
                        }

                        if (moved) {
                            var plasma = this.PlasmaResidue.SingleOrDefault(p => p.X == monster.X && p.Y == monster.Y);
                            if (plasma != null) {
                                // Damaging here may cause the monsters collection to modify while iterating over it
                                plasmaBurnedMonsters.Add(monster);
                                this.PlasmaResidue.Remove(plasma);
                            }
                        }
                    }
                }
            }

            foreach (var monster in plasmaBurnedMonsters) {
                monster.Damage(PlasmaResidueDamage);
                this.LatestMessage = $"{monster.Name} steps on plasma and burns!";
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

                if (entity == Player)
                {
                    Player.OnMove(previousX, previousY);
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
            if (Player.IsDead) {
                if (this.keyboard.IsKeyPressed(Key.Escape))
                {
                    System.Environment.Exit(0);    
                }

                return false; // don't pass time
            }

            if (!Player.CanMove) {
                return false;
            }

            var processedInput = false;

            if (this.keyboard.IsKeyPressed(Key.Escape))
            {
                Environment.Exit(0);
            }
            
            var destinationX = this.Player.X;
            var destinationY = this.Player.Y;
            
            if ((this.keyboard.IsKeyPressed(Key.W) || this.keyboard.IsKeyPressed(Key.Up)))
            {
                destinationY -= 1;
            }
            else if ((this.keyboard.IsKeyPressed(Key.S) || this.keyboard.IsKeyPressed(Key.Down)))
            {
                destinationY += 1;
            }

            if ((this.keyboard.IsKeyPressed(Key.A) || this.keyboard.IsKeyPressed(Key.Left)))
            {
                destinationX -= 1;
            }
            else if ((this.keyboard.IsKeyPressed(Key.D) || this.keyboard.IsKeyPressed(Key.Right)))
            {
                destinationX += 1;
            }
            else if ((this.keyboard.IsKeyPressed(Key.Q)))
            {
                Player.TurnCounterClockwise();
            }
            else if ((this.keyboard.IsKeyPressed(Key.E)))
            {
                Player.TurnClockwise();
            }
            else if (this.keyboard.IsKeyPressed(Key.NumPad1) && Player.Has(Weapon.Blaster))
            {
                Player.CurrentWeapon = Weapon.Blaster;
            }
            else if (this.keyboard.IsKeyPressed(Key.NumPad2) && Player.Has(Weapon.MiniMissile))
            {
                Player.CurrentWeapon = Weapon.MiniMissile;
            }
            else if (this.keyboard.IsKeyPressed(Key.NumPad3) && Player.Has(Weapon.Zapper))
            {
                Player.CurrentWeapon = Weapon.Zapper;
            }
            else if (this.keyboard.IsKeyPressed(Key.NumPad4) && Player.Has(Weapon.PlasmaCannon))
            {
                Player.CurrentWeapon = Weapon.PlasmaCannon;
            }
            else if (this.keyboard.IsKeyPressed(Key.NumPad5) && Player.Has(Weapon.GravityCannon))
            {
                Player.CurrentWeapon = Weapon.GravityCannon;
            }
            else if (this.keyboard.IsKeyPressed(Key.T))
            {
                Player.CurrentWeapon = Weapon.InstaTeleporter;
            }
            else if (this.keyboard.IsKeyPressed(Key.OemPeriod) && (Options.CanUseStairsFromAnywhere || (Player.X == StairsDownLocation.X && Player.Y == StairsDownLocation.Y)))
            {
                Dungeon.Instance.GoToNextFloor();
                destinationX = Player.X;
                destinationY = Player.Y;
            }
            else if (this.floorNum > 0 && this.keyboard.IsKeyPressed(Key.OemComma) && (Options.CanUseStairsFromAnywhere || (Player.X == StairsUpLocation.X && Player.Y == StairsUpLocation.Y)))
            {
                Dungeon.Instance.GoToPreviousFloor();
                destinationX = Player.X;
                destinationY = Player.Y;
            }
            
            if (this.TryToMove(Player, destinationX, destinationY))
            {
                processedInput = true;
                this.OnPlayerMoved();
            }
            else if (this.keyboard.IsKeyPressed(Key.F) && (Player.CurrentWeapon != Weapon.GravityCannon || Player.CanFireGravityCannon))
            {
                // If gravity cannon wasn't fireable, but it's not equipped, make it fireable. This allows us to fire gravity/rocket/gravity/blaster/etc.
                if (Player.CurrentWeapon != Weapon.GravityCannon && !Player.CanFireGravityCannon) {
                    Player.CanFireGravityCannon = true;
                }
                this.FireShot();
            }
            else if (this.Doors.SingleOrDefault(d => d.X == destinationX && d.Y == destinationY && d.IsLocked == false) != null)
            {
                var door = this.Doors.Single(d => d.X == destinationX && d.Y == destinationY && d.IsLocked == false);
                if (!door.IsOpened) {
                    door.IsOpened = true;
                    this.LatestMessage = "You open the door.";
                } else {
                    Player.X = door.X;
                    Player.Y = door.Y;
                }
            }
            else if (this.GetMonsterAt(destinationX, destinationY) != null)
            {
                var monster = this.GetMonsterAt(destinationX, destinationY);
                processedInput = true;

                var damage = Player.Strength - monster.Defense;
                monster.Damage(damage);
                this.LatestMessage = $"You hit {monster.Name} for {damage} damage!";
            }
            else if (this.keyboard.IsKeyPressed(Key.OemPeriod) || this.keyboard.IsKeyPressed(Key.Space))
            {
                // Skip turn
                processedInput = true;
            }

            if (Player.CurrentHealth <= 0)
            {
                this.LatestMessage = "YOU DIE!!!!";
            }

            return processedInput;
        }

        internal void OnPlayerMoved()
        {
            Player.CanFireGravityCannon = true;
            // This is too late - player already moved. For the prototype, we can live with this.
            int viewRadius = (int)Math.Ceiling(Player.VisionRange / 2.0);
            for (var y = Player.Y - viewRadius; y <= Player.Y + viewRadius; y++)
            {
                for (var x = Player.X - viewRadius; x <= Player.X + viewRadius; x++)
                {
                    // Just to be sure
                    if (IsInPlayerFov(x, y))
                    {
                        this.MarkAsSeen(x, y);
                    }
                }
            }

            this.LatestMessage = "";

            // Damaged by plasma residue
            var plasmaUnderPlayer = this.PlasmaResidue.SingleOrDefault(p => p.X == Player.X && p.Y == Player.Y);
            if (plasmaUnderPlayer != null) {
                this.LatestMessage = $"The plasma burns through your suit! {PlasmaResidueDamage} damage!";
                Player.Damage(PlasmaResidueDamage);
                this.PlasmaResidue.Remove(plasmaUnderPlayer);
            }

            this.PlasmaResidue.ForEach(p => p.Degenerate());

            var powerUpUnderPlayer = this.PowerUps.SingleOrDefault(p => p.X == Player.X && p.Y == Player.Y);
            if (powerUpUnderPlayer != null)
            {
                this.PowerUps.Remove(powerUpUnderPlayer);
                Player.Absorb(powerUpUnderPlayer);
                powerUpUnderPlayer.PickUp();
                this.LatestMessage = $"You activate the power-up. {powerUpUnderPlayer.Message}";
            }

            if (this.WeaponPickUp != null && WeaponPickUp.X == Player.X && WeaponPickUp.Y == Player.Y)
            {
                this.Player.Acquire(this.WeaponPickUp.Weapon);
                var key = this.GetKeyFor(this.WeaponPickUp.Weapon);
                var keyText = key.ToString().Replace("NumPad", "");
                this.LatestMessage = $"You assimilate the {this.WeaponPickUp.Weapon}. Press {keyText} to equip it.";
                this.WeaponPickUp = null;
            }
        }

        private Keys GetKeyFor(Weapon weapon)
        {
            switch (weapon) {
                case Weapon.MiniMissile:
                    return Keys.NumPad2;
                case Weapon.Zapper:
                    return Keys.NumPad3;
                case Weapon.GravityCannon:
                    return Keys.NumPad4;
                case Weapon.PlasmaCannon:
                    return Keys.NumPad5;
                case Weapon.InstaTeleporter:
                    return Keys.T;
                default:
                    throw new ArgumentException($"Not sure what the key binding is for {weapon}");
            }
        }

        private void FireShot()
        {
            var character = '+';

            if (Player.CurrentWeapon != Weapon.Zapper) {
                // Blaster: +
                // Missle: !
                // Shock: $
                // Plasma: o
                switch (Player.CurrentWeapon) {
                    case Weapon.Blaster:
                        character = '+';
                        break;
                    case Weapon.MiniMissile:
                        character = '!';
                        break;
                    case Weapon.PlasmaCannon:
                        character = 'o';
                        break;
                    case Weapon.GravityCannon:
                        character = GravityCannonShot;
                        break;
                    case Weapon.InstaTeleporter:
                        character = InstaTeleporterShot;
                        break;
                }

                var dx = 0;
                var dy = 0;

                switch(Player.DirectionFacing) {
                    case Direction.Up: dy = -1; break;
                    case Direction.Down: dy = 1; break;
                    case Direction.Left: dx = -1; break;
                    case Direction.Right: dx = 1; break;
                    default: throw new InvalidOperationException(nameof(Player.DirectionFacing));
                }

                Shot shot;
                if (Player.CurrentWeapon == Weapon.InstaTeleporter) {
                    shot = new TeleporterShot(Player.X, Player.Y, Player.DirectionFacing, this.IsFlyable);
                } else {
                    shot = new Shot(Player.X + dx, Player.Y + dy, character, Palette.Red, Player.DirectionFacing, this.IsFlyable);
                }
                if (character == GravityCannonShot) {
                    Player.CanFireGravityCannon = false;
                }
                EffectEntities.Add(shot);
            }
            else
            {
                // Fires a <- shape in front of you.
                var dx = 0;
                var dy = 0;
                // orthagonal
                var ox = 0;
                var oy = 0;

                character = '$';
                var colour = Palette.Blue;

                switch(Player.DirectionFacing) {
                    case Direction.Up: dy = -1; break;
                    case Direction.Down: dy = 1; break;
                    case Direction.Left: dx = -1; break;
                    case Direction.Right: dx = 1; break;
                    default: throw new InvalidOperationException(nameof(Player.DirectionFacing));
                }

                ox = Player.DirectionFacing == Direction.Up || Player.DirectionFacing == Direction.Down ? 1 : 0;
                oy = Player.DirectionFacing == Direction.Left || Player.DirectionFacing == Direction.Right ? 1 : 0;

                EffectEntities.Add(new Bolt(Player.X + dx, Player.Y + dy));
                EffectEntities.Add(new Bolt(Player.X + 2*dx, Player.Y + 2*dy));
                EffectEntities.Add(new Bolt(Player.X + 3*dx, Player.Y + 3*dy));

                EffectEntities.Add(new Bolt(Player.X + dx + ox, Player.Y + dy + oy));
                EffectEntities.Add(new Bolt(Player.X + 2*dx + 2*ox, Player.Y + 2*dy + 2*oy));
                EffectEntities.Add(new Bolt(Player.X + 3*dx + 3*ox, Player.Y + 3*dy + 3*oy));

                EffectEntities.Add(new Bolt(Player.X + dx - ox, Player.Y + dy - oy));
                EffectEntities.Add(new Bolt(Player.X + 2*dx - 2*ox, Player.Y + 2*dy - 2*oy));
                EffectEntities.Add(new Bolt(Player.X + 3*dx - 3*ox, Player.Y + 3*dy - 3*oy));
            }
            
            this.Player.Freeze();
        }

        private void GenerateMonsters()
        {
            this.Monsters.Clear();

            // floorNum + 1 because B1 is floorNum 0, the dictionary is in B2, B4 ... not 1, 3, ...
            var numFuselings = Options.MonsterMultiplier * this.globalRandom.Next(8, 9); // 8-9 fuselings
            var numSlinks = this.floorNum + 1 >= monsterFloors["slink"] ? Options.MonsterMultiplier * this.globalRandom.Next(3, 5) : 0; // 3-4            
            var numTenLegs = this.floorNum + 1 >= monsterFloors["tenlegs"] ? Options.MonsterMultiplier * this.globalRandom.Next(2, 4) : 0; // 2-3
            var numZugs = this.floorNum + 1 >= monsterFloors["zug"] ? Options.MonsterMultiplier * this.globalRandom.Next(1, 3) : 0; // 1-2

            numFuselings += this.floorNum; // +1 fuseling per floor
            numSlinks += (int)Math.Floor((this.floorNum - monsterFloors["slink"]) / 2f); // +1 slink every other floor (B4, B6, B8, B10)
            numTenLegs += (int)Math.Floor((this.floorNum - monsterFloors["tenlegs"]) / 3f); // +1 tenlegs every third floor (B4, B7, B10)
            numZugs += floorNum >= 8 ? 1 : 0; // +1 zug on floors B9+

            while (numFuselings > 0)
            {
                var spot = this.FindEmptySpot();

                // https://trello.com/c/DNXtSLW5/33-monsters-generate-next-to-player-when-descends-stairs
                // Make sure monsters don't generate right next to the player
                var distanceToStairsUp = Math.Sqrt(Math.Pow(spot.X - StairsUpLocation.X, 2) + Math.Pow(spot.Y - StairsUpLocation.Y, 2));
                var distanceToStairsDown = Math.Sqrt(Math.Pow(spot.X - StairsDownLocation.X, 2) + Math.Pow(spot.Y - StairsDownLocation.Y, 2));

                if (distanceToStairsUp >= MinimumDistanceFromMonsterToStairs && distanceToStairsDown >= MinimumDistanceFromMonsterToStairs)
                {
                    var template = "";
                    if (numZugs > 0) {
                        template = "Zug";
                        numZugs--;
                    } else if (numTenLegs > 0) {
                        template = "TenLegs";
                        numTenLegs--;
                    } else if (numSlinks > 0) {
                        template = "Slink";
                        numSlinks--;
                    } else {
                        template = "Fuseling";
                        numFuselings--;
                    }

                    var monster = Entity.CreateFromTemplate(template, spot.X, spot.Y);
                    this.Monsters.Add(monster);

                    // If it's a slink: look in the 3x3 tiles around/including it, and generate slinks.
                    if (template == "Slink") {
                        var numSubSlinks = this.globalRandom.Next(3, 7); // 3-6 in a bunch
                        var spots = this.GetAdjacentFloors(spot);
                        var spotsToUse = spots.Where(s => IsWalkable(s.X, s.Y)).OrderBy(s => this.globalRandom.Next());
                        
                        var leftInGroup = Math.Min(spots.Count, numSubSlinks);

                        foreach (var slinkSpot in spotsToUse)
                        {
                            distanceToStairsUp = Math.Sqrt(Math.Pow(slinkSpot.X - StairsUpLocation.X, 2) + Math.Pow(slinkSpot.Y - StairsUpLocation.Y, 2));
                            distanceToStairsDown = Math.Sqrt(Math.Pow(slinkSpot.X - StairsDownLocation.X, 2) + Math.Pow(slinkSpot.Y - StairsDownLocation.Y, 2));
                            if (distanceToStairsUp >= MinimumDistanceFromMonsterToStairs && distanceToStairsDown >= MinimumDistanceFromMonsterToStairs)
                            {
                                monster = Entity.CreateFromTemplate(template, slinkSpot.X, slinkSpot.Y);
                                this.Monsters.Add(monster);
                                leftInGroup--;
                            }

                            if (leftInGroup == 0)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        // Finds an empty spot. Secret-room floors are not considered empty.
        private GoRogue.Coord FindEmptySpot()
        {
            int targetX = 0;
            int targetY = 0;
            
            do 
            {
                targetX = this.globalRandom.Next(0, this.width);
                targetY = this.globalRandom.Next(0, this.height);
            } while (!this.IsWalkable(targetX, targetY));

            return new GoRogue.Coord(targetX, targetY);
        }

        private Entity GetMonsterAt(int x, int y)
        {
            // BUG: (secondary?) knockback causes two monsters to occupy the same space!!!
            return this.Monsters.FirstOrDefault(m => m.X == x && m.Y == y);
        }

        // Can a projectile "fly" over a spot? True if empty or a chasm; false if occupied by anything
        // (walls, fake walls, doors, monsters, player, etc.)
        private bool IsFlyable(int x, int y) {
            if (x < 0 || y < 0 || x >= this.width || y >= this.height) {
                return false;
            }

            if (this.Walls.Any(w => w.X == x && w.Y == y))
            {
                return false;
            }

            if (this.FakeWalls.Any(f => f.X == x && f.Y == y))
            {
                return false;
            }

             if (this.Doors.Any(d => d.X == x && d.Y == y && d.IsOpened == false)) {
                return false;
            }

            if (this.GetMonsterAt(x, y) != null)
            {
                return false;
            }

            if (this.Player != null && this.Player.X == x && this.Player.Y == y)
            {
                return false;
            }

            return true;
        }

        private void MarkAsSeen(int x, int y)
        {
            string key = $"{x}, {y}";
            isTileDiscovered[key] = true;
        }
    }

    class ConnectedRoom
    {
        public GoRogue.Rectangle Rectangle { get; set; }
        public bool ConnectedOnLeft {get; set;}
        public Rectangle OriginalRoom { get; }

        public ConnectedRoom(int x, int y, int width, int height, bool connectedOnLeft, Rectangle originalRoom)
        {
            this.Rectangle = new GoRogue.Rectangle(x, y, width, height);
            this.ConnectedOnLeft = connectedOnLeft;
            this.OriginalRoom = originalRoom;
        }
    }
}