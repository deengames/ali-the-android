using System;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Events;
using DeenGames.AliTheAndroid.Prototype;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Prototype
{
    /// <summary>
    /// A living, breathing entity; NOT an ECS entity.
    /// </summary>
    public class Entity : AbstractEntity
    {
        public string Name { get; private set; }
        public int CurrentHealth { get; private set; }
        public int TotalHealth { get; }
        public int Strength { get; }
        public int Defense { get; }

        public int VisionRange { get; }

        public static Entity CreateFromTemplate(string name)
        {
            // This code makes me cry.
            switch (name.ToLower()) {
                case "alien": return new Entity("Alien", 'a', Palette.Red, 40, 8, 3);
                default: throw new InvalidOperationException($"Not sure how to create a {name} template entity");
            }
        }
        
        public Entity(string name, char character, Color color, int health, int strength, int defense, int visionRange = 5)
        : base(character, color)
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