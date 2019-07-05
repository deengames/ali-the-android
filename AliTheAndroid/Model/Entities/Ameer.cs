using DeenGames.AliTheAndroid.Enums;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    // The one and only final boss. He's invulnerable (insta-heals each turn).
    public class Ameer : Entity
    {
        public Ameer() : base("The Ameer", '@', Palette.LightRed, 0, 0, 50, 70, 50, 4)
        {
        }

        override public void Damage(int damage)
        {
            // He laughs at your puny efforts.
        }
    }
}