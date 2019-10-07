using System;
using System.Linq;
using Microsoft.Xna.Framework;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Model;
using DeenGames.AliTheAndroid.Model.Events;

namespace DeenGames.AliTheAndroid.Consoles
{
    public class CoreGameConsole : SadConsole.Console
    {
        private const int ScreenWidth = 640;
        private const int ScreenHeight = 448;
        private readonly int ScreenTilesWidth = 20;
        private readonly int ScreenTilesHeight = 14;
        private const int RotatePowerUpColorEveryMilliseconds = 334;
        private const int RotateWeaponColorEveryMilliseconds = 400;
        private const int RotatePlasmaDriveColorEveryMilliseconds = 250;
        private readonly Color[] StairsColours = new Color[]
        {
            Palette.White, Palette.LightLilacPink, Palette.LilacPinkPurple
        };

        private TimeSpan gameTime;
        private Dungeon dungeon;
        private InGameSubMenuConsole subMenuConsole = null;
        private Random random = new Random();
        private SadConsole.Console backBuffer;
        private SadConsole.Console messageConsole;

        public CoreGameConsole(int width, int height, Dungeon dungeon) : base(width, height)
        {
            var fontMaster = SadConsole.Global.LoadFont("Fonts/AliTheAndroid.font");
            var normalSizedFont = fontMaster.GetFont(SadConsole.Font.FontSizes.Two);
            this.Font = normalSizedFont;

            this.ScreenTilesWidth = (int)Math.Floor(1.0f * ScreenWidth / normalSizedFont.Size.X);
            this.ScreenTilesHeight = (int)Math.Floor(1.0f * ScreenHeight / normalSizedFont.Size.Y);
            Console.WriteLine($"{this.ScreenTilesWidth}x{this.ScreenTilesHeight}");

            this.backBuffer = new SadConsole.Console(width, height);
            this.dungeon = dungeon;

            this.messageConsole = new SadConsole.Console(this.Width, 2);
            this.messageConsole.Position = new Point(0, this.Height - this.messageConsole.Height);
            this.Children.Add(this.messageConsole);
            
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

            // Fix: starting the game shows one frame of everything filled with the stairs/portal character
            this.Fill(Color.Black, Color.Black, ' ');
        }

        override public void Update(TimeSpan delta)
        {
            this.dungeon.Update(delta);
            gameTime += delta;
            this.RedrawEverything(delta);
        }

