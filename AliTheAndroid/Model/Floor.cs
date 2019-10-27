using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model.Events;
using DeenGames.AliTheAndroid.Loggers;
using DeenGames.AliTheAndroid.Consoles;
using GoRogue.MapViews;
using GoRogue.Pathing;
using Microsoft.Xna.Framework;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using Troschuetz.Random;
using DeenGames.AliTheAndroid.Consoles.SubConsoleStrategies;
using DeenGames.AliTheAndroid.Accessibility;
using DeenGames.AliTheAndroid.IO;
using Newtonsoft.Json;
using System.IO;
using DeenGames.AliTheAndroid.Infrastructure;
using DeenGames.AliTheAndroid.Helpers;

namespace DeenGames.AliTheAndroid.Model
{
    public class Floor
    {
        internal const int MinimumDistanceFromMonsterToStairs = 3; // Close, but not too close
        internal const int ExplosionRadius = 2;

        private const int MaxRooms = 10;
        // These are exterior sizes (walls included)
        private const int MinRoomSize = 7;
        private const int MaxRoomSize = 10;
        private const int NumberOfLockedDoors = 3;
        private const int PlasmaResidueDamage = 30;
        private const int GravityRadius = 3;
        private const int ExtraGravityWaveRooms = 1;
        private const int NumChasms = 5;
        private const int PairedPowerUpsMaxDistance = 5; // no more than N tiles apart

        private const char BlasterShot = '0';
        private const char GravityCannonShot = (char)246; // ÷
        private const char InstaTeleporterShot = '?';
        private static readonly char[] MissileCharacters = new char[] {
            '`', 'a', 'b', 'c'
        };
        
        private const int MinimumDistanceFromPlayerToStairs = 10; // be more than MaxRoomSize so they're not in the same room
        private const int MinimumChasmDistance = 3;
        private const int MinimumChasmToDoorDistance = 5;
        

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
        
        // Work-around for poor serialization support of self-referencing entities
        // See comments on SerializationTests.SerializeAndDeserializePowerUps.
        // We pair everything here on deserialize.
        public PowerUp[] PairedPowerUps = new PowerUp[0];
        public readonly List<AbstractEntity> QuantumPlasma = new List<AbstractEntity>();

        public Player Player;
        public WeaponPickUp WeaponPickUp = null;
        public DataCube DataCube = null;
        public ShipCore ShipCore = null;
        
        // Internal for unit testing
        internal ArrayMap<bool> Map; // Initial map ONLY: no secret rooms, monsters, locked doors, etc. true = walkable
        
        [JsonProperty]
        internal int FloorNum = 0;

        // width/height are only used during generation, but need to be deserialized. Internal for testability
        [JsonProperty]
        internal int Width = 0;

        [JsonProperty]
        internal int Height = 0;

        private GoRogue.FOV PlayerFieldOfView;

        private IList<GoRogue.Rectangle> rooms = new List<GoRogue.Rectangle>();
        
        // Used for deterministic things like dungeon generation
        private IGenerator globalRandom;
        // Used for non-deterministic things, like monster movement
        private Random random = new Random(); 

        // Super hack. Key is "x, y", value is IsDiscovered.
        [JsonProperty]
        private Dictionary<string, bool> isTileDiscovered = new Dictionary<string, bool>();

        // https://trello.com/c/P4udy0Cn/85-fire-gravity-change-gun-fire-gravity-doesnt-recharge
        // We set CanFireGravityCannon = false, then immediately emit a player-moved event, which
        // resets the value to true. Instead of that, set the value to true the next time the player
        // takes a turn (rests, etc.), not immediatley.
        private bool EnableGravityCannonNextTurn = false;

        private string lastMessage = "";
        private bool justScrolledMessage = false;
        private string leftOverMessage = ""; // Replaced with [more], show next time
        private IKeyboard keyboard;


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

        /// <summary>
        /// Used when deserializing a saved dungeon; stuff is already generated
        /// </summary>
        [JsonConstructor]
        public Floor(int width, int height, int floorNum, Dictionary<string, bool> isTileDiscovered)
        : this(width, height, floorNum)
        {
            this.isTileDiscovered = isTileDiscovered;
        }

        /// <summary>
        /// Common constructor code shared between serializtion and production workflow
        /// </summary>
        public Floor(int width, int height, int floorNum)
        {
            this.Width = width;
            this.Height = height;
            this.FloorNum = floorNum;            
            this.keyboard = DependencyInjection.kernel.Get<IKeyboard>();

            this.PlasmaResidue = new List<Plasma>();

            var eventBus = EventBus.Instance;

            eventBus.AddListener(GameEvent.PlayerTookTurn, (obj) =>
            {
                if (Dungeon.Instance.CurrentFloorNum == this.FloorNum)
                {
                    this.PlayerTookTurn();
                }
            });

            eventBus.AddListener(GameEvent.EntityDeath, (e) =>
            {
                if (Dungeon.Instance.CurrentFloorNum == this.FloorNum)
                {
                    if (e == Player)
                    {
                        AudioManager.Instance.Play("Die");
                        LastGameLogger.Instance.Log($"Player died!!!");
                        this.LatestMessage = $"YOU DIE! Press {Options.KeyBindings[GameAction.OpenMenu]} to return to the title.";
                        this.Player.Character = '%';
                        this.Player.Color = Palette.DarkBurgandyPurple;

                        if (Options.DeleteSaveGameOnDeath && File.Exists(Serializer.SaveGameFileName))
                        {
                            File.Delete(Serializer.SaveGameFileName);
                        }
                    }
                    else
                    {
                        var monster = e as Entity;
                        this.Monsters.Remove(monster);

                        if (monster.Name.Contains("Ameer"))
                        {
                            AudioManager.Instance.Play("AmeerDies");
                            
                            // Trigger end-game data cube
                            this.ShowDataCube(DataCube.EndGameCube);
                        }
                        else
                        {
                            AudioManager.Instance.Play("MonsterDies");
                        }
                    }
                }
            });

            eventBus.AddListener(GameEvent.EggHatched, (e) => {
                if (Dungeon.Instance.CurrentFloorNum == this.FloorNum)
                {
                    var position = (GoRogue.Coord)e;
                    // Remove egg
                    var egg = this.Monsters.SingleOrDefault(m => m.X == position.X && m.Y == position.Y && m is Egg);
                    // Null if it gets killed before it hatches. I removed the event listener in Die() but, alas.
                    if (egg != null)
                    {
                        this.Monsters.Remove(egg);
                        AudioManager.Instance.Play("EggHatches");
                    }

                    // Add monster
                    this.Monsters.Add(Entity.CreateFromTemplate("Fuseling", position.X, position.Y));
                    this.LatestMessage = "An egg hatches into a Fuseling!";
                }
            });

            eventBus.AddListener(GameEvent.AmeerStunned, (obj) => this.LatestMessage = "The ameer roars as bolts surge through his body!" );
        }

        /// <summary>
        /// "Production" workflow: generate a floor and all content
        /// </summary>
        public Floor(int width, int height, int floorNum, IGenerator globalRandom)
        : this(width, height, floorNum)
        {
            this.globalRandom = globalRandom;
            
            this.GenerateMap();
            
            this.PlayerFieldOfView = new GoRogue.FOV(Map);
        }

