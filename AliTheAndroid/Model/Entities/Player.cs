using System;
using System.Collections.Generic;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Loggers;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class Player : Entity
    {
        public Direction DirectionFacing { get; private set; }
        public Weapon CurrentWeapon = Weapon.Blaster;
        public bool HasEnvironmentSuit = false;
        public bool CanFireGravityCannon { get; set; } = true;
        internal List<DataCube> DataCubes { get; } = new List<DataCube>();
        internal static readonly Dictionary<Weapon, string> WeaponPickupMessages = new Dictionary<Weapon, string>() {
            { Weapon.MiniMissile,       "Fire missiles to destroy weak walls and debris." },
            { Weapon.Zapper,            "Unjam sealed doors with a jolt of energy." },
            { Weapon.GravityCannon,     "Knock back monsters and disperse gravity waves." },
            { Weapon.InstaTeleporter,   "Teleport across insurmountable chasms." },
            { Weapon.PlasmaCannon,      "Super-heats the floor to damage anything in its wake." },
        };

        private List<Weapon> weapons { get; } = new List<Weapon>() { Weapon.Blaster };


        public Player() : base("You", '@', Palette.White, 0, 0, 50, 70, 50, 4)
        {
            this.DirectionFacing = Direction.Up;

            if (Options.PlayerStartsWithAllDataCubes)
            {
                for (var i = DataCube.FirstDataCubeFloor; i < DataCube.FirstDataCubeFloor + DataCube.NumCubes; i++)
                {
                    var cube = DataCube.GetCube(i, new GoRogue.Coord(0, 0));
                    this.GotDataCube(cube);
                }
            }

            if (Options.StartWithAllWeapons)
            {
                foreach (var weapon in (Weapon[])Enum.GetValues(typeof(Weapon)))
                {
                    if (weapon != Weapon.Undefined && weapon != Weapon.QuantumPlasma && !weapons.Contains(weapon))
                    {
                        this.Acquire(weapon);
                    }
                }
            }
        }

        public void OnMove(int previousX, int previousY)
        {
            var dx = this.X - previousX;
            var dy = this.Y - previousY;;

            // Naive and not error-proof; correct if we only move in one direction at a time
            if (dx == 0) {
                this.DirectionFacing = dy < 0 ? Direction.Up : Direction.Down;
            } else {
                this.DirectionFacing = dx < 0 ? Direction.Left : Direction.Right;
            }
        }

        public void Freeze() {
            this.CanMove = false;
        }

        public void Unfreeze() {
            this.CanMove = true;
        }
        
        public void Absorb(PowerUp powerUp)
        {
            this.CurrentHealth += powerUp.HealthBoost;
            this.TotalHealth += powerUp.HealthBoost;
            this.Strength += powerUp.StrengthBoost;
            this.Defense += powerUp.DefenseBoost;
            this.VisionRange += powerUp.VisionBoost;
        }

        public bool Has(Weapon weapon)
        {
            return this.weapons.Contains(weapon);
        }

        public void Acquire(Weapon weapon)
        {
            LastGameLogger.Instance.Log($"Acquired weapon: {weapon}");
            if (!this.weapons.Contains(weapon))
            {
                this.weapons.Add(weapon);
            }
        }

        public void GotDataCube(DataCube cube)
        {
            if (!this.DataCubes.Contains(cube))
            {
                LastGameLogger.Instance.Log($"Found data cube on {cube.FloorNumber}");
                this.DataCubes.Add(cube);
            }
        }

        internal void TurnCounterClockwise()
        {
            switch (this.DirectionFacing) {
                case Direction.Up:
                    this.DirectionFacing = Direction.Left;
                    break;
                case Direction.Left:
                    this.DirectionFacing = Direction.Down;
                    break;
                case Direction.Down:
                    this.DirectionFacing = Direction.Right;
                    break;
                case Direction.Right:
                    this.DirectionFacing = Direction.Up;
                    break;
            }
        }

        internal void TurnClockwise()
        {
            switch (this.DirectionFacing) {
                case Direction.Up:
                    this.DirectionFacing = Direction.Right;
                    break;
                case Direction.Right:
                    this.DirectionFacing = Direction.Down;
                    break;
                case Direction.Down:
                    this.DirectionFacing = Direction.Left;
                    break;
                case Direction.Left:
                    this.DirectionFacing = Direction.Up;
                    break;
            }
        }

    }
}