        // Redraws only what fits on-screen, thus being a "camera" of sorts
        // Redraws ENTIRE MAP, then global-to-local redraws the camera part on screen
        private void RedrawEverything(TimeSpan delta)
        {
            backBuffer.Fill(Color.Black, Color.Black, ' ');

            for (var y = 0; y < this.dungeon.Height; y++)
            {
                for (var x = 0; x < this.dungeon.Width; x++)
                {
                    if (this.dungeon.CurrentFloor.IsInPlayerFov(x, y))
                    {
                        backBuffer.SetGlyph(x, y, '.', Palette.DarkPurple, Palette.DarkPurple);
                    }
                    else if (this.dungeon.CurrentFloor.IsSeen(x, y))
                    {
                        backBuffer.SetGlyph(x, y, '.', Palette.BlackAlmost, Palette.BlackAlmost);
                    }
                }
            }

            foreach (var residue in this.dungeon.CurrentFloor.PlasmaResidue)
            {
                if (this.dungeon.CurrentFloor.IsInPlayerFov(residue.X, residue.Y))
                {
                    backBuffer.SetGlyph(residue.X, residue.Y, residue.Character, residue.Color, Palette.LightRed);
                }
            }

            foreach (var wall in dungeon.CurrentFloor.Walls)
            {
                var x = wall.X;
                var y = wall.Y;

                if (this.dungeon.CurrentFloor.IsInPlayerFov(x, y))
                {
                    backBuffer.SetGlyph(wall.X, wall.Y, wall.Character, wall.Color);
                }
                else if (this.dungeon.CurrentFloor.IsSeen(x, y))
                {
                  backBuffer.SetGlyph(wall.X, wall.Y, wall.Character, Palette.Grey);
                }
            }

            foreach (var chasm in this.dungeon.CurrentFloor.Chasms)
            {
                if (this.dungeon.CurrentFloor.IsInPlayerFov(chasm.X, chasm.Y))
                {
                    backBuffer.SetGlyph(chasm.X, chasm.Y, chasm.Character, chasm.Color, Palette.DarkMutedBrown);
                }
                else if (this.dungeon.CurrentFloor.IsSeen(chasm.X, chasm.Y))
                {
                    backBuffer.SetGlyph(chasm.X, chasm.Y, chasm.Character, Palette.Grey);
                }
            }
            
            foreach (var door in this.dungeon.CurrentFloor.Doors)
            {
                var x = door.X;
                var y = door.Y;

                if (this.dungeon.CurrentFloor.IsInPlayerFov(x, y))
                {
                    backBuffer.SetGlyph(x, y, door.Character, door.Color);
                }
                else if (this.dungeon.CurrentFloor.IsSeen(x, y))
                {
                  backBuffer.SetGlyph(x, y, door.Character, Palette.Grey);
                }
            }
            
            foreach (var wave in this.dungeon.CurrentFloor.GravityWaves) {
                if (this.dungeon.CurrentFloor.IsInPlayerFov(wave.X, wave.Y)) {
                    backBuffer.SetGlyph(wave.X, wave.Y, wave.Character, wave.Color);
                }
            }

            var elapsedSeconds = this.gameTime.TotalMilliseconds;
            var stairsCharIndex = (int)Math.Floor(elapsedSeconds / RotatePlasmaDriveColorEveryMilliseconds) % 3;
            var stairsColour =  StairsColours[stairsCharIndex];

            if (this.dungeon.CurrentFloor.StairsDownLocation != GoRogue.Coord.NONE) {
                int stairsX = this.dungeon.CurrentFloor.StairsDownLocation.X;
                int stairsY = this.dungeon.CurrentFloor.StairsDownLocation.Y;

                if (this.dungeon.CurrentFloor.IsInPlayerFov(stairsX, stairsY) || this.dungeon.CurrentFloor.IsSeen(stairsX, stairsY)) {
                    backBuffer.SetGlyph(stairsX, stairsY, stairsCharIndex, this.dungeon.CurrentFloor.IsInPlayerFov(stairsX, stairsY) ? stairsColour : Palette.Grey);
                }
            }

            if (this.dungeon.CurrentFloorNum > 0 && this.dungeon.CurrentFloor.StairsUpLocation != GoRogue.Coord.NONE) {
                int stairsX = this.dungeon.CurrentFloor.StairsUpLocation.X;
                int stairsY = this.dungeon.CurrentFloor.StairsUpLocation.Y;

                if (this.dungeon.CurrentFloor.IsInPlayerFov(stairsX, stairsY) || this.dungeon.CurrentFloor.IsSeen(stairsX, stairsY)) {
                    backBuffer.SetGlyph(stairsX, stairsY, stairsCharIndex, this.dungeon.CurrentFloor.IsInPlayerFov(stairsX, stairsY) ? stairsColour : Palette.Grey);
                }
            }

            foreach (var monster in this.dungeon.CurrentFloor.Monsters)
            {                
                if (this.dungeon.CurrentFloor.IsInPlayerFov(monster.X, monster.Y))
                {
                    var character = monster.Character;
                    var colour = monster is Ameer ? monster.Color : Entity.MonsterColours[monster.Name];

                    backBuffer.SetGlyph(monster.X, monster.Y, character, colour);
                }
            }

            // Drawn over monsters because THEY HIDE IN FAKE WALLS. Sometimes.
            foreach (var wall in dungeon.CurrentFloor.FakeWalls)
            {
                var x = wall.X;
                var y = wall.Y;

                if (this.dungeon.CurrentFloor.IsInPlayerFov(x, y))
                {
                    backBuffer.SetGlyph(wall.X, wall.Y, wall.Character, FakeWall.Colour);
                }
                else if (this.dungeon.CurrentFloor.IsSeen(x, y))
                {
                  backBuffer.SetGlyph(wall.X, wall.Y, wall.Character, Palette.Grey);
                }
            }

            foreach (var powerUp in this.dungeon.CurrentFloor.PowerUps) {
                // B1 has power-ups under the fake wall. Don't show it.
                if (this.dungeon.CurrentFloor.IsInPlayerFov(powerUp.X, powerUp.Y) && !this.dungeon.CurrentFloor.FakeWalls.Any(f => f.X == powerUp.X && f.Y == powerUp.Y)) {
                    var colours = Options.CurrentPalette.PowerUpColours;
                    var colourIndex = (int)Math.Floor(elapsedSeconds / RotatePowerUpColorEveryMilliseconds) % colours.Count;
                    backBuffer.SetGlyph(powerUp.X, powerUp.Y, powerUp.Character, colours[colourIndex]);
                }
            }

            var weaponPickUp = this.dungeon.CurrentFloor.WeaponPickUp;
            // Weapons are always visible. This adds tension/arrow-of-play. You need them to 
            // get through obstacles on later floors. #notabug
            if (weaponPickUp != null) {
                var colours = Options.CurrentPalette.WeaponColours;
                var colourIndex = (int)Math.Floor(elapsedSeconds / RotateWeaponColorEveryMilliseconds) % colours.Count;
                backBuffer.SetGlyph(weaponPickUp.X, weaponPickUp.Y, weaponPickUp.Character, colours[colourIndex]);
            }

            var dataCube = this.dungeon.CurrentFloor.DataCube;
            if (dataCube != null)
            {
                var colours = Options.CurrentPalette.DataCubeColours;
                var colourIndex = (int)Math.Floor(elapsedSeconds / RotatePowerUpColorEveryMilliseconds) % colours.Count;
                backBuffer.SetGlyph(dataCube.X, dataCube.Y, dataCube.Character, colours[colourIndex]);
            }

            var shipCore = this.dungeon.CurrentFloor.ShipCore;
            if (shipCore != null)
            {
                var colourIndex = (int)Math.Floor(elapsedSeconds / RotatePlasmaDriveColorEveryMilliseconds) % ShipCore.Colours.Length;
                backBuffer.SetGlyph(shipCore.X, shipCore.Y, shipCore.Character, ShipCore.Colours[colourIndex]);
            }

            foreach (var plasma in this.dungeon.CurrentFloor.QuantumPlasma)
            {
                // Doesn't care about LOS. You're dead if you get cornered.
                backBuffer.SetGlyph(plasma.X, plasma.Y, plasma.Character, plasma.Color);
            }

            backBuffer.SetGlyph(this.dungeon.Player.X, this.dungeon.Player.Y, this.dungeon.Player.Character, this.dungeon.Player.Color);

            foreach (var effect in this.dungeon.CurrentFloor.EffectEntities) {
                if (this.dungeon.CurrentFloor.IsInPlayerFov(effect.X, effect.Y)) {
                    backBuffer.SetGlyph(effect.X, effect.Y, effect.Character, effect.Color);
                }
            }

            // Keeps rendering from going out-of-bounds
            var cameraStartX = Math.Max(0, dungeon.CurrentFloor.Player.X - (ScreenTilesWidth / 2));
            var cameraStartY = Math.Max(0, dungeon.CurrentFloor.Player.Y - (ScreenTilesHeight / 2));
            var cameraStopX = Math.Min(cameraStartX + ScreenTilesWidth, dungeon.Width);
            var cameraStopY = Math.Min(cameraStartY + ScreenTilesHeight, dungeon.Height);
            
            // https://twitter.com/nightblade99/status/1180203280946864129
            // What if the user is in the bottom-right corner of the map?
            // camera start will be correct, but stop will be the max, meaning we're just rendering a tiny block.
            // Nah, bro. Instead, offset start X/Y backward until we're rendering a full screen.
            var dx = ScreenTilesWidth - (cameraStopX - cameraStartX);
            var dy = ScreenTilesHeight - (cameraStopY - cameraStartY);
            // dx/dy are only positive in the bottom-right quadrant
            cameraStartX -= dx;
            cameraStartY -= dy;

            for (var y = cameraStartY; y < cameraStopY; y++)
            {
                for (var x = cameraStartX; x < cameraStopX; x++)
                {
                    // Global to local
                    this.SetGlyph(x - cameraStartX, y - cameraStartY, this.backBuffer.GetGlyph(x, y), backBuffer.GetForeground(x, y), backBuffer.GetBackground(x, y));
                }
            }

            this.DrawMessageConsole();
            this.DrawSubMenu(delta);
        }

