using DeenGames.AliTheAndroid.Enums;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public  class DataCube : AbstractEntity
    {
        public static readonly Color[] DisplayColors = new Color[] { Palette.White, Palette.Cyan, Palette.Blue };
        private const char DisplayCharacter = (char)240; // ≡


        public int FloorNumber { get; private set; } // 5 => B5
        public string Text { get; private set; }
        public bool IsRead { get; set; }

        public DataCube(int x, int y, int floorNumber, string text) : base(x, y, DisplayCharacter, Palette.White)
        {
            this.FloorNumber = floorNumber;
            this.Text = text;
        }
    }
}