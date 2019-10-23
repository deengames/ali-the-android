using System.Linq;
using Newtonsoft.Json;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    /// <summary>
    /// Shield idea suggested by @peterklogborg. When out of combat, instantly
    // recharges to the maximum amount. ("Out of combat" means didn't melee-punch,
    // fire, or get hit for three consecutive turns.)
    public class KlogborgShield : Shield
    {
        override public void OnMove(GoRogue.FOV playerFov, System.Collections.Generic.IList<Entity> monsters)
        {
            if (this.CurrentShield < Shield.MaxShield && !monsters.Any(m => playerFov.BooleanFOV[m.X, m.Y] == true))
            {
                this.CurrentShield = Shield.MaxShield;
            }
        }
    }
}