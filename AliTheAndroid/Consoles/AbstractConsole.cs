using System;
using System.Linq;

namespace DeenGames.AliTheAndroid.Consoles
{
    /// A man of many talents. For example, doesn't accept input within ~0.25s of creation, so that
    // inputs don't incorrectly chain from the previous input-processing thing to us.
    public abstract class AbstractConsole : SadConsole.Console
    {
        // Press escape. It spwans and despawns a menu really fast. keyboard.Clear() isn't enough.
        // So, wait for a limited amount of time, after spawning, before we allow despawning.
        private const double SecondsAfterCreationBeforeInputWorks = 0.25;
        private DateTime createdOn;

        public AbstractConsole(int width, int height) : base(width, height)
        {
            this.createdOn = DateTime.Now;
            this.RemoveFpsCounter();
        }

        protected bool ShouldProcessInput()
        {
            return (DateTime.Now - this.createdOn).TotalSeconds >= SecondsAfterCreationBeforeInputWorks;
        }

        private void RemoveFpsCounter()
        {
            // Remove FPS counter
            var fpsCounter = SadConsole.Game.Instance.Components.SingleOrDefault(c => c is SadConsole.Game.FPSCounterComponent);
            if (fpsCounter != null)
            {
                SadConsole.Game.Instance.Components.Remove(fpsCounter);
            }
        }
    }
}