using System;
using System.Text;
using DeenGames.AliTheAndroid.Model.Entities;
using DeenGames.AliTheAndroid.Enums;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class PowerUp : AbstractEntity
    {
        public static readonly int TypicalHealthBoost = 20;
        public static readonly int TypicalStrengthBoost = 4;
        public static readonly int TypicalDefenseBoost = 3;
        public static readonly int TypicalVisionBoost = 1;

        // TODO: character/colour should NOT be part of model!
        private const char DisplayCharacter = (char)175; // »

        public static readonly Color[] DisplayColors = new Color[] { Palette.White, Palette.LightLilacPink, Palette.LilacPinkPurple, Palette.Purple };

        public int HealthBoost { get; private set; }
        public int StrengthBoost { get; private set; }
        public int DefenseBoost { get; private set; }
        public int VisionBoost { get; private set; }
        private Action onPickUp { get; set; }

        public PowerUp(int x, int y, int healthBoost = 0, int strengthBoost = 0, int defenseBoost = 0, int visionBoost = 0)
        : base(x, y, DisplayCharacter, Palette.White)
        {
            this.HealthBoost = healthBoost;
            this.StrengthBoost = strengthBoost;
            this.DefenseBoost = defenseBoost;
            this.VisionBoost = visionBoost;
        }

        public string Message { get {
            var builder = new StringBuilder();
            if (this.HealthBoost > 0)
            {
                builder.Append($"+{this.HealthBoost} health ");
            }
            if (this.StrengthBoost > 0)
            {
                builder.Append($"+{this.StrengthBoost} strength ");
            }
            if (this.DefenseBoost > 0)
            {
                builder.Append($"+{this.DefenseBoost} defense ");
            }
            if (this.VisionBoost > 0)
            {
                builder.Append($"+{this.VisionBoost} sight ");
            }
            return builder.ToString();
        }}

        public void OnPickUp(Action callback)
        {
            this.onPickUp = callback;
        }

        public void PickUp()
        {
            if (this.onPickUp != null)
            {
                this.onPickUp.Invoke();
            }
        }
    }
}