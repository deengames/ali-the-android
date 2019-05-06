using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Entities
{
    /// <summary>
    /// The most basic form of an entity: coordinates, and a visual representation (character/colour).
    /// </summary>
    public class AbstractEntity {
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
    }
}