using System;
using Microsoft.Xna.Framework;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Model.Events;
using System.Collections.Generic;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    /// <summary>
    /// A living, breathing entity.
    /// </summary>
    public class Entity : AbstractEntity
    {
        public static Dictionary<string, Color> MonsterColours = new Dictionary<string, Color>()
        {
            // I hope these are by reference, and update when CurrentPalette updates.
            { "Egg", Options.CurrentPalette.Monster3Colour },
            { "Fuseling", Options.CurrentPalette.Monster1Colour },
            { "Slink", Options.CurrentPalette.Monster2Colour },
            { "TenLegs", Options.CurrentPalette.Monster3Colour },
            { "Zug", Options.CurrentPalette.Monster4Colour },
            { "The Ameer", Options.CurrentPalette.Monster4Colour },
        };
        
        public string Name { get; set; }
        public int CurrentHealth { get; set; }
        public int TotalHealth { get; set; }
        public int Strength { get; set; }
        public int Defense { get; set; }

        public int VisionRange { get; set; }
        public virtual bool CanMove { get; set; } = true; // False for fake walls

        public static Entity CreateFromTemplate(string name, int x, int y)
        {
            // This code makes me cry.
            // Also: colour is ignored and derived at runtime from the current palette.
            switch (name.ToLower()) {
                case "egg": return new Egg(x, y, Options.CurrentPalette.Monster3Colour);
                // Regular enemy. Takes a bit of skill to kill.
                case "fuseling": return new Entity("Fuseling", 'f', Options.CurrentPalette.Monster1Colour, x, y, 140, 45, 4);
                // Fodder. Generates in big groups, though.
                case "slink": return new Entity("Slink", 's', Options.CurrentPalette.Monster2Colour, x, y, 75, 55, 2);
                // Spawner. Tough, and lays eggs frequently.
                case "tenlegs": return new Spawner("TenLegs", 't', Options.CurrentPalette.Monster3Colour, x, y, 300, 65, 5);
                // Tank. REALLY hard to kill.
                case "zug":  return new Entity("Zug", 'z', Options.CurrentPalette.Monster4Colour, x, y, 450, 85, 8);
                default: throw new ArgumentException($"Not sure how to create a {name} template entity");
            }
        }

        public static void ResetPalette()
        {
            Entity.MonsterColours = new Dictionary<string, Color>()
            {
                // I hope these are by reference, and update when CurrentPalette updates.
                { "Egg", Options.CurrentPalette.Monster3Colour },
                { "Fuseling", Options.CurrentPalette.Monster1Colour },
                { "Slink", Options.CurrentPalette.Monster2Colour },
                { "TenLegs", Options.CurrentPalette.Monster3Colour },
                { "Zug", Options.CurrentPalette.Monster4Colour },
                { "The Ameer", Options.CurrentPalette.Monster4Colour },
            };
        }
        
        public Entity(string name, char character, Color color, int x, int y, int health, int strength, int defense, int visionRange = 5)
        : base(x, y, character, color)
        {
            this.Name = name;
            this.CurrentHealth = health;
            this.TotalHealth = health;
            this.Strength = strength;
            this.Defense = defense;
            this.VisionRange = visionRange;
        }

        public virtual void Die()
        {
            this.CurrentHealth = 0;
            this.Character = '%';
            this.Color = Palette.DarkBurgandyPurple;
        }

        public virtual void Damage(int damage, Weapon source)
        {
            if (damage < 0) 
            {
                return;
            }

            this.CurrentHealth -= damage;

            if (this.CurrentHealth <= 0)
            {
                EventBus.Instance.Broadcast(GameEvent.EntityDeath, this);
                this.Die();
            }
        }

        public bool IsDead { get { return this.CurrentHealth <= 0; } }
    }
}