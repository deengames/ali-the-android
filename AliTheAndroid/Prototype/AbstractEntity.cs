using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Prototype
{
    /// <summary>
    /// Stuff common to all entities. Like x/y/char/colour, etc.
    /// </summary>
    public class AbstractEntity {
        public Color Color { get; set; }
        public char Character { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        // Used to prevent things being teleported twice or more in one turn, since we land on the destination teleporter pad.
        
        public AbstractEntity(int x, int y, char character, Color color) : this(character, color)
        {
            this.X = x;
            this.Y = y;
        }

        public AbstractEntity(char character, Color color)
        {
            this.Character = character;
            this.Color = color;
        }
    }
}