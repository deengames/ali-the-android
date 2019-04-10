using DeenGames.AliTheAndroid.Ecs;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace DeenGames.AliTheAndroid.Ecs
{
    /// <summary>
    /// Just a simple collection of entities. You can't have dupes, though.
    /// Insert is O(1), retrieval by type is O(1).
    public class Entity
    {
		public string Name { get; protected set; }

        public Color Color { get; set; }
        public char Character { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
        
		public int VisionRange { get; protected set; } = 5;
		
        private IDictionary<Type, AbstractComponent> components = new Dictionary<Type, AbstractComponent>();

        public Entity(int x, int y, char character, Color color)
        {
            X = x;
            Y = y;
            this.Character = character;
            this.Color = color;
        }

        public void Set(AbstractComponent component)
        {
            var key = component.GetType();
            this.components[key] = component;
        }

        public AbstractComponent Get(Type componentType)
        {
            return this.components[componentType];
        }
    }
}