        public void Update(System.TimeSpan delta)
        {
            bool playerTookTurn = this.ProcessPlayerInput();

            if (playerTookTurn)
            {
                EventBus.Instance.Broadcast(GameEvent.PlayerTookTurn, new PlayerTookTurnData(Player, this.Monsters));
            
                if (ShipCore != null)
                {
                    var distanceToCore = this.DistanceFrom(
                        new GoRogue.Coord(this.Player.X, this.Player.Y), new GoRogue.Coord(this.ShipCore.X, this.ShipCore.Y));

                    if (distanceToCore <= 2)
                    {
                        this.LatestMessage = "The ship core thrums and glows with energy.";
                    }
                }

                this.MentionInterestingAdjacentObjects();
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
                    var source = this.CharacterToWeapon(backlash.Character);
                    Player.Damage(damage, source);
                }

                // Unlock doors hit by bolts
                foreach (var bolt in backlashes.Where(b => b.Character == '$')) {
                    foreach (var door in Doors.Where(d => d.IsLocked && d.X == bolt.X && d.Y == bolt.Y)) {
                        door.IsLocked = false;
                        AudioManager.Instance.Play("UnlockDoor");
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

                // Destroyed fake walls don't block vision.
                // Update FOV by updating map and re-instantiating; there's no other way
                foreach (var f in destroyedFakeWalls)
                {
                    // Make it visible if there's no wall underneath (like in clusters)
                    if (!Walls.Any(w => w.X == f.X && w.Y == f.Y))
                    {
                        Map[f.X, f.Y] = true;
                    }
                }
                this.PlayerFieldOfView = new GoRogue.FOV(this.Map);
                this.RecalculatePlayerFov();
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
                
                if (this.ShipCore != null)
                {
                    var hitCore = destroyedEffects.SingleOrDefault(e => e.X == ShipCore.X && e.Y == ShipCore.Y);
                    if (hitCore != null)
                    {
                        if (hitCore == plasmaShot)
                        {
                            this.LatestMessage = "The core absorbs the plasma, shatters, and erupts in quantum plasma!";
                            AudioManager.Instance.Play("CoreBreaks");
                            this.SpawnQuantumPlasma(this.ShipCore.X, this.ShipCore.Y);
                            this.ShipCore = null;
                        }
                        else
                        {
                            this.LatestMessage = "Energy splays harmlessly across the crystal core.";
                            AudioManager.Instance.Play("CoreAbsorbs");
                        }
                    }
                }

                foreach (var monster in harmedMonsters) {
                    var hitBy = destroyedEffects.Single(e => e.X == monster.X && e.Y == monster.Y);
                    var type = CharacterToWeapon(hitBy.Character);
                    var source = this.CharacterToWeapon(hitBy.Character);
                    var damage = CalculateDamage(type);

                    monster.Damage(damage, source);

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
                        this.OnPlayerMoved();
                    }                    
                }
                
                // Find active gravity shots and destroy rooms full of gravity waves appropriately
                gravityShot = EffectEntities.SingleOrDefault(e => e.Character == GravityCannonShot) as Shot;
                if (gravityShot != null) {
                    var wave = this.GravityWaves.SingleOrDefault(g => g.X == gravityShot.X && g.Y == gravityShot.Y);
                    if (wave != null)
                    {
                        var waves = GravityWaveFinder.FloodFillFind(wave, this.GravityWaves);
                        this.GravityWaves.RemoveAll(w => waves.Contains(w));
                        AudioManager.Instance.Play("DisperseGravity");
                    }
                }

                var teleporterShot = destroyedEffects.SingleOrDefault(s => s.Character == InstaTeleporterShot) as TeleporterShot;
                if (teleporterShot != null && IsWalkable(teleporterShot.TeleportTo.X, teleporterShot.TeleportTo.Y))
                {
                    Player.X = teleporterShot.TeleportTo.X;
                    Player.Y = teleporterShot.TeleportTo.Y;
                    AudioManager.Instance.Play("Teleport");
                    this.OnPlayerMoved();
                }

                // Missiles explode
                var missiles = destroyedEffects.Where(e => MissileCharacters.Contains(e.Character));
                foreach (var missile in missiles) {
                    this.CreateExplosion(missile.X, missile.Y);
                }

                // Trim all dead effects
                this.EffectEntities.RemoveAll(e => destroyedEffects.Contains(e));
            }
            
            if (!this.Player.CanMove && !this.EffectEntities.Any())
            {
                // https://trello.com/c/kMPvlMoB/128-destroying-clusters-leaves-behind-a-black-square
                // After unfreeze, mark all FOV tiles as seen. If we just destroyed some fake walls,
                // not doing this could leave them as unseen even though they're not in the FOV.
                // And since we only operate on newly-seen, it'll leave that black hole in our FOV.
                this.MarkCurrentFovAsSeen();

                this.Player.Unfreeze();
                EventBus.Instance.Broadcast(GameEvent.PlayerTookTurn, new PlayerTookTurnData(Player, this.Monsters));
            }
        }


        public bool IsInPlayerFov(int x, int y)
        {
            if (x < 0 || y < 0 || x >= this.Width || y >= this.Height)
            {
                return false; // Out of bounds = not visible
            }

#pragma warning disable
// VS Code doesn't understand that the last code is reachable if OmniSight is off
            if (Options.EnableOmniSight) {
                return true;
            }
            return PlayerFieldOfView.BooleanFOV[x, y] == true;
#pragma warning restore
        }

        public bool IsSeen(int x, int y)
        {
            string key = $"{x}, {y}";
            return isTileDiscovered.ContainsKey(key);
        }

        public bool IsWalkable(int x, int y, bool considerGravityWalkable = false)
        {
            if (this.Chasms.Any(c => c.X == x && c.Y == y))
            {
                return false;
            }

            return this.IsFlyable(x, y, considerGravityWalkable);
        }

        public void RecalculatePlayerFov()
        {
            // Recalculate FOV
            PlayerFieldOfView.Calculate(Player.X, Player.Y, Player.VisionRange);
        }

        public void MarkCurrentFovAsSeen()
        {
            // https://trello.com/c/neuspyiA/115-start-new-game-walk-around-seen-tiles-stay-black
            // When starting a new game, and maybe descending, the starting area tiles stay black
            // even when you walk around, until you see them the second time.
            
            // Root cause: they're in the FOV but not marked as seen, we only mark newly-seen
            // tiles as seen! #derp #herp #lolwut
            foreach (var tile in PlayerFieldOfView.CurrentFOV)
            {
                this.MarkAsSeen(tile.X, tile.Y);
            }
        }

        public void PairPowerUps()
        {
            // If we know they're supposed to be paired, pair 'em.
            if (this.PairedPowerUps.Any())
            {
                PowerUp.Pair(this.PairedPowerUps[0], this.PairedPowerUps[1]);
            }
            
            foreach (var powerUp in this.PairedPowerUps)
            {
                powerUp.OnPickUp(() => {
                    this.PowerUps.Remove(powerUp);
                    this.PowerUps.Remove(powerUp.PairedTo);
                });
            }

            // https://trello.com/c/zT3IX8nh/147-unpaired-power-ups-again
            // This seems like a new bug: power-ups are paired but OnPickUp is null.
            // I probably broke something and didn't realize it. Delete this kludge.
            foreach (var powerUp in this.PowerUps)
            {
                if (powerUp.PairedTo != null && powerUp.PickUpCallback == null)
                {
                    Action removeBoth = () => {
                        this.PowerUps.Remove(powerUp);
                        this.PowerUps.Remove(powerUp.PairedTo);
                    };

                    powerUp.OnPickUp(removeBoth);
                    powerUp.PairedTo.OnPickUp(removeBoth);
                }
            }

            // Then, if there are two in close proximity (eg. backtracking room),
            // pair those, too. Make sure they're not already paired (during load game).
            var unpaired = this.PowerUps.Where(p => p.PairedTo == null);

            foreach (var p1 in unpaired)
            {
                foreach (var p2 in unpaired)
                {
                    if (p1 != p2 && GoRogue.Distance.EUCLIDEAN.Calculate(p1.X, p1.Y, p2.X, p2.Y) <= PairedPowerUpsMaxDistance)
                    {
                        PowerUp.Pair(p1, p2);
                        Action removeBoth = () => {
                            this.PowerUps.Remove(p1);
                            this.PowerUps.Remove(p2);
                        };
                        p1.OnPickUp(removeBoth);
                        p2.OnPickUp(removeBoth);
                    }
                }
            }
        }

        public void InitializeMapAndFov()
        {
            // ArrayMap is not deserializable and neither is GoRogue.FOV
            // Therefore, reconstruct it.
            // Used for deserialization; overwritten by the regular constructor
            this.Map = new ArrayMap<bool>(this.Width, this.Height);
            // Make everything visible
            for (var y = 0; y < this.Height; y++) 
            {
                for (var x = 0; x < this.Width; x++)
                {
                    this.Map[x, y] = true;
                }
            }

            // Make walls occlude visibility
            this.Walls.ForEach(w => this.Map[w.X, w.Y] = false);
            this.FakeWalls.ForEach(w => this.Map[w.X, w.Y] = false);

            this.PlayerFieldOfView = new GoRogue.FOV(Map);
        }

        public void RecreateSubclassedMonsters()
        {
            // https://trello.com/c/PHKmRlcg/86-after-loading-tenlegs-no-longer-lay-eggs
            // After loading, Spawners no longer spawn any eggs. Why? Dunno. Probably because
            // We add instances of Spawner to a collection of Entity, then serialize, then
            // deserialize; that initial fact that it's a Spawner, seems to be gone. BUT!
            // We can't prove this in a test, because instances of Entity are never Spawners.
            // So, this is all conjecture and experimentation. IT WORKS!
            
            // Ditto for the Ameer: https://trello.com/c/JEDA1zH3/92-ameer-is-killable-if-you-load
            var toRemove = new List<Entity>();
            var toAdd = new List<Entity>();

            foreach (var monster in this.Monsters)
            {
                if (monster.Name == "TenLegs")
                {
                    toRemove.Add(monster);
                    var replacement = Entity.CreateFromTemplate("TenLegs", monster.X, monster.Y);
                    replacement.CurrentHealth = monster.CurrentHealth;
                    toAdd.Add(replacement);
                }
                else if (monster.Name.Contains("Ameer"))
                {
                    toRemove.Add(monster);
                    var replacement = new Ameer();
                    replacement.X = monster.X;
                    replacement.Y = monster.Y;
                    // Can't copy over number of turns stunned, since that information is lost on serialization
                    
                    toAdd.Add(replacement);
                }
                else if (monster.Name.Contains("Egg"))
                {
                    toRemove.Add(monster);
                    var replacement = Entity.CreateFromTemplate("Egg", monster.X, monster.Y);
                    replacement.CurrentHealth = monster.CurrentHealth;
                    toAdd.Add(replacement);
                }
            }

            foreach (var monster in toRemove)
            {
                this.Monsters.Remove(monster);
            }

            foreach (var monster in toAdd)
            {
                this.Monsters.Add(monster);
            }
        }
        
        // Only used for generating rock clusters and doors; ignores doors (they're considered walkable)
        internal int CountAdjacentFloors(GoRogue.Coord coordinates) {
            return GetAdjacentFloors(coordinates).Count;
        }

