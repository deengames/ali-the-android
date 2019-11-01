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
            { Weapon.MiniMissile,       "Fire missiles to destroy cracked walls and debris." },
            { Weapon.Zapper,            "Unjam sealed doors with a jolt of energy. Zaps adjacent enemies." },
            { Weapon.GravityCannon,     "Knock back monsters and disperse gravity waves." },
            { Weapon.InstaTeleporter,   "Teleport across insurmountable chasms." },
            { Weapon.PlasmaCannon,      "Super-heats the floor to damage anything in its wake." },
        };

        public Direction DirectionFacing { get; internal set; }
        public Weapon CurrentWeapon = Weapon.Blaster;
        public bool HasEnvironmentSuit = false;

        public bool CanFireGravityCannon { get; set; } = true;
        
        // Not readonly for JSON.NET to set on load/deserialize.
        public KlogborgShield Shield = new KlogborgShield();

        [JsonProperty]
        internal List<DataCube> DataCubes { get; set; } = new List<DataCube>();

        [JsonProperty]        
        internal List<Weapon> Weapons { get; private set; } = new List<Weapon>() { Weapon.Blaster };


        public Player(List<Weapon> weapons = null) : base("You", '@', NormalColor, 0, 0, 100, 35, 25, 4)
        {
            if (weapons != null)
            {
                // Deserializing. Work around a bug where we end up with two copies of the Blaster.
                this.Weapons = weapons;
            }

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
                    if (weapon != Weapon.Undefined && weapon != Weapon.QuantumPlasma && !Weapons.Contains(weapon))
                    {
                        this.Acquire(weapon);
                    }
                }
            }

            this.ChangeToShieldColor();

            this.GotDataCube(DataCube.GetCube(1, GoRogue.Coord.NONE));
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
            var shieldDamage = this.Shield.Damage(damage);
            var leftoverDamage = damage - shieldDamage;
            base.Damage(leftoverDamage, source);

            if (this.Shield.IsDown())
            {
                this.Color = NormalColor;
                if (this.CurrentHealth <= 0)
                {
                    this.Color = Palette.Grey;
                }
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

        internal void RegenerateShield(GoRogue.FOV playerFov, IList<Entity> monsters)
        {
            this.Shield.OnMove(playerFov, monsters);
            if (!this.Shield.IsDown())
            {
                this.ChangeToShieldColor();
            }
        }

        private void ChangeToShieldColor()
        {
            this.Color = Options.CurrentPalette == SelectablePalette.StandardPalette ? Palette.Cyan : Palette.Blue;
        }
    }
}