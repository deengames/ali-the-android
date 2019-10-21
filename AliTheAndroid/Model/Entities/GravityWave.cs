using System;
using System.Collections.Generic;
using System.Linq;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Model.Events;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class GravityWave : AbstractEntity
    {
        public bool IsBacktrackingWave { get; private set; }

        private const char GravityCharacter = (char)247; // ≈
        
        [JsonProperty]
        // Internal for testing
        internal int FloorNum;

        public GravityWave(int x, int y, bool isBacktrackingWave, int floorNum) : base(x, y, GravityCharacter, Palette.LightLilacPink)
        {
            this.IsBacktrackingWave = isBacktrackingWave;
        }
    }
}