        internal List<GoRogue.Coord> GetAdjacentFloors(GoRogue.Coord coordinates, bool isGravityWalkable = false)
        {
            return this.GetAdjacentFloors(coordinates.X, coordinates.Y, isGravityWalkable);
        }

        internal List<GoRogue.Coord> GetAdjacentFloors(int centerX, int centerY, bool isGravityWalkable = false)
        {
            var toReturn = new List<GoRogue.Coord>();

            for (var y = centerY - 1; y <= centerY + 1; y++) {
                for (var x = centerX - 1; x <= centerX + 1; x++) {
                    if (IsWalkable(x, y) || (isGravityWalkable && IsWalkable(x, y, true)))
                    {
                        toReturn.Add(new GoRogue.Coord(x, y));
                    }
                }
            }

            toReturn.Remove(toReturn.Find(c => c.X == centerX && c.Y == centerY));
            return toReturn;
        }

        
        /// For when the player ACTUALLY MOVED, like changed squares. For non-post-move-turn things (like after firing), see PlayerTookTurn.
        internal void OnPlayerMoved()
        {
            this.RecalculatePlayerFov();

            foreach (var newlySeen in this.PlayerFieldOfView.NewlySeen)
            {
                this.MarkAsSeen(newlySeen.X, newlySeen.Y);
            }

            this.Player.RegenerateShield(this.PlayerFieldOfView, this.Monsters);

            this.LatestMessage = "";

            // Damaged by plasma residue
            var plasmaUnderPlayer = this.PlasmaResidue.SingleOrDefault(p => p.X == Player.X && p.Y == Player.Y);
            if (plasmaUnderPlayer != null) {
                this.LatestMessage = $"The plasma burns through your suit! {PlasmaResidueDamage} damage!";
                Player.Damage(PlasmaResidueDamage, Weapon.Undefined);
                this.PlasmaResidue.Remove(plasmaUnderPlayer);
            }

            var powerUpUnderPlayer = this.PowerUps.SingleOrDefault(p => p.X == Player.X && p.Y == Player.Y);
            if (powerUpUnderPlayer != null)
            {
                this.PowerUps.Remove(powerUpUnderPlayer);
                Player.Absorb(powerUpUnderPlayer);
                AudioManager.Instance.Play("PowerUp");
                powerUpUnderPlayer.PickUp();

                //  Heal if it's a health power-up
                if (powerUpUnderPlayer.HealthBoost > 0)
                {
                    Player.CurrentHealth = Player.TotalHealth;
                }

                this.LatestMessage = $"You activate the power-up. {powerUpUnderPlayer.Message}. (Game saved)";
                SaveManager.SaveGame();
            }

            if (this.WeaponPickUp != null && WeaponPickUp.X == Player.X && WeaponPickUp.Y == Player.Y)
            {
                var weaponType = this.WeaponPickUp.Weapon;
                this.Player.Acquire(weaponType);
                AudioManager.Instance.Play("PickUpWeapon");

                var key = this.GetKeyFor(weaponType);
                var keyText = key.ToString().Replace("NumPad", "");
                var weaponInfo = DeenGames.AliTheAndroid.Model.Entities.Player.WeaponPickupMessages[weaponType];
                this.LatestMessage = $"You assimilate the {weaponType}. Press {keyText} to equip it. {weaponInfo} (Game saved)";
                this.WeaponPickUp = null;
                SaveManager.SaveGame();
            }

            if (this.DataCube != null && DataCube.X == Player.X && DataCube.Y == Player.Y)
            {
                this.Player.GotDataCube(this.DataCube);
                this.ShowDataCube(this.DataCube);
                AudioManager.Instance.Play("PickUpDataCube");

                this.LatestMessage = $"You find a data cube titled '{this.DataCube.Title}.' (Game saved)";
                this.DataCube = null;
                SaveManager.SaveGame();
            }

            if (Player.X == StairsDownLocation.X && Player.Y == StairsDownLocation.Y)
            {
                this.LatestMessage = $"Press {Options.KeyBindings[GameAction.DescendStairs]} to descend to the next floor.";
            }
            else if (Player.X == StairsUpLocation.X && Player.Y == StairsUpLocation.Y)
            {
                this.LatestMessage = $"Press {Options.KeyBindings[GameAction.DescendStairs]} to ascend to the previous floor.";
            }
        }

        internal void CreateExplosion(int centerX, int centerY)
        {
            for (var y = centerY - ExplosionRadius; y <= centerY + ExplosionRadius; y++)
            {
                for (var x = centerX - ExplosionRadius; x <= centerX + ExplosionRadius; x++)
                {
                    var distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                    if (distance <= ExplosionRadius)
                    {
                        this.EffectEntities.Add(new Explosion(x, y));
                    }
                }
            }
            AudioManager.Instance.Play("Explosion");
        }

        

        private void GeneratePowerUps()
        {
            var floorsNearStairs = this.GetAdjacentFloors(StairsDownLocation, true);
            if (floorsNearStairs.Count < 2)
            {
                // No nearby floors? Look harder. This happens when you generate a floor with seed=1234
                var aboveStairs = new GoRogue.Coord(StairsDownLocation.X, StairsDownLocation.Y - 1);
                var belowStairs = new GoRogue.Coord(StairsDownLocation.X, StairsDownLocation.Y + 1);
                var leftOfStairs = new GoRogue.Coord(StairsDownLocation.X - 1, StairsDownLocation.Y);
                var rightOfStairs = new GoRogue.Coord(StairsDownLocation.X + 1, StairsDownLocation.Y);

                var moreTiles = this.GetAdjacentFloors(aboveStairs, true);
                moreTiles.AddRange(this.GetAdjacentFloors(belowStairs, true));
                moreTiles.AddRange(this.GetAdjacentFloors(leftOfStairs, true));
                moreTiles.AddRange(this.GetAdjacentFloors(rightOfStairs, true));

                floorsNearStairs = moreTiles.Where(f => this.IsWalkable(f.X, f.Y, true)).ToList();
            }

            if (!floorsNearStairs.Any() || floorsNearStairs.Count < 2)
            {
                throw new InvalidOperationException($"Can't generate power-ups on B{this.FloorNum + 1}");
            }

            // Use Distinct here because we may get duplicate floors (probably if we have only <= 2 tiles next to stairs)
            // https://trello.com/c/Cp7V5SWW/43-dungeon-generates-with-two-power-ups-on-the-same-spot
            var locations = floorsNearStairs.Distinct().OrderBy(f => globalRandom.Next()).Take(2).ToArray();
            var firstPowerUp = PowerUp.Generate(globalRandom);
            var secondPowerUp = PowerUp.Generate(globalRandom);

            // Make sure they're different types. Same message = same type
            while (secondPowerUp.Message == firstPowerUp.Message)
            {
                secondPowerUp = PowerUp.Generate(globalRandom);
            }
            var powerUps = new List<PowerUp>() { firstPowerUp, secondPowerUp };

            for (var i = 0; i < locations.Count(); i++)
            {
                var powerUp = powerUps[i];
                var location = locations[i];

                powerUp.X = location.X;
                powerUp.Y = location.Y;

                this.PowerUps.Add(powerUp);
            }

            this.PairedPowerUps = powerUps.ToArray();
            this.PairPowerUps();            
        }

        private void SpawnQuantumPlasma(int x, int y)
        {
            if (x >= 0 && x < this.Width && y >= 0 && y < Height && !this.Walls.Any(w => w.X == x && w.Y == y))
            {
                var plasma = AbstractEntity.Create(SimpleEntity.QuantumPlasma, x, y);
                this.AddNonDupeEntity(plasma, this.QuantumPlasma);
            }
        }

