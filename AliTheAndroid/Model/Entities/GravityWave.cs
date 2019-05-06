using DeenGames.AliTheAndroid.Prototype.Enums;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class GravityWave : AbstractEntity
    {
        private const char GravityCharacter = (char)247;

        public GravityWave(int x, int y) : base(x, y, GravityCharacter, Palette.LightLilacPink)
        {
            // TODO: add code to perturb anything alive that steps on us
        }
    }
}