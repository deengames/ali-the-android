using System;
using System.Linq;
using Microsoft.Xna.Framework;
using GoRogue.MapViews;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Model.Events;

namespace DeenGames.AliTheAndroid.Consoles
{
    public class CoreGameConsole : SadConsole.Console
    {
        private const int RotatePowerUpColorEveryMilliseconds = 334;
        private const int RotateWeaponColorEveryMilliseconds = 400;
        private const int RotatePlasmaDriveColorEveryMilliseconds = 250;
        private TimeSpan gameTime;
        private Dungeon dungeon;
        private InGameSubMenuConsole subMenuConsole = null;
        private Random random = new Random();


        public CoreGameConsole(int width, int height, Dungeon dungeon) : base(width, height)
        {
            this.dungeon = dungeon;
            
            EventBus.Instance.AddListener(GameEvent.ShowSubMenu, (obj) =>
            {
                if (this.subMenuConsole == null)
                {
                    this.subMenuConsole = new InGameSubMenuConsole(this.dungeon.CurrentFloor.Player);
                    this.subMenuConsole.Position = new Point((Width - this.subMenuConsole.Width) / 2, (Height - this.subMenuConsole.Height) / 2);
                    this.Children.Add(this.subMenuConsole);
                }
            });

            EventBus.Instance.AddListener(GameEvent.HideSubMenu, (data) =>
            {
                if (this.subMenuConsole != null)
                {
                    this.Children.Remove(this.subMenuConsole);
                    this.subMenuConsole = null;
                    InGameSubMenuConsole.IsOpen = false;
                }
            });
        }

        override public void Update(TimeSpan delta)
        {
            this.dungeon.Update(delta);
            gameTime += delta;
            this.RedrawEverything(delta);
        }