        private void DrawMessageConsole()
        {
            this.messageConsole.DrawLine(new Point(0, 0), new Point(this.dungeon.Width, 0), null, Palette.BlackAlmost, ' ');
            this.messageConsole.DrawLine(new Point(0, 1), new Point(this.dungeon.Width, 1), null, Palette.BlackAlmost, ' ');
            this.DrawHealthAndPowerUpIndicators();

            this.messageConsole.Print(this.dungeon.Width - 4, 0, $"B{this.dungeon.CurrentFloorNum + 1}", Palette.White);

            var message = this.dungeon.CurrentFloor.LatestMessage;
            
            if (message.Length > this.dungeon.Width)
            {
                var firstLineBreak = message.Substring(0, this.dungeon.Width - 7).LastIndexOfAny(new char[] { ' ', '.'} ) + 1;
                var firstLine = message.Substring(0, firstLineBreak);

                this.messageConsole.Print(1, 1, $"{firstLine} [more]", Palette.White);
            }
            else
            {
                this.messageConsole.Print(1, 1, message, Palette.White);
            }
        }

        private void DrawHealthAndPowerUpIndicators()
        {
            var weaponString = $"{this.dungeon.Player.CurrentWeapon}";
            if (this.dungeon.Player.CurrentWeapon == Weapon.GravityCannon && !this.dungeon.Player.CanFireGravityCannon) {
                weaponString += " (charging)";
            }
            string message = $"{this.dungeon.Player.CurrentHealth}/{this.dungeon.Player.TotalHealth} health, {this.dungeon.Player.CurrentShield}/{Player.MaxShield} shield ({this.dungeon.Player.DirectionFacing.ToString()}) Using: {weaponString}";
            
            foreach (var monster in this.dungeon.CurrentFloor.Monsters)
            {
                var distance = Math.Sqrt(Math.Pow(monster.X - this.dungeon.Player.X, 2) + Math.Pow(monster.Y - this.dungeon.Player.Y, 2));
                if (distance <= 1)
                {
                    // compact
                    message = $"{message} {monster.Character}: {monster.CurrentHealth}/{monster.TotalHealth}"; 
                }
            }

            this.messageConsole.Print(1, 0, message, Palette.White);

            var powerUpMessage = "";

            foreach (var powerUp in this.dungeon.CurrentFloor.PowerUps)
            {
                var distance = Math.Sqrt(Math.Pow(powerUp.X - this.dungeon.Player.X, 2) + Math.Pow(powerUp.Y - this.dungeon.Player.Y, 2));
                if (distance <= 1)
                {
                    powerUpMessage = $"{powerUpMessage} Power-up: {powerUp.Message}";
                }
            }

            this.messageConsole.Print(1, 1, powerUpMessage, Palette.White);
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