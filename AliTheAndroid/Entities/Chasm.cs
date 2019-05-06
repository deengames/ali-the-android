using DeenGames.AliTheAndroid.Prototype.Enums;

namespace DeenGames.AliTheAndroid.Entities
{
    public class Chasm : Wall
    {
        public Chasm(int x, int y) : base(x, y)
        {
            this.Character = ' ';
            this.Color = Palette.BlackAlmost;
        }
    }
}