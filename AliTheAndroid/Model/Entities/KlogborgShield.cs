using System.Linq;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    /// <summary>
    /// Shield idea suggested by @peterklogborg. When out of combat, instantly
    // recharges to the maximum amount. ("Out of combat" means didn't melee-punch,
    // fire, or get hit for three consecutive turns.)
    public class KlogborgShield : Shield
    {
        new public const int MaxShield = 40;

        public KlogborgShield()
        {
            this.CurrentShield = KlogborgShield.MaxShield;
        }

        override public void OnMove(GoRogue.FOV playerFov, System.Collections.Generic.IList<Entity> monsters)
        {
            if (this.CurrentShield < KlogborgShield.MaxShield && !monsters.Any(m => playerFov.BooleanFOV[m.X, m.Y] == true))
            {
                this.CurrentShield = KlogborgShield.MaxShield;
            }
        }
    }
}