        private void RedrawEverything(TimeSpan delta)
        {
            var floorCharacter = Options.DisplayOldStyleAsciiCharacters ? '.' : ' ';
            var wallCharacter = Options.DisplayOldStyleAsciiCharacters ? 
                AbstractEntity.WallCharacter["ascii"] : AbstractEntity.WallCharacter["solid"];            

            this.Fill(Palette.BlackAlmost, Palette.BlackAlmost, ' ');

            for (var y = 0; y < this.dungeon.Height; y++)
            {
                for (var x = 0; x < this.dungeon.Width; x++)
                {
                    if (this.dungeon.CurrentFloor.IsInPlayerFov(x, y))
                    {
                        this.SetGlyph(x, y, floorCharacter, Palette.Grey);
                    }
                    else if (this.dungeon.CurrentFloor.IsSeen(x, y))
                    {
                        this.SetGlyph(x, y, floorCharacter, Palette.DarkPurple);
                    }
                }
            }

            foreach (var residue in this.dungeon.CurrentFloor.PlasmaResidue) {
                if (this.dungeon.CurrentFloor.IsInPlayerFov(residue.X, residue.Y)) {
                    this.SetGlyph(residue.X, residue.Y, residue.Character, residue.Color);
                }
            }

            var allWalls = this.dungeon.CurrentFloor.Walls.Union(this.dungeon.CurrentFloor.FakeWalls);

            foreach (var wall in allWalls)
            {
                var x = wall.X;
                var y = wall.Y;

                var colour = Options.ShowFakeWalls && this.dungeon.CurrentFloor.FakeWalls.Contains(wall) ?
                    FakeWall.Colour : wall.Color;

                if (this.dungeon.CurrentFloor.IsInPlayerFov(x, y))
                {
                    this.SetGlyph(wall.X, wall.Y, wallCharacter, colour);
                }
                else if (this.dungeon.CurrentFloor.IsSeen(x, y))
                {
                  this.SetGlyph(wall.X, wall.Y, wallCharacter, Palette.Grey);
                }
            }

            foreach (var chasm in this.dungeon.CurrentFloor.Chasms)
            {
                if (this.dungeon.CurrentFloor.IsInPlayerFov(chasm.X, chasm.Y))
                {
                    this.SetGlyph(chasm.X, chasm.Y, chasm.Character, chasm.Color, Palette.DarkMutedBrown);
                } else if (this.dungeon.CurrentFloor.IsSeen(chasm.X, chasm.Y))
                {
                    this.SetGlyph(chasm.X, chasm.Y, chasm.Character, Palette.Grey);
                }
            }
            
            foreach (var door in this.dungeon.CurrentFloor.Doors)
            {
                var x = door.X;
                var y = door.Y;

                if (this.dungeon.CurrentFloor.IsInPlayerFov(x, y))
                {
                    this.SetGlyph(x, y, door.Character, door.Color);
                }
                else if (this.dungeon.CurrentFloor.IsSeen(x, y))
                {
                  this.SetGlyph(x, y, door.Character, Palette.Grey);
                }
            }
            
            foreach (var wave in this.dungeon.CurrentFloor.GravityWaves) {
                if (this.dungeon.CurrentFloor.IsInPlayerFov(wave.X, wave.Y)) {
                    this.SetGlyph(wave.X, wave.Y, wave.Character, wave.Color);
                }
            }

            if (this.dungeon.CurrentFloor.StairsDownLocation != GoRogue.Coord.NONE) {
                int stairsX = this.dungeon.CurrentFloor.StairsDownLocation.X;
                int stairsY = this.dungeon.CurrentFloor.StairsDownLocation.Y;

                if (this.dungeon.CurrentFloor.IsInPlayerFov(stairsX, stairsY) || this.dungeon.CurrentFloor.IsSeen(stairsX, stairsY)) {
                    this.SetGlyph(stairsX, stairsY, '>', this.dungeon.CurrentFloor.IsInPlayerFov(stairsX, stairsY) ? Palette.White : Palette.Grey);
                }
            }

            if (this.dungeon.CurrentFloorNum > 0 && this.dungeon.CurrentFloor.StairsUpLocation != GoRogue.Coord.NONE) {
                int stairsX = this.dungeon.CurrentFloor.StairsUpLocation.X;
                int stairsY = this.dungeon.CurrentFloor.StairsUpLocation.Y;

                if (this.dungeon.CurrentFloor.IsInPlayerFov(stairsX, stairsY) || this.dungeon.CurrentFloor.IsSeen(stairsX, stairsY)) {
                    this.SetGlyph(stairsX, stairsY, '<', this.dungeon.CurrentFloor.IsInPlayerFov(stairsX, stairsY) ? Palette.White : Palette.Grey);
                }
            }

            foreach (var monster in this.dungeon.CurrentFloor.Monsters)
            {                
                if (this.dungeon.CurrentFloor.IsInPlayerFov(monster.X, monster.Y))
                {
                    var character = monster.Character;
                    var colour = Entity.MonsterColours[monster.Name];

                    this.SetGlyph(monster.X, monster.Y, character, colour);
                }
            }

            foreach (var powerUp in this.dungeon.CurrentFloor.PowerUps) {
                // B1 has power-ups under the fake wall. Don't show it.
                if (this.dungeon.CurrentFloor.IsInPlayerFov(powerUp.X, powerUp.Y) && !this.dungeon.CurrentFloor.FakeWalls.Any(f => f.X == powerUp.X && f.Y == powerUp.Y)) {
                    var elapsedSeconds = this.gameTime.TotalMilliseconds;
                    var colours = Options.CurrentPalette.PowerUpColours;
                    var colourIndex = (int)Math.Floor(elapsedSeconds / RotatePowerUpColorEveryMilliseconds) % colours.Count;
                    this.SetGlyph(powerUp.X, powerUp.Y, powerUp.Character, colours[colourIndex]);
                }
            }

            var weaponPickUp = this.dungeon.CurrentFloor.WeaponPickUp;
            // Weapons are always visible. This adds tension/arrow-of-play. You need them to 
            // get through obstacles on later floors. #notabug
            if (weaponPickUp != null) {
                var elapsedSeconds = this.gameTime.TotalMilliseconds;
                var colours = Options.CurrentPalette.WeaponColours;
                var colourIndex = (int)Math.Floor(elapsedSeconds / RotateWeaponColorEveryMilliseconds) % colours.Count;
                this.SetGlyph(weaponPickUp.X, weaponPickUp.Y, weaponPickUp.Character, colours[colourIndex]);
            }

            var dataCube = this.dungeon.CurrentFloor.DataCube;
            if (dataCube != null)
            {
                var elapsedSeconds = this.gameTime.TotalMilliseconds;
                var colours = Options.CurrentPalette.DataCubeColours;
                var colourIndex = (int)Math.Floor(elapsedSeconds / RotatePowerUpColorEveryMilliseconds) % colours.Count;
                this.SetGlyph(dataCube.X, dataCube.Y, dataCube.Character, colours[colourIndex]);
            }

            var shipCore = this.dungeon.CurrentFloor.ShipCore;
            if (shipCore != null)
            {
                var elapsedSeconds = this.gameTime.TotalMilliseconds;
                var colourIndex = (int)Math.Floor(elapsedSeconds / RotatePlasmaDriveColorEveryMilliseconds) % ShipCore.Colours.Length;
                this.SetGlyph(shipCore.X, shipCore.Y, shipCore.Character, ShipCore.Colours[colourIndex]);
            }

            foreach (var plasma in this.dungeon.CurrentFloor.QuantumPlasma)
            {
                // Doesn't care about LOS. You're dead if you get cornered.
                var plasmaColor = random.Next(100) <= 7 ? Palette.LilacPinkPurple : plasma.Color;
                this.SetGlyph(plasma.X, plasma.Y, plasma.Character, plasmaColor);
            }

            this.SetGlyph(this.dungeon.Player.X, this.dungeon.Player.Y, this.dungeon.Player.Character, this.dungeon.Player.Color);

            foreach (var effect in this.dungeon.CurrentFloor.EffectEntities) {
                if (this.dungeon.CurrentFloor.IsInPlayerFov(effect.X, effect.Y)) {
                    this.SetGlyph(effect.X, effect.Y, effect.Character, effect.Color);
                }
            }

            this.DrawLine(new Point(0, this.dungeon.Height - 2), new Point(this.dungeon.Width, this.dungeon.Height - 2), null, Palette.BlackAlmost, ' ');
            this.DrawLine(new Point(0, this.dungeon.Height - 1), new Point(this.dungeon.Width, this.dungeon.Height - 1), null, Palette.BlackAlmost, ' ');
            this.DrawHealthAndPowerUpIndicators();

            this.Print(this.dungeon.Width - 4, this.dungeon.Height - 2, $"B{this.dungeon.CurrentFloorNum + 1}", Palette.White);

            var message = this.dungeon.CurrentFloor.LatestMessage;
            
            if (message.Length > this.dungeon.Width)
            {
                var firstLineBreak = message.Substring(0, this.dungeon.Width).LastIndexOfAny(new char[] { ' ', '.'} ) + 1;
                var firstLine = message.Substring(0, firstLineBreak);
                var secondLine = message.Substring(firstLineBreak);

                // Overrides health momentarily.
                this.Print(1, this.dungeon.Height - 2, firstLine, Palette.White);
                this.Print(1, this.dungeon.Height - 1, secondLine, Palette.White);
            }
            else
            {
                this.Print(1, this.dungeon.Height - 1, message, Palette.White);
            }

            this.DrawSubMenu(delta);
        }

