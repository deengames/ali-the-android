using System;
using DeenGames.AliTheAndroid.Prototype.Enums;
using DeenGames.AliTheAndroid.Prototype;
using Microsoft.Xna.Framework;
using DeenGames.AliTheAndroid.Entities;

namespace DeenGames.AliTheAndroid.Prototype
{
    /// <summary>
    /// A living, breathing entity.
    /// </summary>
    public class Entity : AbstractEntity
    {
        public string Name { get; private set; }
        public int CurrentHealth { get; private set; }
        public int TotalHealth { get; }
        public int Strength { get; }
        public int Defense { get; }

        public int VisionRange { get; }
        public bool CanMove { get; set; } = true; // False for fake walls

        public static Entity CreateFromTemplate(string name, int x, int y)
        {
            // This code makes me cry.
            switch (name.ToLower()) {
                // Regular enemy. Takes a bit of skill to kill.
                case "alien": return new Entity("Alien", 'a', Palette.Blue, x, y, 42, 9, 4);
                // Fodder. Generates in big groups, though.
                case "slink": return new Entity("Slink", 's', Palette.DarkBlueMuted, x, y, 33, 6, 2);
                // Tank. REALLY hard to kill (I hope).
                case "zug":  return new Entity("Zug", 'z', Palette.Red, x, y, 90, 18, 8);
                default: throw new InvalidOperationException($"Not sure how to create a {name} template entity");
            }
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

        public void Die()
        {
            this.CurrentHealth = 0;
            this.Character = '%';
            this.Color = Palette.DarkBurgandyPurple;
        }

        public void Damage(int damage)
        {
            if (damage < 0) 
            {
                return;
            }

            this.CurrentHealth -= damage;

            if (this.CurrentHealth <= 0)
            {
                EventBus.Instance.Broadcast(GameEvent.EntityDeath, this);
            }
        }

        public bool IsDead { get { return this.CurrentHealth <= 0; } }
    }
}