        // Get the set of tiles spanning a path from the stairs up to the stairs down. Get all rooms that encompass those tiles.
        private List<GoRogue.Rectangle> RoomsInPathFromStairsToStairs()
        {
            // Plot a path from the player to the stairs. Pick one of those rooms in that path, and fill it with gravity.            
            var pathFinder = new AStar(Map, GoRogue.Distance.EUCLIDEAN);
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
            var candidateRooms = rooms.Where(r => r != gravityRoom && !r.Contains(stairsUpCoordinates) && !roomsInPath.Contains(r)).ToList();

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
            // Use +1 and < to NOT cover the walls/doors with gravity waves.
            for (var y = room.MinExtentY + 1; y < room.MaxExtentY; y++)
            {
                for (var x = room.MinExtentX + 1; x < room.MaxExtentX; x++)
                {
                    AddNonDupeEntity(new GravityWave(x, y, isBacktrackingWave, this.FloorNum), this.GravityWaves);
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

            var pathFinder = new AStar(Map, GoRogue.Distance.EUCLIDEAN);
            var path = pathFinder.ShortestPath(StairsUpLocation, StairsDownLocation, true);

            this.GenerateFakeWallClusters();

            var actualFloorNum = this.FloorNum + 1;
            if (actualFloorNum >= weaponPickUpFloors[Weapon.MiniMissile])
            {
                // Add one more fake wall cluster between the player and the stairs down.
                var middle = globalRandom.Next((int)(path.Length * 0.25), (int)(path.Length * 0.75));
                var midPath = path.GetStep(middle);
                this.CreateFakeWallClusterAt(midPath);
            }

            if (actualFloorNum >= weaponPickUpFloors[Weapon.GravityCannon])
            {
                this.GenerateGravityWaves();
            }
            
            if (actualFloorNum > weaponPickUpFloors[Weapon.InstaTeleporter])
            {
                this.GenerateChasms();
            }

            this.GenerateBacktrackingObstacles();

            if (actualFloorNum == Dungeon.NumFloors)
            {
                this.GenerateBoss();
            }

            // Appropriately, remove stairs here, after we no longer need it for path-finding
            if (actualFloorNum == Dungeon.NumFloors)
            {
                this.StairsDownLocation = GoRogue.Coord.NONE;
            }

            // If we're on a weapon floor, lock the stairs with that weapon.
            if (weaponPickUpFloors.Values.Contains(actualFloorNum) && actualFloorNum != weaponPickUpFloors[Weapon.PlasmaCannon])
            {
                this.SurroundStairsWithRelevantObstacle();
            }

            if (actualFloorNum < Dungeon.NumFloors)
            {
                this.GeneratePowerUps();
                this.GenerateWeaponPickUp();
                this.GenerateDataCube();
            }

            this.GenerateShipCore();
            this.GenerateMonsters();

            // Hack for bug: some rooms are like pocket dimensions for light
            // See: https://trello.com/c/ZcMhxYPo/119-secret-rooms-arent-flooded-with-fake-walls
            // Since save/load works, just use that workflow's treatment of the FOV map
            this.InitializeMapAndFov();
        }
        
        private void SurroundStairsWithRelevantObstacle()
        {
            int actualFloorNum = this.FloorNum + 1;
            var data = weaponPickUpFloors.Single(kvp => kvp.Value == actualFloorNum);
            var weapon = data.Key;

            if (weapon == Weapon.GravityCannon)
            {
                // Special case: just flood the room with gravity
                var room = this.rooms.Single(r => r.Contains(this.StairsDownLocation));
                this.FillWithGravity(room);
                return;
            }

            Action<int, int> obstacleCreator;
            switch (weapon) {
                case Weapon.MiniMissile: obstacleCreator = (x, y) =>  
                {
                    var fakeWall = new FakeWall(x, y);
                    FakeWalls.Add(fakeWall);
                };                
                break;
                case Weapon.Zapper: obstacleCreator = (x, y) =>
                {
                    var door = new Door(x, y, true);
                    Doors.Add(door);
                };
                break;
                case Weapon.InstaTeleporter: obstacleCreator = (x, y) => 
                {
                    var chasm = Entity.Create(SimpleEntity.Chasm, x, y);
                    Chasms.Add(chasm);
                };
                break;
                default:
                    throw new InvalidOperationException($"Tried to generate obstacle for {weapon}!");
            }

            for (var y = this.StairsDownLocation.Y - 1; y <= this.StairsDownLocation.Y + 1; y++)
            {
                for (var x = this.StairsDownLocation.X - 1; x <= this.StairsDownLocation.X + 1; x++)
                {
                    if (x >= 0 && x < this.Width && y >= 0 && y < this.Height && new GoRogue.Coord(x, y) != StairsDownLocation)
                    {
                        obstacleCreator.Invoke(x, y);
                    }
                }
            }
        }

        private void GenerateShipCore()
        {
            var actualFloorNumber = this.FloorNum + 1; // 0 => B1, 8 => B9
            if (actualFloorNumber == 10)
            {
                // Find the room whose center is closest to the map center. NOT the stairs-up room!
                var mapCenter = new GoRogue.Coord(this.Width / 2, this.Height / 2);
                var roomsWithoutStairs = this.rooms.Where(r => !r.Contains(StairsUpLocation)).ToArray();

                var closestRoom = roomsWithoutStairs[0];
                var closestDistance = DistanceFrom(mapCenter, closestRoom.Center);

                foreach (var room in roomsWithoutStairs)
                {
                    var distance = DistanceFrom(room.Center, mapCenter);
                    if (distance < closestDistance)
                    {
                        closestRoom = room;
                        closestDistance = distance;
                    }
                }

                var location = closestRoom.Center;            
                this.ShipCore = new ShipCore(location.X, location.Y);
                
                // Surround with (fake and real) walls
                for (var y = location.Y - 1; y <= location.Y + 1; y++)
                {
                    for (var x = location.X - 1; x <= location.X + 1; x++)
                    {
                        if (x != location.X && y != location.Y && IsWalkable(x, y, true))
                        {
                            this.FakeWalls.Add(new FakeWall(x, y));
                        }
                    }
                }
            }
        }

        
        private void MentionInterestingAdjacentObjects()
        {
            if (this.Doors.Any(d => d.IsLocked && GoRogue.Distance.EUCLIDEAN.Calculate(d.X, d.Y, Player.X, Player.Y) <= 1))
            {
                this.LatestMessage += " This door looks jammed.";
            }
            if (this.FakeWalls.Any(f => GoRogue.Distance.EUCLIDEAN.Calculate(f.X, f.Y, Player.X, Player.Y) <= 1))
            {
                this.LatestMessage += " This wall looks cracked.";
            }
            if (this.Chasms.Any(c => GoRogue.Distance.EUCLIDEAN.Calculate(c.X, c.Y, Player.X, Player.Y) <= 1))
            {
                this.LatestMessage += " A chasm stretches down into darkness.";
            }
        }

        private double DistanceFrom(GoRogue.Coord c1, GoRogue.Coord c2)
        {
            return Math.Sqrt(Math.Pow(c1.X - c2.X, 2) + Math.Pow(c1.Y - c2.Y, 2));
        }

        private void GenerateDataCube()
        {
            var actualFloorNumber = this.FloorNum + 1; // 0 => B1, 8 => B9
            if (actualFloorNumber >= 2 && actualFloorNumber <= 9)
            {
                var spot = this.FindEmptySpot(true);
                this.DataCube = DataCube.GetCube(actualFloorNumber, spot);
            }
        }

        // Generates things out-of-depth (eg. fake walls before the missile launcher pick-up or gaps before the teleporter pick-up).
        // Each one generates just one floor back, for simplicity and user experience (backtracking 2-4 floors is painful).
        private void GenerateBacktrackingObstacles()
        {            
            var actualFloorNumber = this.FloorNum + 1; // 0 => B1, 8 => B9
            GoRogue.Rectangle room;

            if (actualFloorNumber == weaponPickUpFloors[Weapon.MiniMissile] - 1)
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
                // https://trello.com/c/MDHAEkAz/139-backtracking-plasma-rooms-should-be-full-to-the-edges
                // FillRoomWith doesn't fill up to the edges, because isolated rooms are returned as just
                // interior values. The rest of these cases work fine, so just cludge-fix this.
                for (var y = nonCriticalRoom.MinExtentY; y <= nonCriticalRoom.MaxExtentY; y++)
                {
                    for (var x = nonCriticalRoom.MinExtentX; x <= nonCriticalRoom.MaxExtentX; x++)
                    {
                        this.GravityWaves.Add(new GravityWave(x, y, true, this.FloorNum));
                    }
                }
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
                        powerups.Add(new PowerUp(0, room.Center.Y, PowerUp.Characters["Health"], true, healthBoost: PowerUp.TypicalHealthBoost));
                        break;
                     case "strength":
                        powerups.Add(new PowerUp(0, room.Center.Y, PowerUp.Characters["Strength"], true, strengthBoost: PowerUp.TypicalStrengthBoost));
                        break;
                     case "defense":
                        powerups.Add(new PowerUp(0, room.Center.Y, PowerUp.Characters["Defense"], true, defenseBoost: PowerUp.TypicalDefenseBoost));
                        break;
                     case "vision":
                        powerups.Add(new PowerUp(0, room.Center.Y, PowerUp.Characters["Vision"], true, visionBoost: PowerUp.TypicalVisionBoost));
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
            var startSpot = new GoRogue.Coord(globalRandom.Next(this.Width), globalRandom.Next(this.Height));
            while (!this.IsWallRegion(startSpot, width, height))
            {
                startSpot = new GoRogue.Coord(globalRandom.Next(this.Width), globalRandom.Next(this.Height));
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

            // Allow the FOV to penetrate this room - it's not a wall any more.
            for (int y = toReturn.MinExtentY; y <= toReturn.MaxExtentY; y++)
            {
                for (var x = toReturn.MinExtentX; x <= toReturn.MaxExtentX; x++)
                {
                    this.Map[x, y] = true;
                }
            }
            
            toReturn = new GoRogue.Rectangle(toReturn.X, toReturn.Y, width, height);
            return toReturn;
        }

        private void DigTunnel(int startX, int startY, int stopX, int stopY, bool destroyObstacles = false)
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

                    // We're fixing broken level generation by tunneling from the stairs to the nearest
                    // room, thus clearing space for a weapon drop. DESTROY ANYTHING IN OUR WAY.
                    if (destroyObstacles)
                    {
                        var fakeWall = this.FakeWalls.SingleOrDefault(f => f.X == x && f.Y == y);
                        if (fakeWall != null)
                        {
                            this.FakeWalls.Remove(fakeWall);
                        }

                        var lockedDoor = this.Doors.SingleOrDefault(d => d.X == x && d.Y == y && d.IsLocked);
                        if (lockedDoor != null)
                        {
                            this.Doors.Remove(lockedDoor);
                        }

                        var chasm = this.Chasms.SingleOrDefault(c => c.X == x && c.Y == y);
                        if (chasm != null)
                        {
                            this.Chasms.Remove(chasm);
                        }
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
            var actualFloorNumber = this.FloorNum + 1; // 0 => B1, 8 => B9
            var weaponFloorNumbers = weaponPickUpFloors.Values;

            if (weaponFloorNumbers.Contains(actualFloorNumber))
            {
                var weaponType = weaponPickUpFloors.Single(w => w.Value == actualFloorNumber).Key;
                
                var floorTiles = this.GetTilesAccessibleFromStairsIfCollectedAllWeaponsSoFar();
                if (!floorTiles.Any())
                {
                    // This happens in lots of cases where generation goes bad. It's easy to do
                    // things like "don't generate X on the path from stairs to stairs," but then,
                    // that makes the game predictable (obstacle = not the way to the stairs).
                    
                    // So instead, burrow a hole to the nearest room from here.
                    // Among other bugs, see the following:
                    // https://trello.com/c/BSf21Zdr/143-fix-crash-bugs
                    // https://trello.com/c/eL0eQYg7/140-youre-trapped-on-b8
                    // https://trello.com/c/BYFu7sGD/131-dungeon-generation-crashes
                    var closestRoom = this.rooms[0];
                    var shortestDistance = GoRogue.Distance.EUCLIDEAN.Calculate(this.StairsUpLocation.X, this.StairsUpLocation.Y, closestRoom.Center.X, closestRoom.Center.Y);
                    foreach (var room in this.rooms)
                    {
                        var distance = GoRogue.Distance.EUCLIDEAN.Calculate(this.StairsUpLocation.X, this.StairsUpLocation.Y, closestRoom.Center.X, closestRoom.Center.Y);
                        if (distance < shortestDistance)
                        {
                            shortestDistance = distance;
                            closestRoom = room;
                        }
                    }
                    
                    // When digging, show no mercy; destroy ALL obstacles in our way.
                    this.DigTunnel(this.StairsUpLocation.X, this.StairsUpLocation.Y, closestRoom.Center.X, closestRoom.Center.Y, true);

                    floorTiles = this.GetTilesAccessibleFromStairsIfCollectedAllWeaponsSoFar();
                }

                if (!floorTiles.Any())
                {
                    return;
                }

                var target = floorTiles.OrderByDescending(c => 
                    // Order by farthest to closest (distance to stairs)
                    Math.Sqrt(Math.Pow(c.X - this.StairsUpLocation.X, 2) + Math.Pow(c.Y - this.StairsUpLocation.Y, 2)))
                    // Make sure it's not covered
                    .Where(t => !this.FakeWalls.Any(f => t.X == f.X && t.Y == f.Y))
                    // Pick randomly from the first 10
                    .Take(10).OrderBy(c => globalRandom.Next()).First();

                this.WeaponPickUp = new WeaponPickUp(target.X, target.Y, weaponType);
            }
        }

        // Start at the stairs-up. Flood fill floor tiles. Return the floor tiles that you can reach, without
        // wandering through locked doors, gravity waves, fake walls, or across chasms.
        // Excludes the stairs themselves. We don't want to spawn things there.
        private List<GoRogue.Coord> GetTilesAccessibleFromStairsIfCollectedAllWeaponsSoFar()
        {
            var actualFloorNum = this.FloorNum + 1;

            var toExplore = new List<GoRogue.Coord>();
            var explored = new List<GoRogue.Coord>();
            var reachable = new List<GoRogue.Coord>();

            toExplore.Add(this.StairsUpLocation);

            while (toExplore.Any())
            {
                var check = toExplore.First();
                toExplore.Remove(check);
                explored.Add(check);

                // Explores, stopping when it sees walls, locked doors, chasms, and gravity.
                // That is, assuming you're on that floor and the weapon is on that or a future floor.
                var isReachable = check.X >= 0 && check.X < this.Width && check.Y >= 0 && check.Y < this.Height
                    && !Walls.Any(w => w.X == check.X && w.Y == check.Y);

                if (actualFloorNum <= weaponPickUpFloors[Weapon.MiniMissile])
                {
                    isReachable &= !FakeWalls.Any(w => w.X == check.X && w.Y == check.Y);
                }
                    
                if (actualFloorNum <= weaponPickUpFloors[Weapon.Zapper])
                {
                    isReachable &= !Doors.Any(d => d.IsLocked && d.X == check.X && d.Y == check.Y);
                }

                if (actualFloorNum <= weaponPickUpFloors[Weapon.GravityCannon])
                {
                    isReachable &= !GravityWaves.Any(g => g.X == check.X && g.Y == check.Y);
                }

                if (actualFloorNum <= weaponPickUpFloors[Weapon.InstaTeleporter])
                {
                    isReachable &= !Chasms.Any(c => c.X == check.X && c.Y == check.Y);
                }

                if (isReachable)
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
            }

            reachable.Remove(this.StairsUpLocation);
            return reachable;
        }

        private void GenerateBoss()
        {
            var actualFloorNum = this.FloorNum + 1;
            if (actualFloorNum == 10) // 10 = B10
            {
                var bossLocation = this.StairsDownLocation;
                var boss = new Ameer();
                this.Monsters.Add(boss);
                boss.X = bossLocation.X;
                boss.Y = bossLocation.Y;
            }
        }

        private void GenerateChasms()
        {
            this.Chasms.Clear();
            
            // Pick hallways and fill them with chasms. Make sure they're far from each other.
            var hallwayTiles = new List<GoRogue.Coord>();
            for (var y = 0; y < this.Height; y++)
            {
                for (var x = 0; x < this.Width; x++)
                {
                    // Not 100% accurate since we have monsters, etc.
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
            while (iterations++ < 10000 && this.Chasms.Count < NumChasms)
            {
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
                this.GenerateChasmIfNotTooClose(spot);
            }
        }

        private bool GenerateChasmIfNotTooClose(GoRogue.Coord spot)
        {
            // Calculate the distance to the closest chasm, and make sure it's distant enough.
            var minDistanceToOtherChasms = this.Chasms.Any() ? this.Chasms.Select(c => Math.Sqrt(Math.Pow(c.X - spot.X, 2) + Math.Pow(c.Y - spot.Y, 2))).Min() : int.MaxValue;
            // Make sure we're not near any doors, either.
            var minDistanceToDoors = this.Doors.Select(d => Math.Sqrt(Math.Pow(d.X - spot.X, 2) + Math.Pow(d.Y - spot.Y, 2))).Min();
            if (minDistanceToOtherChasms >= MinimumChasmDistance && minDistanceToDoors >= MinimumChasmToDoorDistance)
            {
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
                var closestDoorDistance = this.Doors.Min(d => GoRogue.Distance.EUCLIDEAN.Calculate(adjacency.X, adjacency.Y, d.X, d.Y));
                if (adjacency != StairsUpLocation && adjacency != StairsDownLocation && closestDoorDistance > 1)
                {
                    this.Chasms.Add(AbstractEntity.Create(SimpleEntity.Chasm, adjacency.X, adjacency.Y));
                }
            }
        }

        private GoRogue.Rectangle PickRandomRoom()
        {
            return this.rooms.OrderBy(r => globalRandom.Next()).First();
        }

        private void GenerateStairs()
        {
            // Stairs up generate under the player. Available on all floors (start location), but only usable/visible on floor > 0.
            var stairsUpRoom = this.PickRandomRoom();
            this.StairsUpLocation = stairsUpRoom.Center;

            // Stairs down generate far from the player.
            var room = stairsUpRoom;
            var distance = 0d;

            do {
                room = this.PickRandomRoom();
                var spot = room.Center;
                distance = Math.Sqrt(Math.Pow(spot.X - StairsUpLocation.X, 2)  + Math.Pow(spot.Y - StairsUpLocation.Y, 2));
            } while (distance <= MinimumDistanceFromPlayerToStairs);

            this.StairsDownLocation = room.Center;
        }


        private void GenerateMapRooms()
        {
            var actualFloorNum = this.FloorNum + 1;

            this.rooms = this.GenerateWalls();

            if (actualFloorNum >= weaponPickUpFloors[Weapon.MiniMissile])
            {
                var secretRooms = this.GenerateSecretRooms(rooms);
                // At this point (going to prod "soon"), I'm okay with not generating secret rooms. It's OK.
                if (secretRooms.Any())
                {
                    foreach (var secretRoom in secretRooms)
                    {
                        // Generating too many power-ups is game-breaking, so do this 50% of the time.
                        // Generating out-of-depth monsters is game-breaking, so no TenLegs until the appropriate floor.
                        // There are eight floors. I'll give you two power-ups (25%), and two empties (25%), 4 monsters (50%)
                        var contents = this.globalRandom.Next(100);
                        // 50% chance to have monsters
                        if (contents <= 50)
                        {
                            var whichMonster = this.globalRandom.Next(100);
                            var spot = secretRoom.Center;
                            var template = "Fuseling";
                            if (whichMonster <= 50 && actualFloorNum >= monsterFloors["slink"])
                            {
                                template = "Slink";
                            }
                            if (whichMonster <= 75 && actualFloorNum >= monsterFloors["tenlegs"])
                            {
                                template = "TenLegs";
                            }
                            else if (whichMonster > 75 && actualFloorNum >= monsterFloors["zug"])
                            {
                                template = "Zug";
                            }

                            var monster = Entity.CreateFromTemplate(template, spot.X, spot.Y);
                            this.Monsters.Add(monster);

                            // If it's a slink: look in the 3x3 tiles around/including it, and generate slinks.
                            if (template == "Slink") {
                                this.GenerateSlinkHorde(spot);
                            }
                        }
                        else if (contents <= 75)
                        {
                            // 25% chance to have a power-up
                            var powerUp = PowerUp.Generate(this.globalRandom);
                            this.PowerUps.Add(powerUp);
                            powerUp.X = secretRoom.Center.X;
                            powerUp.Y = secretRoom.Center.Y;
                        }
                        else
                        {
                            // 25% chance to be empty
                        }
                    }
                }
            }
        }

        private IList<GoRogue.Rectangle> GenerateWalls()
        {
            this.Walls.Clear();
            this.Map = new ArrayMap<bool>(this.Width, this.Height);
            // true = passable, check GoRogue docs.
            var rooms = GoRogue.MapGeneration.QuickGenerators.GenerateRandomRoomsMap(Map, this.globalRandom, MaxRooms, MinRoomSize, MaxRoomSize);
            
            for (var y = 0; y < this.Height; y++) {
                for (var x = 0; x < this.Width; x++) {
                    if (!Map[x, y]) {      
                        var wall = AbstractEntity.Create(SimpleEntity.Wall, x, y);

                        // B9/B10 use alternate colour scheme for walls
                        if (AreLastTwoFloors())
                        {
                            wall.Color = Palette.DarkSkinBrown;
                        }

                        this.Walls.Add(wall);
                    }
                }
            }

            return rooms.ToList();
        }

        private bool AreLastTwoFloors()
        {
            return this.FloorNum >= 8;
        }

        private void GenerateFakeWallClusters()
        {
            var actualFloorNum = this.FloorNum + 1;
            // Don't generate on the missile floor; doing so could block the exit, if we generate
            // between the stairs and block the player if they have limited options to go forward.
            // See: https://trello.com/c/DdNrWX6X/118-random-destructible-walls-block-2f
            if (actualFloorNum > weaponPickUpFloors[Weapon.MiniMissile])
            {
                // Throw in a few fake walls in random places. Well, as long as that tile doesn't have more than 4 adjacent empty spaces.
                var numFakeWallClusters = 3;
                while (numFakeWallClusters > 0) {
                    var spot = this.FindEmptySpot();
                    var adjacentFloors = this.GetAdjacentFloors(spot);
                    if (adjacentFloors.Count <= 4)
                    {
                        // Make a plus-shaped cluster. It's cooler.
                        this.CreateFakeWallClusterAt(spot);
                        numFakeWallClusters -= 1;
                    }
                }
            }
        }

        private void CreateFakeWallClusterAt(GoRogue.Coord center)
        {
            var spots = new List<GoRogue.Coord>() {
                new GoRogue.Coord(center.X, center.Y),
                new GoRogue.Coord(center.X - 1, center.Y),
                new GoRogue.Coord(center.X + 1, center.Y),
                new GoRogue.Coord(center.X, center.Y - 1),
                new GoRogue.Coord(center.X, center.Y + 1),
            };

            foreach (var spot in spots)
            {
                // https://trello.com/c/BYFu7sGD/131-dungeon-generation-crashes
                // Don't generate a fake-wall cluster where one of the peripheral pieces is on the stairs
                if (this.IsWalkable(spot.X, spot.Y) &&
                    !(spot.X == StairsUpLocation.X && spot.Y == StairsUpLocation.Y) &&
                    !(spot.X == StairsDownLocation.X && spot.Y == StairsDownLocation.Y))
                {
                    this.AddNonDupeEntity(new FakeWall(spot.X, spot.Y), this.FakeWalls);
                }
            }
        }

        private IEnumerable<GoRogue.Rectangle> GenerateSecretRooms(IEnumerable<GoRogue.Rectangle> rooms, int numRooms = 2, bool flagWallsAsBacktracking = false)
        {
            var actualFloorNum = this.FloorNum + 1;

            var secretRooms = this.FindPotentialSecretRooms(rooms).Take(numRooms);

            foreach (var room in secretRooms) {
                // Fill the interior with fake walls. Otherwise, FOV gets complicated.
                // Trim perimeter by 1 tile so we get an interior only
                for (var y = room.Rectangle.Y + 1; y < room.Rectangle.Y + room.Rectangle.Height - 1; y++) {
                    for (var x = room.Rectangle.X + 1; x < room.Rectangle.X + room.Rectangle.Width - 1; x++) {
                        var wall = this.Walls.SingleOrDefault(w => w.X == x && w.Y == y);
                        if (wall != null)
                        {
                            this.Walls.Remove(wall);
                        }

                        // Mark as "secret floor" if not perimeter
                        /////////////// Y U NO WORK
                        this.FakeWalls.Add(new FakeWall(x, y, flagWallsAsBacktracking));
                    }
                }

                // Hollow out the walls between us and the real room and fill it with fake walls
                var secretX = room.ConnectedOnLeft ? room.Rectangle.X + room.Rectangle.Width - 1 : room.Rectangle.X;
                for (var y = room.Rectangle.Y + 1; y < room.Rectangle.Y + room.Rectangle.Height - 1; y++) {
                    var wall = this.Walls.SingleOrDefault(w => w.X == secretX && w.Y == y);
                    if (wall != null)
                    {
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
            var actualFloorNum = this.FloorNum + 1;
            if (actualFloorNum >= weaponPickUpFloors[Weapon.Zapper])
            {
                var leftToGenerate = NumberOfLockedDoors;
                var iterationsLeft = 50;

                while (leftToGenerate > 0 && iterationsLeft-- > 0) {
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

        private bool IsAreaWalled(int startX, int startY, int stopX, int stopY)
        {
            for (var y = startY; y < stopY; y++) {
                for (var x = startX; x < stopX; x++) {
                    if (!this.Walls.Any(w => w.X == x && w.Y == y)) {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool AreAdjacentFloors(GoRogue.Coord coord)
        {
            if (this.IsWalkable(coord.X - 1, coord.Y - 1)) { return true; }
            if (this.IsWalkable(coord.X - 1, coord.Y)) { return true; }
            if (this.IsWalkable(coord.X - 1, coord.Y + 1)) { return true; }
            if (this.IsWalkable(coord.X, coord.Y - 1)) { return true; }
            if (this.IsWalkable(coord.X, coord.Y + 1)) { return true; }
            if (this.IsWalkable(coord.X + 1, coord.Y - 1)) { return true; }
            if (this.IsWalkable(coord.X + 1, coord.Y)) { return true; }
            if (this.IsWalkable(coord.X + 1, coord.Y + 1)) { return true; }
            return false;
        }

        private void ShowDataCube(DataCube cube)
        {
            // Trigger the data cube screen and select the cube in question
            EventBus.Instance.Broadcast(GameEvent.ShowSubMenu);
            EventBus.Instance.Broadcast(GameEvent.ChangeSubMenu, typeof(ShowDataCubesStrategy));
            EventBus.Instance.Broadcast(GameEvent.ShowDataCube, cube);
        }

        private void ApplyKnockbacks(Entity entity, int centerX, int centerY, int distance, Direction optionalDirection)
        {
            // Primary knockback in the direction of entity => center
            var dx = entity.X - centerX;
            var dy = entity.Y - centerY;
            if (dx == 0 && dy == 0) {
                // Special case of sorts: gravity shot hit dead-center on the entity; use direction.
                switch (optionalDirection) {
                    case Direction.Down: dy += 1; break;
                    case Direction.Right: dx += 1; break;
                    case Direction.Up: dy -= 1; break;
                    case Direction.Left: dx -= 1; break;
                }
            }
            
            // Move entity <distance> times if spaces are clear
            for (var i = 0; i < distance; i++)
            {
                // Check all spaces and move the entity one by one if the space is empty.
                if (this.IsWalkable(entity.X + Math.Sign(dx), entity.Y + Math.Sign(dy)))
                {
                    // One of these is zero so we're really just moving in one direction.
                    // We can't just set entity position because maybe the knockback is 
                    // knocking back to the left, we're always iterating to the right.
                    entity.X += Math.Sign(dx);                            
                    entity.Y += Math.Sign(dy);
                }
                else
                {
                    return;
                }
            }
        }

        private void AddNonDupeEntity<T>(T entity, List<T> collection) where T : AbstractEntity {
            if (!collection.Any(e => e.X == entity.X && e.Y == entity.Y)) {
                collection.Add(entity);
            }
        }

        private int CalculateDamage(char weaponCharacter)
        {
            if (weaponCharacter == '*') {
                return (int)Math.Ceiling(this.CalculateDamage(Weapon.MiniMissile) * 0.75);
            }

            switch (weaponCharacter) {
                case '`':
                case 'a':
                case 'b':
                case 'c':
                    return this.CalculateDamage(Weapon.MiniMissile);
                case '$': return 0; // Short-range, shouldn't damage you back
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
                case BlasterShot: return Weapon.Blaster;
                case '`':
                case 'a':
                case 'b':
                case 'c':
                    return Weapon.MiniMissile;
                case '$': return Weapon.Zapper;
                case 'o': return Weapon.PlasmaCannon;
                case InstaTeleporterShot: return Weapon.InstaTeleporter;
                case GravityCannonShot: return Weapon.GravityCannon;

                case '*': return Weapon.MiniMissile; // explosion
            }
            throw new InvalidOperationException($"{display} ???");
        }

        // For when the player took a turn (rest? fire?), but DID NOT MOVE. For post-move things, see OnPlayerMoved.
        private void PlayerTookTurn()
        {
            this.ProcessMonsterTurns();
            
            this.PlasmaResidue.ForEach(p => p.Degenerate());
            var deadPlasma = this.PlasmaResidue.Where(p => !p.IsAlive);
            this.PlasmaResidue.RemoveAll(p => deadPlasma.Contains(p));

            if (!this.Player.CanFireGravityCannon)
            {
                if (!this.EnableGravityCannonNextTurn)
                {
                    this.EnableGravityCannonNextTurn = true;
                }
                else
                {
                    this.EnableGravityCannonNextTurn = false;
                    Player.CanFireGravityCannon = true;
                }
            }

            // Copy to array to prevent concurrent modification exception
            foreach (var plasma in this.QuantumPlasma.ToArray())
            {
                this.SpawnQuantumPlasma(plasma.X - 1, plasma.Y);
                this.SpawnQuantumPlasma(plasma.X + 1, plasma.Y);
                this.SpawnQuantumPlasma(plasma.X, plasma.Y - 1);
                this.SpawnQuantumPlasma(plasma.X, plasma.Y + 1);
            }

            if (this.QuantumPlasma.Any(q => q.X == Player.X && q.Y == Player.Y))
            {
                Player.Damage(Player.TotalHealth, Weapon.QuantumPlasma);
                this.LatestMessage = "As quantum plasma rips through you, the Ameer's laughter echoes in your ears ...";
            }

            // Copy array to prevent concurrent modification exception
            var vapourizedMonsters = this.Monsters.ToArray().Where(m => this.QuantumPlasma.Any(p => m.X == p.X && m.Y == p.Y));
            foreach (var monster in vapourizedMonsters)
            {
                monster.Damage(monster.TotalHealth, Weapon.QuantumPlasma);
            }

            var ameer = this.Monsters.SingleOrDefault(m => m is Ameer);
            if (ameer != null)
            {
                ((Ameer)ameer).OnPlayerMoved();
            }            
        }

        private void ProcessMonsterTurns()
        {
            var plasmaBurnedMonsters = new List<Entity>();
            
            // Eggs' turns create more monsters (modify enumeration during iteration).
            // Just use ToArray here to create a copy.
            foreach (var monster in this.Monsters.Where(m => m.CanMove).ToArray())
            {
                if (monster.IsStunned)
                {
                    monster.IsStunned = false;
                }
                else
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
                                AudioManager.Instance.Play("EggLaid");
                            }
                        }
                        
                        // Process turn.
                        if (distance <= 1)
                        {
                            // ATTACK~!
                            var damage = Math.Max(monster.Strength - Player.Defense, 0);
                            Player.Damage(damage, Weapon.Undefined);
                            this.LatestMessage += $" {monster.Name} attacks for {damage} damage!";
                            AudioManager.Instance.Play($"{monster.Name.Replace(" ", "")}Attacks");
                        }
                        else
                        {
                            // Move closer. Randomly-chosen axis.
                            var dx = Math.Sign(Player.X - monster.X);
                            var dy = Math.Sign(Player.Y - monster.Y);
                            var moved = false;
                            var validMoves = new List<GoRogue.Coord>(2);
                            
                            if (this.IsWalkable(monster.X + dx, monster.Y))
                            {
                                validMoves.Add(new GoRogue.Coord(monster.X + dx, monster.Y));
                            }
                            if (this.IsWalkable(monster.X, monster.Y + dy))
                            {
                                validMoves.Add(new GoRogue.Coord(monster.X, monster.Y + dy));
                            }

                            if (validMoves.Any())
                            {
                                var move = validMoves[this.random.Next(0, validMoves.Count)];
                                moved = this.TryToMove(monster, move.X, move.Y);
                            }

                            if (moved)
                            {
                                var plasma = this.PlasmaResidue.SingleOrDefault(p => p.X == monster.X && p.Y == monster.Y);
                                if (plasma != null)
                                {
                                    // Damaging here may cause the monsters collection to modify while iterating over it
                                    plasmaBurnedMonsters.Add(monster);
                                    this.PlasmaResidue.Remove(plasma);
                                }
                            }
                        }
                    }
                }
            }

            foreach (var monster in plasmaBurnedMonsters) {
                monster.Damage(PlasmaResidueDamage, Weapon.Undefined);
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
                if (this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.OpenMenu]))
                {
                    LastGameLogger.Instance.Log("Player died.");
                    SadConsole.Global.CurrentScreen = new TitleConsole(Program.GameWidthInTiles, Program.GameHeightInTiles);
                }

                return false; // don't pass time
            }

            if (justScrolledMessage)
            {
                // When pressing space => showing the last message, don't process THAT as a turn.
                this.LatestMessage = this.leftOverMessage;
                this.leftOverMessage = "";
                justScrolledMessage = false;
                return false;
            }
            if (this.LatestMessage.Length > this.Width && this.keyboard.GetKeysPressed().Any())
            {
                // -7 for [more]. Find the last word boundary before it, and everything after that, 
                // (which we set to leftOverMessage) displays the next page/text.
                var startIndex = this.LatestMessage.Substring(0, this.Width - 7).LastIndexOf(' ');
                this.leftOverMessage = this.LatestMessage.Substring(startIndex);
                this.LatestMessage = this.LatestMessage.Substring(0, startIndex); 
                justScrolledMessage = true;
                return false;
            }

            if (!Player.CanMove)
            {
                return false;
            }

            if (InGameSubMenuConsole.IsOpen)
            {
                return false; // sub-menu will handle input
            }

            var processedInput = false;

            if (this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.OpenMenu]))
            {
                EventBus.Instance.Broadcast(GameEvent.ShowSubMenu);
            }
            
            var destinationX = this.Player.X;
            var destinationY = this.Player.Y;
            
            if ((this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.MoveUp])))
            {
                destinationY -= 1;
                this.Player.DirectionFacing = Direction.Up;
            }
            else if ((this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.MoveDown])))
            {
                destinationY += 1;
                this.Player.DirectionFacing = Direction.Down;
            }

            if ((this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.MoveLeft])))
            {
                destinationX -= 1;
                this.Player.DirectionFacing = Direction.Left;
            }
            else if ((this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.MoveRight])))
            {
                destinationX += 1;
                this.Player.DirectionFacing = Direction.Right;
            }
            else if ((this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.TurnCounterClockWise])))
            {
                Player.TurnCounterClockwise();
            }
            else if ((this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.TurnClockWise])))
            {
                Player.TurnClockwise();
            }
            else if (this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.SelectBlaster]) && Player.Has(Weapon.Blaster))
            {
                Player.CurrentWeapon = Weapon.Blaster;
                AudioManager.Instance.Play("ChangeGun");
            }
            else if (this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.SelectMiniMissile]) && Player.Has(Weapon.MiniMissile))
            {
                Player.CurrentWeapon = Weapon.MiniMissile;
                AudioManager.Instance.Play("ChangeGun");
            }
            else if (this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.SelectZapper]) && Player.Has(Weapon.Zapper))
            {
                Player.CurrentWeapon = Weapon.Zapper;
                AudioManager.Instance.Play("ChangeGun");
            }
            else if (this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.SelectGravityCannon]) && Player.Has(Weapon.GravityCannon))
            {
                Player.CurrentWeapon = Weapon.GravityCannon;
                AudioManager.Instance.Play("ChangeGun");
            }
            else if (this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.SelectPlasmaCannon]) && Player.Has(Weapon.PlasmaCannon))
            {
                Player.CurrentWeapon = Weapon.PlasmaCannon;
                AudioManager.Instance.Play("ChangeGun");
            }
            else if (this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.SelectTeleporter]) && Player.Has(Weapon.InstaTeleporter))
            {
                Player.CurrentWeapon = Weapon.InstaTeleporter;
                AudioManager.Instance.Play("ChangeGun");
            }
            else if (this.FloorNum < 9 && this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.DescendStairs]) && (Options.CanUseStairsFromAnywhere || (Player.X == StairsDownLocation.X && Player.Y == StairsDownLocation.Y)))
            {
                Dungeon.Instance.GoToNextFloor();
                destinationX = Player.X;
                destinationY = Player.Y;
            }
            else if (this.FloorNum > 0 && this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.AscendStairs]) && (Options.CanUseStairsFromAnywhere || (Player.X == StairsUpLocation.X && Player.Y == StairsUpLocation.Y)))
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
            else if (this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.Fire]) && (Player.CurrentWeapon != Weapon.GravityCannon || Player.CanFireGravityCannon))
            {
                this.FireShot();
                // No need to set processedInput=true, that's handled when the weapon effect despawns
            }
            else if (this.GetMonsterAt(destinationX, destinationY) != null)
            {
                // Melee attack
                var monster = this.GetMonsterAt(destinationX, destinationY);
                processedInput = true;

                var damage = Player.Strength - monster.Defense;
                monster.Damage(damage, Weapon.Undefined);
                this.LatestMessage = $"You hit {monster.Name} for {damage} damage!";
                AudioManager.Instance.Play("Melee");
                
                if (Options.MeleeAttackPushesMonsters)
                {
                    var defaultDirection = Direction.Up; // arbitrarily chosen
                    if (monster.X == Player.X)
                    {
                        defaultDirection = Player.Y < monster.Y ? Direction.Down : Direction.Up;
                    }
                    else if (monster.Y == Player.Y)
                    {
                        defaultDirection = Player.X < monster.X ? Direction.Right : Direction.Left;
                    }

                    this.ApplyKnockbacks(monster, Player.X, Player.Y, 1, defaultDirection);
                }

                if (Options.MeleeAttackStuns)
                {
                    monster.IsStunned = true;
                }
            }
            else if (this.Doors.SingleOrDefault(d => d.X == destinationX && d.Y == destinationY && d.IsLocked == false) != null)
            {
                var door = this.Doors.Single(d => d.X == destinationX && d.Y == destinationY && d.IsLocked == false);
                if (!door.IsOpened) {
                    door.IsOpened = true;
                    this.LatestMessage = "You open the door.";
                    AudioManager.Instance.Play("OpenDoor");
                } else {
                    Player.X = door.X;
                    Player.Y = door.Y;
                }
            }
            else if (this.keyboard.IsKeyPressed(Options.KeyBindings[GameAction.SkipTurn]))
            {
                // Skip turn
                processedInput = true;
            }

            return processedInput;
        }

        private Key GetKeyFor(Weapon weapon)
        {
            GameAction action;

            switch (weapon) {
                case Weapon.MiniMissile:
                    action = GameAction.SelectMiniMissile;
                    break;
                case Weapon.Zapper:
                    action = GameAction.SelectZapper;
                    break;
                case Weapon.GravityCannon:
                    action = GameAction.SelectGravityCannon;
                    break;
                case Weapon.PlasmaCannon:
                    action = GameAction.SelectPlasmaCannon;
                    break;
                case Weapon.InstaTeleporter:
                    action = GameAction.SelectTeleporter;
                    break;
                default:
                    throw new ArgumentException($"Not sure what the key binding is for {weapon}");
            }

            return Options.KeyBindings[action];
        }

        private void FireShot()
        {
            var character = BlasterShot;
            string soundEffect = "";

            if (Player.CurrentWeapon != Weapon.Zapper) {
                // Blaster: +
                // Missle: !
                // Shock: $
                // Plasma: o
                switch (Player.CurrentWeapon) {
                    case Weapon.Blaster:
                        character = BlasterShot;
                        soundEffect = "Blaster";
                        break;
                    case Weapon.MiniMissile:
                        switch (Player.DirectionFacing) {
                            case Direction.Up: character = MissileCharacters[0]; break;
                            case Direction.Right: character = MissileCharacters[1]; break;
                            case Direction.Down: character = MissileCharacters[2]; break;
                            case Direction.Left: character = MissileCharacters[3]; break;
                        }
                        soundEffect = "Missile";
                        break;
                    case Weapon.PlasmaCannon:
                        character = 'o';
                        soundEffect = "ShootPlasma";
                        break;
                    case Weapon.GravityCannon:
                        character = GravityCannonShot;
                        soundEffect = "GravityCannon";
                        break;
                    case Weapon.InstaTeleporter:
                        character = InstaTeleporterShot;
                        soundEffect = "TeleporterShot";
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
                    shot = new TeleporterShot(Player.X, Player.Y, Player.DirectionFacing, this.IsWalkable, this.StairsDownLocation);
                } else {
                    shot = new Shot(Player.X + dx, Player.Y + dy, character, Palette.Red, Player.DirectionFacing, this.IsFlyable);
                }
                
                if (character == GravityCannonShot) {
                    Player.CanFireGravityCannon = false;
                }
                
                EffectEntities.Add(shot);
                if (!string.IsNullOrEmpty(soundEffect))
                {
                    AudioManager.Instance.Play(soundEffect, true);
                }
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

                AudioManager.Instance.Play("Zapper");
            }
            
            this.Player.Freeze();
        }

        private void GenerateMonsters()
        {
            // floorNum + 1 because B1 is floorNum 0, the dictionary is in B2, B4 ... not 1, 3, ...
            var numFuselings = this.globalRandom.Next(8, 9); // 8-9 fuselings
            var numSlinks = this.FloorNum + 1 >= monsterFloors["slink"] ? this.globalRandom.Next(3, 5) : 0; // 3-4            
            var numTenLegs = this.FloorNum + 1 >= monsterFloors["tenlegs"] ? this.globalRandom.Next(2, 4) : 0; // 2-3
            var numZugs = this.FloorNum + 1 >= monsterFloors["zug"] ? this.globalRandom.Next(1, 3) : 0; // 1-2

            numFuselings += this.FloorNum; // +1 fuseling per floor
            numSlinks += (int)Math.Floor((this.FloorNum - monsterFloors["slink"]) / 2f); // +1 slink every other floor (B4, B6, B8, B10)
            numTenLegs += (int)Math.Floor((this.FloorNum - monsterFloors["tenlegs"]) / 3f); // +1 tenlegs every third floor (B4, B7, B10)
            numZugs += this.AreLastTwoFloors() ? 1 : 0; // +1 zug on floors B9+

            int iterationsLeft = 100;

            while (numFuselings > 0)
            {
                iterationsLeft--;
                var spot = this.FindEmptySpot(true);

                // https://trello.com/c/DNXtSLW5/33-monsters-generate-next-to-player-when-descends-stairs
                // Make sure monsters don't generate right next to the player
                var distanceToStairsUp = Math.Sqrt(Math.Pow(spot.X - StairsUpLocation.X, 2) + Math.Pow(spot.Y - StairsUpLocation.Y, 2));
                var distanceToStairsDown = Math.Sqrt(Math.Pow(spot.X - StairsDownLocation.X, 2) + Math.Pow(spot.Y - StairsDownLocation.Y, 2));

                if (iterationsLeft == 0 || (distanceToStairsUp >= MinimumDistanceFromMonsterToStairs && distanceToStairsDown >= MinimumDistanceFromMonsterToStairs))
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
                        this.GenerateSlinkHorde(spot);
                    }
                }
            }
        }

        private void GenerateSlinkHorde(GoRogue.Coord spot)
        {
            var numSubSlinks = this.globalRandom.Next(3, 7); // 3-6 in a bunch
            var spots = this.GetAdjacentFloors(spot);
            var spotsToUse = spots.Where(s => IsWalkable(s.X, s.Y)).OrderBy(s => this.globalRandom.Next());
            
            var leftInGroup = Math.Min(spots.Count, numSubSlinks);

            foreach (var slinkSpot in spotsToUse)
            {
                var distanceToStairsUp = Math.Sqrt(Math.Pow(slinkSpot.X - StairsUpLocation.X, 2) + Math.Pow(slinkSpot.Y - StairsUpLocation.Y, 2));
                var distanceToStairsDown = Math.Sqrt(Math.Pow(slinkSpot.X - StairsDownLocation.X, 2) + Math.Pow(slinkSpot.Y - StairsDownLocation.Y, 2));
                if (distanceToStairsUp >= MinimumDistanceFromMonsterToStairs && distanceToStairsDown >= MinimumDistanceFromMonsterToStairs)
                {
                    var monster = Entity.CreateFromTemplate("Slink", slinkSpot.X, slinkSpot.Y);
                    this.Monsters.Add(monster);
                    leftInGroup--;
                }

                if (leftInGroup == 0)
                {
                    return;
                }
            }
        }

        private GoRogue.Coord FindEmptySpot(bool considerGravityWalkable = false)
        {
            var target = new GoRogue.Coord(0, 0);
            var found = false;

            while (!found)
            {
                target = new GoRogue.Coord(this.globalRandom.Next(0, this.Width), this.globalRandom.Next(0, this.Height));

                found =
                    this.IsWalkable(target.X, target.Y, considerGravityWalkable) &&
                    target != this.StairsDownLocation && target != this.StairsUpLocation && // no stairs here
                    PowerUps.All(p => p.X != target.X || p.Y != target.Y);
            }

            return target;
        }

        private Entity GetMonsterAt(int x, int y)
        {
            // BUG: (secondary?) knockback causes two monsters to occupy the same space!!!
            return this.Monsters.FirstOrDefault(m => m.X == x && m.Y == y);
        }

        // Can a projectile "fly" over a spot? True if empty or a chasm; false if occupied by anything
        // (walls, fake walls, doors, monsters, player, etc.)
        private bool IsFlyable(int x, int y, bool considerGravityWalkable = false)
        {
            if (x < 0 || y < 0 || x >= this.Width || y >= this.Height) {
                return false;
            }

            if (this.ShipCore != null && ShipCore.X == x && ShipCore.Y == y)
            {
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

            if (!considerGravityWalkable && this.GravityWaves.Any(g => g.X == x && g.Y == y))
            {
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