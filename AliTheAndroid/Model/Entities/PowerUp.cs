using System;
using System.Text;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Loggers;
using Troschuetz.Random;
using System.Collections.Generic;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class PowerUp : AbstractEntity
    {
        // We don't want too many. Even if we don't get any, s'ok.
        public const int VisionPowerUpProbability = 10;
        public const int HealthPowerUpProbability = 30;
        public const int StrengthPowerUpProbability = 30;
        public const int DefensePowerUpProbability = 30;

        public const int TypicalHealthBoost = 50;
        public const int TypicalStrengthBoost = 5;
        public const int TypicalDefenseBoost = 5;
        public const int TypicalVisionBoost = 1;

        // TODO: character/colour should NOT be part of model!
        // No longer used.
        internal const char DisplayCharacter = (char)175; // »
        internal static readonly Dictionary<string, char> Characters = new Dictionary<string, char>() {
            { "Health", 'P'},
            { "Strength", 'Q'},
            { "Defense", 'R'},
            { "Vision", 'S'},
        };

        public int HealthBoost { get; private set; }
        public int StrengthBoost { get; private set; }
        public int DefenseBoost { get; private set; }
        public int VisionBoost { get; private set; }
        public PowerUp PairedTo { get; private set; }
        public bool IsBacktrackingPowerUp { get; private set; }

        private Action onPickUp { get; set; }

        public static PowerUp Generate(IGenerator generator)
        {
            var buckets = new Dictionary<string, int>();
            buckets["Vision"] = VisionPowerUpProbability;
            buckets["Health"] = VisionPowerUpProbability + HealthPowerUpProbability;
            buckets["Strength"] = VisionPowerUpProbability + HealthPowerUpProbability + StrengthPowerUpProbability;
            buckets["Defense"] = VisionPowerUpProbability + HealthPowerUpProbability + StrengthPowerUpProbability + DefensePowerUpProbability;

            var next = generator.Next(VisionPowerUpProbability + HealthPowerUpProbability + StrengthPowerUpProbability + DefensePowerUpProbability);
            
            if (next <= buckets["Vision"])
            {
                return new PowerUp(0, 0, Characters["Vision"], visionBoost: TypicalVisionBoost);
            }
            else if (next > buckets["Vision"] && next <= buckets["Health"])
            {
                return new PowerUp(0, 0, Characters["Health"], healthBoost: TypicalHealthBoost);
            }
            else if (next > buckets["Health"] && next <= buckets["Strength"])
            {
                return new PowerUp(0, 0, Characters["Strength"], strengthBoost: TypicalStrengthBoost);
            }
            else if (next > buckets["Strength"] && next <= buckets["Defense"])
            {
                return new PowerUp(0, 0, Characters["Defense"], defenseBoost: TypicalDefenseBoost);
            }
            else
            {
                throw new InvalidOperationException("Failed to generate appropriate power-up. Please report this bug.");
            }
        }

        public static void Pair(PowerUp p1, PowerUp p2)
        {
            p1.PairedTo = p2;
            p2.PairedTo = p1;
        }

        public PowerUp(int x, int y, char character = DisplayCharacter, bool isBacktrackingPowerUp = false, int healthBoost = 0, int strengthBoost = 0, int defenseBoost = 0, int visionBoost = 0, PowerUp pairedTo = null)
        : base(x, y, DisplayCharacter, Palette.White)
        {
            this.Character = character;
            this.HealthBoost = healthBoost;
            this.StrengthBoost = strengthBoost;
            this.DefenseBoost = defenseBoost;
            this.VisionBoost = visionBoost;
            this.IsBacktrackingPowerUp = isBacktrackingPowerUp;

            // Serializer passes in this value, production code doesn't. Sometimes. (Not for both paired entities.)
            this.PairedTo = pairedTo;
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
            LastGameLogger.Instance.Log($"Picked up a {(this.IsBacktrackingPowerUp ? "Backtracking" : "")} power-up: {this.Message}");
            
            if (this.onPickUp != null)
            {
                this.onPickUp.Invoke();
            }
        }
    }
}