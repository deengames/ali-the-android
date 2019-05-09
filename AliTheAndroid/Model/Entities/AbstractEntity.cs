using System;
using DeenGames.AliTheAndroid.Enums;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    /// <summary>
    /// The most basic form of an entity: coordinates, and a visual representation (character/colour).
    /// </summary>
    public class AbstractEntity
    {
        public Color Color { get; set; }
        public char Character { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        
        public AbstractEntity(int x, int y, char character, Color color)
        {
            this.X = x;
            this.Y = y;
            this.Character = character;
            this.Color = color;
        }

        public static AbstractEntity Create(SimpleEntity type, int x, int y)
        {
            switch (type)
            {
                case SimpleEntity.Chasm:
                    return new AbstractEntity(x, y, ' ', Palette.BlackAlmost);
                case SimpleEntity.Wall:
                    // Values duplicated in FakeWall constructor
                    return new AbstractEntity(x, y, '#', Palette.LightGrey);
                default:
                    throw new ArgumentException($"Not sure how to create a '{type}' entity");
            }
        }
    }
}