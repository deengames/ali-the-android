using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GoRogue.MapViews;
using Troschuetz.Random;
using Troschuetz.Random.Generators;
using DeenGames.AliTheAndroid.Prototype.Enums;
using Global = SadConsole.Global;
using AliTheAndroid.Prototype;
using AliTheAndroid.Enums;
using static DeenGames.AliTheAndroid.Prototype.Shot;
using GoRogue.Pathing;

namespace DeenGames.AliTheAndroid.Prototype
{
    public class PrototypeGameConsole : SadConsole.Console
    {
        public static readonly IGenerator GlobalRandom;

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

        private static readonly int? GameSeed = null; // null = random each time
        private const char GravityCannonShot = (char)246; 
        private const char InstaTeleporterShot = '?';
        private const int MinimumDistanceFromPlayerToStairs = 10; // be more than MaxRoomSize so they're not in the same room
        private const int MinimumChasmDistance = 10;

        private Random random = new Random(); // for gravity perturbances and non-generative random
        
        private GoRogue.Coord stairsLocation = new GoRogue.Coord();
        private readonly Player player;
        private readonly List<Entity> monsters = new List<Entity>();
        private IList<GoRogue.Rectangle> rooms = new List<GoRogue.Rectangle>();
        private readonly List<AbstractEntity> walls = new List<AbstractEntity>();
        private readonly List<AbstractEntity> fakeWalls = new List<AbstractEntity>();
        private readonly List<Door> doors = new List<Door>();
        private readonly List<Effect> effectEntities = new List<Effect>();
        
        private readonly List<Plasma> plasmaResidue = new List<Plasma>();
        private readonly List<AbstractEntity> gravityWaves = new List<AbstractEntity>();
        private readonly List<AbstractEntity> chasms = new  List<AbstractEntity>();

        // Super hack. Key is "x, y", value is IsDiscovered.
        private Dictionary<string, bool> isTileDiscovered = new Dictionary<string, bool>();

        private int currentFloorNum = 0;

        private ArrayMap<bool> map; // Initial map ONLY: no secret rooms, monsters, locked doors, etc. true = walkable

        private readonly int mapHeight;
        private string lastMessage = "";

