using Newtonsoft.Json;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    /// <summary>
    /// Shield idea suggested by @peterklogborg. When out of combat, instantly
    // recharges to the maximum amount. ("Out of combat" means didn't melee-punch,
    // fire, or get hit for three consecutive turns.)
    public class KlogborgShield : Shield
    {
        private const int OutOfCombatTurns = 3;

        [JsonProperty]
        internal int MovesWithoutAttacking = 0;

        [JsonProperty]
        internal int MovesWithoutDamage = 0;

        [JsonConstructor]
        public KlogborgShield(int currentShield, int movesWithoutAttacking, int movesWithoutDamage)
        : base(currentShield)
        {
            this.MovesWithoutAttacking = movesWithoutAttacking;
            this.MovesWithoutDamage = movesWithoutDamage;
        }

        override public void OnMove()
        {
            this.MovesWithoutAttacking++;
            this.MovesWithoutDamage++;

            if (this.IsDown() && MovesWithoutAttacking >= OutOfCombatTurns && this.MovesWithoutDamage >= OutOfCombatTurns)
            {
                this.CurrentShield = Shield.MaxShield;
            }
        }

        override internal int Damage(int damage)
        {
            ////// TODO: consider this further
            // When regularly walking around, don't care to track moves without attacking until damaged.
            // Because, if we recharge now, you could take damage and regen from "3+ non-attacking" immediately.
            
            // But, say you're in mid-combat, got 2 turns without attacking, then take damage; we don't
            // want to reset MovesWithoutAttacking, because you're in combat and legit got 2 turns done.
            this.MovesWithoutDamage = 0;
            return base.Damage(damage);
        }
    }
}