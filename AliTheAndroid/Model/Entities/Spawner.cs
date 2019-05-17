using DeenGames.AliTheAndroid.Enums;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class Spawner : Entity
    {
        public Spawner(string name, char character, Color color, int x, int y, int health, int strength, int defense, int visionRange = 5) : base(name, character, color, x, y, health, strength, defense, visionRange)
        {
            // They're just a distinct type. Eggs hatch and handle all that logic.
        }
    }
}