        private string LatestMessage { 
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
        
        static PrototypeGameConsole() {
            if (!GameSeed.HasValue) {
                GameSeed = new Random().Next();
            }
            
            System.Console.WriteLine($"Universe #{GameSeed.Value}");
            GlobalRandom = new StandardGenerator(GameSeed.Value);
        }

        public PrototypeGameConsole(int width, int height) : base(width, height)
        {
            this.player = new Player();
            this.mapHeight = height - 2;
            this.GenerateMap();

            EventBus.Instance.AddListener(GameEvent.EntityDeath, (e) => {
                if (e == player)
                {
                    this.LatestMessage = "YOU DIE!!!";
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

        private void GenerateGravityWaves()
        {
            this.gravityWaves.Clear();

            // Plot a path from the player to the stairs. Pick one of those rooms in that path, and fill it with gravity.            
            var pathFinder = new AStar(map, GoRogue.Distance.EUCLIDEAN);
            var path = pathFinder.ShortestPath(new GoRogue.Coord(player.X, player.Y), new GoRogue.Coord(stairsLocation.X, stairsLocation.Y), true);
            var playerRoom = this.rooms.SingleOrDefault(r => r.Contains(new GoRogue.Coord(player.X, player.Y)));

            var roomsInPath = new List<GoRogue.Rectangle>();

            foreach (var step in path.StepsWithStart)
            {
                var stepRoom = this.rooms.SingleOrDefault(r => r.Contains(step));
                if (stepRoom != GoRogue.Rectangle.EMPTY && stepRoom != playerRoom && !roomsInPath.Contains(stepRoom)) {
                    roomsInPath.Add(stepRoom);
                }
            }

            // If there are no rooms between us (stairs is in a hallway), we don't generate this room.
            // If there's just one room - the stairs room - it will be full of gravity.
            // If there are two or more, pick one and gravity-fill it.
            GoRogue.Rectangle gravityRoom = Rectangle.Empty;

            if (roomsInPath.Any()) {
                // Guaranteed not to be the player room. If there's only one room between these two, could be the exit room.
                gravityRoom = roomsInPath[GlobalRandom.Next(roomsInPath.Count)];
                this.FillWithGravity(gravityRoom);            
            }

            var extraRooms = ExtraGravityWaveRooms;
            var candidateRooms = rooms.Where(r => r != gravityRoom).ToList();

            while (extraRooms > 0) {
                var nextRoom = candidateRooms[GlobalRandom.Next(candidateRooms.Count)];
                if (!nextRoom.Contains(new GoRogue.Coord(player.X, player.Y))) {
                    this.FillWithGravity(nextRoom);
                    candidateRooms.Remove(nextRoom);
                    extraRooms -= 1;
                }
            }
        }

        private void FillWithGravity(GoRogue.Rectangle room) {
            for (var y = room.MinExtentY; y <= room.MaxExtentY; y++) {
                for (var x = room.MinExtentX; x <= room.MaxExtentX; x++) {
                    this.gravityWaves.Add(new AbstractEntity(x, y, (char)247, Palette.LightLilacPink));
                }
            }
        }

        private void GenerateMap()
        {
            this.currentFloorNum++;
            this.lastMessage = "";
            this.isTileDiscovered.Clear();
            this.plasmaResidue.Clear();

            this.GenerateMapRooms();
            this.GenerateMonsters();

            var emptySpot = this.FindEmptySpot();
            player.X = (int)emptySpot.X;
            player.Y = (int)emptySpot.Y;

            this.GenerateStairs();

            // After setting player coordinates and stairs, because generates path between them
            this.GenerateGravityWaves();
            this.GenerateChasms();

            this.RedrawEverything();
        }

        private void GenerateChasms()
        {
            this.chasms.Clear();
            
            // Pick three/N hallways and fill them with chasms. Make sure they're far from each other.
            var hallwayTiles = new List<GoRogue.Coord>();
            for (var y = 0; y < mapHeight; y++) {
                for (var x = 0; x < this.Width; x++) {
                    // Not 100% accurate since we have monsters, ec.
                    var coordinates = new GoRogue.Coord(x, y);
                    if (IsWalkable(x, y) && !this.rooms.Any(r => r.Contains(coordinates))) {
                        hallwayTiles.Add(coordinates);
                    }
                }
            }

            var numGenerated = 0;
            var iterations = 0;
            var candidates = hallwayTiles.Where(h => this.CountAdjacentFloors(h) == 2).OrderBy(c => GlobalRandom.Next()).ToList();
            
            // Make sure we don't generate chasms too close to each other. This can make hallways impossible to traverse.
            // https://trello.com/c/HxpLSDMt/3-map-generates-a-stuck-map-seed-740970391
            do {
                var candidate = candidates.FirstOrDefault();
                if (candidate != null) {
                    if (!this.chasms.Any()) {
                        this.GenerateChasmAt(candidate);
                        candidates.Remove(candidate);
                        numGenerated++;
                    } else {
                        // Calculate the distance to the closest chasm, and make sure it's distant enough.
                        var minDistance = this.chasms.Select(c => Math.Sqrt(Math.Pow(c.X - candidate.X, 2) + Math.Pow(c.Y - candidate.Y, 2))).Min();
                        if (minDistance >= MinimumChasmDistance) {
                            this.GenerateChasmAt(candidate);
                            candidates.Remove(candidate);
                            numGenerated++;
                        }
                    }
                }
            // Iterations because: hard to tell if we ran out of hallway tiles.
            } while (iterations++ < 1000 && numGenerated < NumChasms);
        }

        private void GenerateChasmAt(GoRogue.Coord location) {
            this.chasms.Add(new AbstractEntity(location.X, location.Y, ' ', Palette.BlackAlmost));
            foreach (var adjacency in this.GetAdjacentFloors(location)) {
                this.chasms.Add(new AbstractEntity(adjacency.X, adjacency.Y, ' ', Palette.BlackAlmost));
            }
        }

        private void GenerateStairs()
        {
            var spot = new GoRogue.Coord(player.X, player.Y);
            var distance = 0d;

            do {
                spot = this.FindEmptySpot();
                distance = Math.Sqrt(Math.Pow(spot.X - player.X, 2)  + Math.Pow(spot.Y - player.Y, 2));
            } while (distance <= MinimumDistanceFromPlayerToStairs);

            this.stairsLocation = spot;
        }

        private void GenerateMapRooms() {
            this.rooms = this.GenerateWalls();
            this.GenerateFakeWallClusters();
            this.GenerateSecretRooms(rooms);
            this.GenerateDoors(rooms);
        }

        private IList<GoRogue.Rectangle> GenerateWalls()
        {
            this.walls.Clear();
            this.map = new ArrayMap<bool>(this.Width, this.mapHeight);
            // true = passable, check GoRogue docs.
            var rooms = GoRogue.MapGeneration.QuickGenerators.GenerateRandomRoomsMap(map, GlobalRandom, MaxRooms, MinRoomSize, MaxRoomSize);
            
            for (var y = 0; y < this.mapHeight; y++) {
                for (var x = 0; x < this.Width; x++) {
                    if (!map[x, y]) {
                        this.walls.Add(new AbstractEntity(x, y, '#', Palette.LightGrey)); // FOV determines colour
                    }
                }
            }

            return rooms.ToList();
        }

        private void GenerateFakeWallClusters()
        {
            this.fakeWalls.Clear();

             // Throw in a few fake walls in random places. Well, as long as that tile doesn't have more than 4 adjacent empty spaces.
            var numFakeWallClusters = 3;
            while (numFakeWallClusters > 0) {
                var spot = this.FindEmptySpot();
                var numFloors = this.CountAdjacentFloors(spot);
                if (numFloors <= 4) {
                    // Make a plus-shaped cluster. It's cooler.
                    this.AddNonDupeEntity(new AbstractEntity((int)spot.X, (int)spot.Y, '#', Palette.LightGrey), this.fakeWalls);
                    this.AddNonDupeEntity(new AbstractEntity((int)spot.X - 1, (int)spot.Y, '#', Palette.LightGrey), this.fakeWalls);
                    this.AddNonDupeEntity(new AbstractEntity((int)spot.X + 1, (int)spot.Y, '#', Palette.LightGrey), this.fakeWalls);
                    this.AddNonDupeEntity(new AbstractEntity((int)spot.X, (int)spot.Y - 1, '#', Palette.LightGrey), this.fakeWalls);
                    this.AddNonDupeEntity(new AbstractEntity((int)spot.X, (int)spot.Y + 1, '#', Palette.LightGrey), this.fakeWalls);
                    numFakeWallClusters -= 1;
                }
            }
        }

        private void GenerateSecretRooms(IEnumerable<GoRogue.Rectangle> rooms)
        {
            var secretRooms = this.FindPotentialSecretRooms(rooms).Take(2);
            foreach (var room in secretRooms) {
                // Fill the interior with fake walls. Otherwise, FOV gets complicated.
                // Trim perimeter by 1 tile so we get an interior only
                for (var y = room.Rectangle.Y + 1; y < room.Rectangle.Y + room.Rectangle.Height - 1; y++) {
                    for (var x = room.Rectangle.X + 1; x < room.Rectangle.X + room.Rectangle.Width - 1; x++) {
                        var wall = this.walls.SingleOrDefault(w => w.X == x && w.Y == y);
                        if (wall != null) {
                            this.walls.Remove(wall);
                        }

                        // Mark as "secret floor" if not perimeter
                        this.fakeWalls.Add(new AbstractEntity(x, y, '#', Palette.Blue));
                    }
                }

                // Hollow out the walls between us and the real room and fill it with fake walls
                var secretX = room.ConnectedOnLeft ? room.Rectangle.X + room.Rectangle.Width - 1 : room.Rectangle.X;
                for (var y = room.Rectangle.Y + 1; y < room.Rectangle.Y + room.Rectangle.Height - 1; y++) {
                    var wall = this.walls.SingleOrDefault(w => w.X == secretX && w.Y == y);
                    if (wall != null) {
                        this.walls.Remove(wall);
                    }

                    this.fakeWalls.Add(new AbstractEntity(secretX, y, '#', Palette.Blue));
                }
            }
        }

        private void GenerateDoors(IEnumerable<GoRogue.Rectangle> rooms) {
            this.doors.Clear();

            // Generate regular doors: any time we have a room, look at the perimeter tiles around that room.
            // If any of them have <= 4 ground tiles (including tiles with doors on them already), add a door.
            foreach (var room in rooms) {
                var startX = room.X;
                var stopX = room.X + room.Width - 1;
                var startY = room.Y;
                var stopY = room.Y + room.Height - 1;

                for (var x = startX; x <= stopX; x++) {
                    if (this.IsDoorCandidate(x, room.Y - 1)) {
                        this.doors.Add(new Door(x, room.Y - 1));
                    }
                    if (this.IsDoorCandidate(x, room.Y + room.Height - 1)) {
                        this.doors.Add(new Door(x, room.Y + room.Height - 1));
                    }
                }

                for (var y = startY; y <= stopY; y++) {
                    if (this.IsDoorCandidate(room.X, y)) {
                        this.doors.Add(new Door(room.X, y));
                    }
                }

                for (var y = startY; y <= stopY; y++) {
                    if (this.IsDoorCandidate(room.X + room.Width - 1, y)) {
                        this.doors.Add(new Door(room.X + room.Width - 1, y));
                    }
                }
            }

            // Generate locked doors: random spots with only two surrounding ground tiles.
            var leftToGenerate = NumberOfLockedDoors;
            while (leftToGenerate > 0) {
                var spot = this.FindEmptySpot();
                var numFloors = this.CountAdjacentFloors(spot);
                if (numFloors == 2) {
                    this.doors.Add(new Door((int)spot.X, (int)spot.Y, true));
                    leftToGenerate--;
                }
            }
        }

        private bool IsDoorCandidate(int x, int y) {
            return this.IsWalkable(x, y) && this.CountAdjacentFloors(new GoRogue.Coord(x, y)) == 4;
        }

        // Only used for generating rock clusters and doors; ignores doors (they're considered walkable)
        private int CountAdjacentFloors(GoRogue.Coord coordinates) {
            return GetAdjacentFloors(coordinates).Count;
        }

        private List<GoRogue.Coord> GetAdjacentFloors(GoRogue.Coord coordinates) {
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
                    if (!this.walls.Any(w => w.X == x && w.Y == y)) {
                        return false;
                    }
                }
            }

            return true;
        }

        public override void Update(System.TimeSpan delta)
        {
            bool playerPressedKey = this.ProcessPlayerInput();

            if (playerPressedKey)
            {
                this.ConsumePlayerTurn();
            }

            if (this.effectEntities.Any()) {
                // Process all effects.
                foreach (var effect in this.effectEntities)
                {
                    effect.OnUpdate();
                    // For out-of-sight effects, accelerate to the point that they destroy.
                    // This prevents the player from waiting, frozen, for out-of-sight shots.
                    if (!this.IsInPlayerFov(effect.X, effect.Y) && !DebugOptions.IsOmnisight) {
                        effect.OnAction();
                    }
                }

                // Harm the player from explosions/zaps.
                var backlashes = this.effectEntities.Where(e => e.Character == '*' || e.Character == '$');
                var playerBacklashes = (backlashes.Where(e => e.X == player.X && e.Y == player.Y));

                foreach (var backlash in playerBacklashes) {
                    var damage = this.CalculateDamage(backlash.Character);
                    Console.WriteLine("Player damaged by backlash for " + damage + " damage!");
                    player.Damage(damage);
                }

                // Unlock doors hit by bolts
                foreach (var bolt in backlashes.Where(b => b.Character == '$')) {
                    foreach (var door in doors.Where(d => d.IsLocked && d.X == bolt.X && d.Y == bolt.Y)) {
                        door.IsLocked = false;
                        this.LatestMessage = "You unlock the door!";
                    }
                }

                // Find and destroy fake walls
                var destroyedFakeWalls = new List<AbstractEntity>();
                this.fakeWalls.ForEach(f => {
                    if (backlashes.Any(e => e.X == f.X && e.Y == f.Y && e.Character == '*')) {
                        destroyedFakeWalls.Add(f);
                    }
                });

                if (destroyedFakeWalls.Any()) {
                    this.LatestMessage = "You discovered a secret room!";
                }
                this.fakeWalls.RemoveAll(e => destroyedFakeWalls.Contains(e));

                // Process if the player shot a plasma shot. 
                var plasmaShot = this.effectEntities.SingleOrDefault(e => e.Character == 'o') as Shot;
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

                        if (!plasmaResidue.Any(f => f.X == previousX && f.Y == previousY))
                        {
                            this.AddNonDupeEntity(new Plasma(previousX, previousY), this.plasmaResidue);
                        }
                    }
                }

                // Destroy any effect that hit something (wall/monster/etc.)
                // Force copy via ToList so we evaluate now. If we evaluate after damage, this is empty on monster kill.
                var destroyedEffects = this.effectEntities.Where((e) => !e.IsAlive || (!(e is TeleporterShot) && !this.IsFlyable(e.X, e.Y))).ToList();
                // If they hit a monster, damage it.
                var harmedMonsters = this.monsters.Where(m => destroyedEffects.Any(e => e.X == m.X && e.Y == m.Y)).ToArray(); // Create copy to prevent concurrent modification exception
                
                foreach (var monster in harmedMonsters) {
                    var hitBy = destroyedEffects.Single(e => e.X == monster.X && e.Y == monster.Y);
                    var type = CharacterToWeapon(hitBy.Character);
                    var damage = CalculateDamage(type);

                    monster.Damage(damage);

                    // Thunder damage hits adjacent monsters. Spawn more bolts~!
                    if (hitBy.Character == '$') {
                        // Crowded areas can cause multiple bolts on the same monster.
                        // This is not intended. A .Single call above will fail.
                        this.AddNonDupeEntity(new Bolt(monster.X - 1, monster.Y), this.effectEntities);
                        this.AddNonDupeEntity(new Bolt(monster.X + 1, monster.Y), this.effectEntities);
                        this.AddNonDupeEntity(new Bolt(monster.X, monster.Y - 1), this.effectEntities);
                        this.AddNonDupeEntity(new Bolt(monster.X, monster.Y + 1), this.effectEntities);
                    }
                }

                // Find destroyed gravity shots and perturb stuff appropriately
                var gravityShot = destroyedEffects.SingleOrDefault(e => e.Character == GravityCannonShot) as Shot;
                if (gravityShot != null) {                    
                    foreach (var monster in this.monsters) {
                        var distance = (int)Math.Ceiling(Math.Sqrt(Math.Pow(monster.X - gravityShot.X, 2) + Math.Pow(monster.Y - gravityShot.Y, 2)));
                        if (distance <= GravityRadius) {
                            int moveBy = GravityRadius - distance;
                            this.ApplyKnockbacks(monster, gravityShot.X, gravityShot.Y, moveBy, gravityShot.Direction);
                        }
                    }

                    var playerDistance = (int)Math.Ceiling(Math.Sqrt(Math.Pow(player.X - gravityShot.X, 2) + Math.Pow(player.Y - gravityShot.Y, 2)));
                    if (playerDistance <= GravityRadius) {
                        int moveBy = GravityRadius - playerDistance;
                        this.ApplyKnockbacks(player, (int)gravityShot.X, (int)gravityShot.Y, moveBy, gravityShot.Direction);
                    }
                }
                
                // Find active gravity shots and destroy rooms full of gravity waves appropriately
                gravityShot = effectEntities.SingleOrDefault(e => e.Character == GravityCannonShot) as Shot;
                if (gravityShot != null) {
                    var room = this.rooms.SingleOrDefault(r => r.Contains(new GoRogue.Coord(gravityShot.X, gravityShot.Y)));
                    if (room != GoRogue.Rectangle.EMPTY) {
                        var waves = this.gravityWaves.Where(g => room.Contains(new GoRogue.Coord(g.X, g.Y)));
                        this.gravityWaves.RemoveAll(w => waves.Contains(w));
                    }
                }

                var teleporterShot = destroyedEffects.SingleOrDefault(s => s.Character == InstaTeleporterShot) as TeleporterShot;
                if (teleporterShot != null) {
                    player.X = teleporterShot.TeleportTo.X;
                    player.Y = teleporterShot.TeleportTo.Y;
                    this.OnPlayerMoved();
                }

                // Missiles explode
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
        
        public void ApplyKnockbacks(Entity entity, int centerX, int centerY, int distance, Direction optionalDirection)
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

        private List<GoRogue.Coord> GetAdjacentFloors(int centerX, int centerY) {
            var toReturn = new List<GoRogue.Coord>();

            for (var y = centerY - 1; y <= centerY + 1; y++) {
                for (var x = centerX - 1; x <= centerX + 1; x++) {
                    if (x != centerX && y != centerY && IsWalkable(x, y))
                    {
                        toReturn.Add(new GoRogue.Coord(x, y));
                    }
                }
            }

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
                        this.effectEntities.Add(new Explosion(x, y));
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
                case Weapon.Blaster: return player.Strength;
                case Weapon.MiniMissile: return player.Strength * 3;
                case Weapon.Zapper: return player.Strength * 2;
                case Weapon.PlasmaCannon: return player.Strength * 4;
                case Weapon.GravityCannon: return player.Strength * 4;
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

        private void ConsumePlayerTurn()
        {
                this.ProcessMonsterTurns();
                this.ProcessGravityPerturbances();
                
                var deadPlasma = this.plasmaResidue.Where(p => !p.IsAlive);
                this.plasmaResidue.RemoveAll(p => deadPlasma.Contains(p));
        }

        private void ProcessGravityPerturbances()
        {
            var perturbedMonsters = this.monsters.Where(m => this.gravityWaves.Any(g => g.X == m.X && g.Y == m.Y));
            foreach (var monster in perturbedMonsters) {
                var moves = this.WhereCanIMove(monster);
                if (moves.Any()) {
                    var move = moves[random.Next(moves.Count)];
                    monster.X = move.X;
                    monster.Y = move.Y;
                }
            }

            if (this.gravityWaves.SingleOrDefault(w => w.X == player.X && w.Y == player.Y) != null) {
                var moves = this.WhereCanIMove(player);
                if (moves.Any()) {
                    var move = moves[random.Next(moves.Count)];
                    player.X = move.X;
                    player.Y = move.Y;
                }
            }
        }

        private List<GoRogue.Coord> WhereCanIMove(Entity e)
        {
            var toReturn = new List<GoRogue.Coord>();

            if (IsWalkable(e.X - 1, e.Y)) { toReturn.Add(new GoRogue.Coord(e.X - 1, e.Y)); }
            if (IsWalkable(e.X +1, e.Y)) { toReturn.Add(new GoRogue.Coord(e.X + 1, e.Y)); }
            if (IsWalkable(e.X, e.Y - 1)) { toReturn.Add(new GoRogue.Coord(e.X, e.Y - 1)); }
            if (IsWalkable(e.X, e.Y + 1)) { toReturn.Add(new GoRogue.Coord(e.X, e.Y + 1)); }

            return toReturn;
        }

        private void ProcessMonsterTurns()
        {
            var plasmaBurnedMonsters = new List<Entity>();

            foreach (var monster in this.monsters.Where(m => m.CanMove))
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
                        this.LatestMessage += $" {monster.Name} attacks for {damage} damage!";
                    }
                    else
                    {
                        // Move closer. Naively. Randomly.
                        var dx = player.X - monster.X;
                        var dy = player.Y - monster.Y;
                        var tryHorizontallyFirst = PrototypeGameConsole.GlobalRandom.Next(0, 100) <= 50;
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
                            var plasma = this.plasmaResidue.SingleOrDefault(p => p.X == monster.X && p.Y == monster.Y);
                            if (plasma != null) {
                                // Damaging here may cause the monsters collection to modify while iterating over it
                                plasmaBurnedMonsters.Add(monster);
                                this.plasmaResidue.Remove(plasma);
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
                player.CurrentWeapon = Weapon.Zapper;
            }
            else if (Global.KeyboardState.IsKeyPressed(Keys.NumPad4))
            {
                player.CurrentWeapon = Weapon.PlasmaCannon;
            }
            else if (Global.KeyboardState.IsKeyPressed(Keys.NumPad5))
            {
                player.CurrentWeapon = Weapon.GravityCannon;
            }
            else if (Global.KeyboardState.IsKeyPressed(Keys.T))
            {
                player.CurrentWeapon = Weapon.InstaTeleporter;
            }
            else if (Global.KeyboardState.IsKeyPressed(Keys.OemPeriod) && player.X == stairsLocation.X && player.Y == stairsLocation.Y)
            {
                this.GenerateMap();
            }
            
            if (this.TryToMove(player, destinationX, destinationY))
            {
                processedInput = true;
                this.OnPlayerMoved();
            }
            else if (Global.KeyboardState.IsKeyPressed(Keys.F) && (player.CurrentWeapon != Weapon.GravityCannon || player.CanFireGravityCannon))
            {
                // If gravity cannon wasn't fireable, but it's not equipped, make it fireable. This allows us to fire gravity/rocket/gravity/blaster/etc.
                if (player.CurrentWeapon != Weapon.GravityCannon && !player.CanFireGravityCannon) {
                    player.CanFireGravityCannon = true;
                }
                this.FireShot();
            }
            else if (this.doors.SingleOrDefault(d => d.X == destinationX && d.Y == destinationY && d.IsLocked == false) != null)
            {
                var door = this.doors.Single(d => d.X == destinationX && d.Y == destinationY && d.IsLocked == false);
                if (!door.IsOpened) {
                    door.IsOpened = true;
                    this.LatestMessage = "You open the door.";
                } else {
                    player.X = door.X;
                    player.Y = door.Y;
                }
            }
            else if (this.GetMonsterAt(destinationX, destinationY) != null)
            {
                var monster = this.GetMonsterAt(destinationX, destinationY);
                processedInput = true;

                var damage = player.Strength - monster.Defense;
                monster.Damage(damage);
                this.LatestMessage = $"You hit {monster.Name} for {damage} damage!";
            }
            else if (Global.KeyboardState.IsKeyPressed(Keys.OemPeriod) || Global.KeyboardState.IsKeyPressed(Keys.Space))
            {
                // Skip turn
                processedInput = true;
            }

            if (player.CurrentHealth <= 0)
            {
                this.LatestMessage = "YOU DIE!!!!";
            }

            return processedInput;
        }

        private void OnPlayerMoved()
        {
            player.CanFireGravityCannon = true;
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

            this.LatestMessage = "";

            // Damaged by plasma residue
            var plasmaUnderPlayer = this.plasmaResidue.SingleOrDefault(p => p.X == player.X && p.Y == player.Y);
            if (plasmaUnderPlayer != null) {
                this.LatestMessage = $"The plasma burns through your suit! {PlasmaResidueDamage} damage!";
                player.Damage(PlasmaResidueDamage);
                this.plasmaResidue.Remove(plasmaUnderPlayer);
            }

            this.plasmaResidue.ForEach(p => p.Degenerate());
        }

        private void FireShot()
        {
            var character = '+';

            if (player.CurrentWeapon != Weapon.Zapper) {
                // Blaster: +
                // Missle: !
                // Shock: $
                // Plasma: o
                switch (player.CurrentWeapon) {
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

                switch(player.DirectionFacing) {
                    case Direction.Up: dy = -1; break;
                    case Direction.Down: dy = 1; break;
                    case Direction.Left: dx = -1; break;
                    case Direction.Right: dx = 1; break;
                    default: throw new InvalidOperationException(nameof(player.DirectionFacing));
                }

                Shot shot;
                if (player.CurrentWeapon == Weapon.InstaTeleporter) {
                    shot = new TeleporterShot(player.X, player.Y, player.DirectionFacing, this.IsFlyable);
                } else {
                    shot = new Shot(player.X + dx, player.Y + dy, character, Palette.Red, player.DirectionFacing, this.IsFlyable);
                }
                if (character == GravityCannonShot) {
                    player.CanFireGravityCannon = false;
                }
                effectEntities.Add(shot);
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

                switch(player.DirectionFacing) {
                    case Direction.Up: dy = -1; break;
                    case Direction.Down: dy = 1; break;
                    case Direction.Left: dx = -1; break;
                    case Direction.Right: dx = 1; break;
                    default: throw new InvalidOperationException(nameof(player.DirectionFacing));
                }

                ox = player.DirectionFacing == Direction.Up || player.DirectionFacing == Direction.Down ? 1 : 0;
                oy = player.DirectionFacing == Direction.Left || player.DirectionFacing == Direction.Right ? 1 : 0;

                effectEntities.Add(new Bolt(player.X + dx, player.Y + dy));
                effectEntities.Add(new Bolt(player.X + 2*dx, player.Y + 2*dy));
                effectEntities.Add(new Bolt(player.X + 3*dx, player.Y + 3*dy));

                effectEntities.Add(new Bolt(player.X + dx + ox, player.Y + dy + oy));
                effectEntities.Add(new Bolt(player.X + 2*dx + 2*ox, player.Y + 2*dy + 2*oy));
                effectEntities.Add(new Bolt(player.X + 3*dx + 3*ox, player.Y + 3*dy + 3*oy));

                effectEntities.Add(new Bolt(player.X + dx - ox, player.Y + dy - oy));
                effectEntities.Add(new Bolt(player.X + 2*dx - 2*ox, player.Y + 2*dy - 2*oy));
                effectEntities.Add(new Bolt(player.X + 3*dx - 3*ox, player.Y + 3*dy - 3*oy));
            }
            
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

            foreach (var residue in this.plasmaResidue) {
                if (IsInPlayerFov(residue.X, residue.Y)) {
                    this.SetGlyph(residue.X, residue.Y, residue.Character, residue.Color);
                }
            }

            var allWalls = this.walls.Union(this.fakeWalls);

            foreach (var wall in allWalls)
            {
                var x = (int)wall.X;
                var y = (int)wall.Y;

                var colour = DebugOptions.ShowFakeWalls && fakeWalls.Contains(wall) ? Palette.Blue : Palette.LightGrey;

                if (IsInPlayerFov(x, y))
                {
                    this.SetGlyph(wall.X, wall.Y, wall.Character, colour);
                }
                else if (IsSeen(x, y))
                {
                  this.SetGlyph(wall.X, wall.Y, wall.Character, colour);
                }
            }

            foreach (var chasm in chasms) {
                if (IsInPlayerFov(chasm.X, chasm.Y)) {
                    this.SetGlyph(chasm.X, chasm.Y, chasm.Character, chasm.Color);
                } else if (IsSeen(chasm.X, chasm.Y)) {
                    this.SetGlyph(chasm.X, chasm.Y, chasm.Character, Palette.Grey);
                }
            }

            
            foreach (var door in doors)
            {
                var x = door.X;
                var y = door.Y;

                if (IsInPlayerFov(x, y))
                {
                    this.SetGlyph(x, y, door.Character, door.Color);
                }
                else if (IsSeen(x, y))
                {
                  this.SetGlyph(x, y, door.Character, Palette.Grey);
                }
            }
            
            foreach (var wave in this.gravityWaves) {
                if (IsInPlayerFov(wave.X, wave.Y)) {
                    this.SetGlyph(wave.X, wave.Y, wave.Character, wave.Color);
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

            int stairsX = (int)this.stairsLocation.X;
            int stairsY = (int)this.stairsLocation.Y;
            if (IsInPlayerFov(stairsX, stairsY) || this.IsSeen(stairsX, stairsY)) {
                this.SetGlyph(stairsX, stairsY, '>', this.IsInPlayerFov(stairsX, stairsY) ? Palette.White : Palette.Grey);
            }

            this.SetGlyph(player.X, player.Y, player.Character, player.Color);

            this.DrawLine(new Point(0, this.Height - 2), new Point(this.Width, this.Height - 2), null, Palette.BlackAlmost, ' ');
            this.DrawLine(new Point(0, this.Height - 1), new Point(this.Width, this.Height - 1), null, Palette.BlackAlmost, ' ');
            this.DrawHealthIndicators();
            this.Print(0, this.Height - 1, this.LatestMessage, Palette.White);
            this.Print(this.Width - 4, this.Height - 2, $"B{this.currentFloorNum}", Palette.White);
        }

        private void DrawHealthIndicators()
        {
            var weaponString = $"{player.CurrentWeapon}";
            if (player.CurrentWeapon == Weapon.GravityCannon && !player.CanFireGravityCannon) {
                weaponString += " (charging)";
            }
            string message = $"You: {player.CurrentHealth}/{player.TotalHealth} (facing {player.DirectionFacing.ToString()}) Equipped: {weaponString}";
            
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
            if (DebugOptions.IsOmnisight) {
                return true;
            }

            // Doesn't use LoS calculations, just simple range check
            var distance = Math.Sqrt(Math.Pow(player.X - x, 2) + Math.Pow(player.Y - y, 2));
            return distance <= player.VisionRange;
        }

        private void GenerateMonsters()
        {
            this.monsters.Clear();

            var numZugs = DebugOptions.MonsterMultiplier * GlobalRandom.Next(1, 3); // 1-2
            var numSlinks = DebugOptions.MonsterMultiplier * GlobalRandom.Next(2, 5); // 2-4            
            var numMonsters = DebugOptions.MonsterMultiplier * GlobalRandom.Next(8, 9); // 8-9 aliens

            while (numMonsters > 0)
            {
                var spot = this.FindEmptySpot();

                var template = "";
                if (numZugs > 0) {
                    template = "Zug";
                    numZugs--;
                } else if (numSlinks > 0) {
                    template = "Slink";
                    numSlinks--;
                } else {
                    template = "Alien";
                    numMonsters--;
                }

                var monster = Entity.CreateFromTemplate(template);
                monster.X = spot.X;
                monster.Y = spot.Y;
                this.monsters.Add(monster);

                // If it's a slink: look in the 3x3 tiles around/including it, and generate slinks.
                if (template == "Slink") {
                    var numSubSlinks = GlobalRandom.Next(3, 7); // 3-6
                    var spots = this.GetAdjacentFloors(spot);
                    var spotsToUse = spots.OrderBy(s => GlobalRandom.Next()).Take(Math.Min(spots.Count, numSubSlinks));

                    foreach (var slinkSpot in spotsToUse) {
                        monster = Entity.CreateFromTemplate(template);
                        monster.X = slinkSpot.X;
                        monster.Y = slinkSpot.Y;
                        this.monsters.Add(monster);
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
                targetX = PrototypeGameConsole.GlobalRandom.Next(0, this.Width);
                targetY = PrototypeGameConsole.GlobalRandom.Next(0, this.mapHeight);
            } while (!this.IsWalkable(targetX, targetY));

            return new GoRogue.Coord(targetX, targetY);
        }

        private Entity GetMonsterAt(int x, int y)
        {
            // BUG: (secondary?) knockback causes two monsters to occupy the same space!!!
            return this.monsters.FirstOrDefault(m => m.X == x && m.Y == y);
        }

        // Can a projectile "fly" over a spot? True if empty or a chasm; false if occupied by anything
        // (walls, fake walls, doors, monsters, player, etc.)
        private bool IsFlyable(int x, int y) {
            if (x < 0 || y < 0 || x >= this.Width || y >= mapHeight) {
                return false;
            }

            if (this.walls.Any(w => w.X == x && w.Y == y))
            {
                return false;
            }

            if (this.fakeWalls.Any(f => f.X == x && f.Y == y))
            {
                return false;
            }

             if (this.doors.Any(d => d.X == x && d.Y == y && d.IsOpened == false)) {
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
        private bool IsWalkable(int x, int y)
        {
            if (this.chasms.Any(c => c.X == x && c.Y == y)) {
                return false;
            }

            return this.IsFlyable(x, y);
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