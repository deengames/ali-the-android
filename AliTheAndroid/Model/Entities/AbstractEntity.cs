using System;
using System.Collections.Generic;
using DeenGames.AliTheAndroid.Enums;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    /// <summary>
    /// The most basic form of an entity: coordinates, and a visual representation (character/colour).
    /// </summary>
    public class AbstractEntity
    {
        // TODO: character/colour should NOT be part of the model!!!!
        public Color Color { get; set; }
        public char Character { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        internal static readonly Dictionary<string, char> WallCharacter = new Dictionary<string, char>
        {
            { "ascii", '#' }, { "solid",(char)219 } // 219 = █
        };
        
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
                    var chasmCharacter = Options.DisplayOldStyleAsciiCharacters ? ' ' : '.';
                    return new AbstractEntity(x, y, chasmCharacter, Palette.BlackAlmost);
                case SimpleEntity.Wall:
                    // Value is determined at draw-time so options auto-change everything
                    return new AbstractEntity(x, y, 'X', Palette.Grey);
                case SimpleEntity.QuantumPlasma:
                    return new AbstractEntity(x, y, WallCharacter["solid"], Palette.White);
                default:
                    throw new ArgumentException($"Not sure how to create a '{type}' entity");
            }
        }
    }
}