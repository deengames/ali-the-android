using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Model.Events;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    // The one and only final boss. He's invulnerable (insta-heals each turn).
    public class Ameer : Entity
    {
        private const int StunRoundsPerZap = 3;

        // Same colour as Zug or strongest monster
        public bool IsStunned { get { return this.turnsLeftStunned > 0; } }

        internal int turnsLeftStunned = 0;

        // Colour is ignored and derived at runtime from current palette
        public Ameer() : base("The Ameer", '@', Options.CurrentPalette.Monster4Colour, 0, 0, 999, 30, 5, 4)
        {
        }

        override public void Damage(int damage, Weapon source)
        {
            switch (source)
            {
                case Weapon.QuantumPlasma:
                    this.CurrentHealth = 0;
                    base.Damage(0, source); // Broadcast death event
                    break;
                case Weapon.Zapper:
                    this.turnsLeftStunned += StunRoundsPerZap + 1; // +1 because current round expires
                    this.Color = Palette.Cyan;
                    EventBus.Instance.Broadcast(GameEvent.AmeerStunned);
                    break;
            }
        }

        override public bool CanMove { get { 
            return !this.IsStunned;
        } }

        public void OnPlayerMoved()
        {
            if (this.IsStunned)
            {
                this.turnsLeftStunned--;
                if (!this.IsStunned)
                {
                    this.Color = Options.CurrentPalette.Monster4Colour;
                }
            }
        }
    }
}