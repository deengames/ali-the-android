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
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.EventData;
using DeenGames.AliTheAndroid.Model;

namespace DeenGames.AliTheAndroid.Prototype
{
    public class PrototypeGameConsole : SadConsole.Console
    {
        private Dungeon dungeon;

        public PrototypeGameConsole(int width, int height) : base(width, height)
        {
            this.dungeon = new Dungeon(width, height);
            this.dungeon.Generate();
            this.RedrawEverything();
        }

        override public void Update(TimeSpan delta)
        {
            this.dungeon.Update(delta);
        }

        private void RedrawEverything()
        {
            this.Fill(Palette.BlackAlmost, Palette.BlackAlmost, ' ');

            // One day, I will do better. One day, I will efficiently draw only what changed!
            for (var y = 0; y < this.dungeon.Height; y++)
            {
                for (var x = 0; x < this.dungeon.Width; x++)
                {
                    if (this.dungeon.CurrentFloor.IsInPlayerFov(x, y))
                    {
                        this.SetGlyph(x, y, '.', Palette.LightGrey);
                    }
                    else if (this.dungeon.CurrentFloor.IsSeen(x, y))
                    {
                        this.SetGlyph(x, y, '.', Palette.Grey);
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

                var colour = DebugOptions.ShowFakeWalls && this.dungeon.CurrentFloor.FakeWalls.Contains(wall) ? Palette.Blue : Palette.LightGrey;

                if (this.dungeon.CurrentFloor.IsInPlayerFov(x, y))
                {
                    this.SetGlyph(wall.X, wall.Y, wall.Character, colour);
                }
                else if (this.dungeon.CurrentFloor.IsSeen(x, y))
                {
                  this.SetGlyph(wall.X, wall.Y, wall.Character, colour);
                }
            }

            foreach (var chasm in this.dungeon.CurrentFloor.Chasms) {
                if (this.dungeon.CurrentFloor.IsInPlayerFov(chasm.X, chasm.Y)) {
                    this.SetGlyph(chasm.X, chasm.Y, chasm.Character, chasm.Color);
                } else if (this.dungeon.CurrentFloor.IsSeen(chasm.X, chasm.Y)) {
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

            foreach (var monster in this.dungeon.CurrentFloor.Monsters)
            {                
                if (this.dungeon.CurrentFloor.IsInPlayerFov(monster.X, monster.Y))
                {
                    var character = monster.Character;

                    this.SetGlyph(monster.X, monster.Y, character, monster.Color);
                    
                    if (monster.CurrentHealth < monster.TotalHealth) {
                        this.SetGlyph(monster.X, monster.Y, character, Palette.Orange);
                    }
                }
            }

            foreach (var effect in this.dungeon.CurrentFloor.EffectEntities) {
                if (this.dungeon.CurrentFloor.IsInPlayerFov(effect.X, effect.Y)) {
                    this.SetGlyph(effect.X, effect.Y, effect.Character, effect.Color);
                }
            }

            int stairsX = this.this.dungeon.CurrentFloor.StairsLocation.X;
            int stairsY = this.this.dungeon.CurrentFloor.stairsLocation.Y;

            if (this.dungeon.CurrentFloor.IsInPlayerFov(stairsX, stairsY) || this.dungeon.CurrentFloor.IsSeen(stairsX, stairsY)) {
                this.SetGlyph(stairsX, stairsY, '>', this.this.dungeon.CurrentFloor.IsInPlayerFov(stairsX, stairsY) ? Palette.White : Palette.Grey);
            }

            this.SetGlyph(player.X, player.Y, player.Character, player.Color);

            this.DrawLine(new Point(0, this.dungeon.Height - 2), new Point(this.dungeon.Width, this.dungeon.Height - 2), null, Palette.BlackAlmost, ' ');
            this.DrawLine(new Point(0, this.dungeon.Height - 1), new Point(this.dungeon.Width, this.dungeon.Height - 1), null, Palette.BlackAlmost, ' ');
            this.DrawHealthIndicators();
            this.Print(0, this.dungeon.Height - 1, this.LatestMessage, Palette.White);
            this.Print(this.dungeon.Width - 4, this.dungeon.Height - 2, $"B{this.currentFloorNum}", Palette.White);
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

            this.Print(1, this.dungeon.Height - 2, message, Palette.White);
        }
    }
}