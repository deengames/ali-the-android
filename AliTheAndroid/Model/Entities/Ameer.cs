using DeenGames.AliTheAndroid.Enums;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    // The one and only final boss. He's invulnerable (insta-heals each turn).
    public class Ameer : Entity
    {
        // Same colour as Zug or strongest monster
        public bool IsStunned { get { return this.turnsLeftStunned > 0; } }

        private int turnsLeftStunned = 0;

        public Ameer() : base("The Ameer", '@', Options.CurrentPalette.Monster4Colour, 0, 0, 50, 7, 5, 4)
        {
        }

        override public void Damage(int damage, Weapon source)
        {
            if (source == Weapon.QuantumPlasma)
            {
                this.CurrentHealth = 0;
                base.Damage(0, source); // Broadcast death event
            }
        }
    }
}