        private void DrawHealthAndPowerUpIndicators()
        {
            var weaponString = $"{this.dungeon.Player.CurrentWeapon}";
            if (this.dungeon.Player.CurrentWeapon == Weapon.GravityCannon && !this.dungeon.Player.CanFireGravityCannon) {
                weaponString += " (charging)";
            }
            string message = $"You: {this.dungeon.Player.CurrentHealth}/{this.dungeon.Player.TotalHealth} (facing {this.dungeon.Player.DirectionFacing.ToString()}) Equipped: {weaponString}";
            
            foreach (var monster in this.dungeon.CurrentFloor.Monsters)
            {
                var distance = Math.Sqrt(Math.Pow(monster.X - this.dungeon.Player.X, 2) + Math.Pow(monster.Y - this.dungeon.Player.Y, 2));
                if (distance <= 1)
                {
                    // compact
                    message = $"{message} {monster.Character}: {monster.CurrentHealth}/{monster.TotalHealth}"; 
                }
            }

            this.Print(1, this.dungeon.Height - 2, message, Palette.White);


            var powerUpMessage = "";

            foreach (var powerUp in this.dungeon.CurrentFloor.PowerUps)
            {
                var distance = Math.Sqrt(Math.Pow(powerUp.X - this.dungeon.Player.X, 2) + Math.Pow(powerUp.Y - this.dungeon.Player.Y, 2));
                if (distance <= 1)
                {
                    powerUpMessage = $"{powerUpMessage} Power-up: {powerUp.Message}";
                }
            }

            this.Print(1, this.dungeon.Height - 1, powerUpMessage, Palette.White);
        }

        private void DrawSubMenu(TimeSpan delta)
        {
            if (this.subMenuConsole != null)
            {
                this.subMenuConsole.Update(delta);
            }
        }
    }
}