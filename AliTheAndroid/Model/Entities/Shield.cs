using System;
using Newtonsoft.Json;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class Shield
    {
        public const int MaxShield = 100;
        private const int ShieldRegenPerMove = 1;
        
        [JsonProperty]
        internal int CurrentShield { get; private set; }

        public Shield()
        {
            this.CurrentShield = Shield.MaxShield;
        }

        [JsonConstructor]
        public Shield(int currentShield)
        {
            this.CurrentShield = currentShield;
        }

        public void OnMove()
        {
            this.IncrementallyRegenerate();
        }

        /// <summary>
        /// Tries to damage the shield by the specified amount. Returns the actual amount damaged
        /// (eg. if the shield is only 8, calling Damage(20) returns 8).
        internal int Damage(int damage)
        {
            var actualDamage = Math.Min(this.CurrentShield, damage);
            this.CurrentShield -= actualDamage;
            return actualDamage;
        }

        internal bool IsDown()
        {
            return this.CurrentShield <= 0;
        }

        private void IncrementallyRegenerate()
        {
            this.CurrentShield += Shield.ShieldRegenPerMove;
            this.CurrentShield = Math.Min(this.CurrentShield, Shield.MaxShield);
        }
    }
}