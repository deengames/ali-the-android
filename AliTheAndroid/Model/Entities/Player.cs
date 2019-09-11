using System;
using System.Collections.Generic;
using DeenGames.AliTheAndroid.Accessibility;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Loggers;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class Player : Entity
    {
        private static Color NormalColor = Palette.White;

        internal static readonly Dictionary<Weapon, string> WeaponPickupMessages = new Dictionary<Weapon, string>() {
            { Weapon.MiniMissile,       "Fire missiles to destroy weak walls and debris." },
            { Weapon.Zapper,            "Unjam sealed doors with a jolt of energy." },
            { Weapon.GravityCannon,     "Knock back monsters and disperse gravity waves." },
            { Weapon.InstaTeleporter,   "Teleport across insurmountable chasms." },
            { Weapon.PlasmaCannon,      "Super-heats the floor to damage anything in its wake." },
        };

        public Direction DirectionFacing { get; private set; }
        public Weapon CurrentWeapon = Weapon.Blaster;
        public bool HasEnvironmentSuit = false;
        private const int ShieldRegenPerMove = 1;

        public bool CanFireGravityCannon { get; set; } = true;
        public int CurrentShield { get; private set; }
        public const int MaxShield = 100;

        [JsonProperty]
        internal List<DataCube> DataCubes { get; set; } = new List<DataCube>();

        [JsonProperty]        
        internal List<Weapon> Weapons { get; private set; } = new List<Weapon>() { Weapon.Blaster };


        public Player(List<Weapon> weapons = null) : base("You", '@', NormalColor, 0, 0, 250, 35, 25, 4)
        {
            if (weapons != null)
            {
                // Deserializing. Work around a bug where we end up with two copies of the Blaster.
                this.Weapons = weapons;
            }
            
            this.DirectionFacing = Direction.Up;
            this.CurrentShield = Player.MaxShield;

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
                    if (weapon != Weapon.Undefined && weapon != Weapon.QuantumPlasma && !Weapons.Contains(weapon))
                    {
                        this.Acquire(weapon);
                    }
                }
            }

            this.ChangeToShieldColor();
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
            return this.Weapons.Contains(weapon);
        }

        public void Acquire(Weapon weapon)
        {
            LastGameLogger.Instance.Log($"Acquired weapon: {weapon}");
            if (!this.Weapons.Contains(weapon))
            {
                this.Weapons.Add(weapon);
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

        override public void Damage(int damage, Weapon source)
        {
            var shieldDamage = Math.Min(this.CurrentShield, damage);
            var leftoverDamage = damage - shieldDamage;
            this.CurrentShield -= shieldDamage;
            base.Damage(leftoverDamage, source);

            if (this.CurrentShield == 0)
            {
                this.Color = NormalColor;
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

        internal void RegenerateShield()
        {
            this.CurrentShield += Player.ShieldRegenPerMove;
            this.CurrentShield = Math.Min(this.CurrentShield, Player.MaxShield);
            this.ChangeToShieldColor();
        }

        private void ChangeToShieldColor()
        {
            this.Color = Options.CurrentPalette == SelectablePalette.StandardPalette ? Palette.Cyan : Palette.Blue;
        }
    }
}