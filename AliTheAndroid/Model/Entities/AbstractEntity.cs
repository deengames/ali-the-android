using System;
using System.Collections.Generic;
using DeenGames.AliTheAndroid.Enums;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    /// <summary>
    /// The most basic form of an entity: coordinates, and a visual representation (character/colour).
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Entity at ({X}, {Y})")]
    public class AbstractEntity
    {
        // TODO: character/colour should NOT be part of the model!!!!
        public Color Color { get; set; }
        public char Character { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        private const char SolidWallCharacter = (char)46; // ,
        private const char QuantumPlasmaCharacter = (char)219; // █
        private const char ChasmCharacter = ' ';
        
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
                    return new AbstractEntity(x, y, ChasmCharacter, Palette.BlackAlmost);
                case SimpleEntity.Wall:
                    return new AbstractEntity(x, y, SolidWallCharacter, Palette.LightGrey);
                case SimpleEntity.QuantumPlasma:
                    return new AbstractEntity(x, y, QuantumPlasmaCharacter, Palette.White);
                default:
                    throw new ArgumentException($"Not sure how to create a '{type}' entity");
            }
        }
    }
}