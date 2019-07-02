using System;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model.Events;
using Ninject;

namespace DeenGames.AliTheAndroid.Consoles
{
    public class InGameSubMenuConsole : SadConsole.Console
    {
        internal static bool IsOpen = false;

        // Press escape. It spwans and despawns a menu really fast. keyboard.Clear() isn't enough.
        // So, wait for a limited amount of time.
        private const double SecondsAfterCreationBeforeInputWorks = 0.25;
        private const int DefaultWidth = 35;
        private const int DefaultHeight = 20;
        private readonly SadConsole.Cell BorderCell = new SadConsole.Cell(Palette.White, Palette.White, ' ');        
        private IKeyboard keyboard;
        private DateTime createdOn;

        public InGameSubMenuConsole() : base(DefaultWidth, DefaultHeight)
        {
            this.keyboard = DependencyInjection.kernel.Get<IKeyboard>();
            this.keyboard.Clear();
            this.createdOn = DateTime.Now;
            InGameSubMenuConsole.IsOpen = true;
        }

        override public void Update(System.TimeSpan delta)
        {
            this.RedrawEverything();            

            if ((DateTime.Now - this.createdOn).TotalSeconds >= SecondsAfterCreationBeforeInputWorks && this.keyboard.IsKeyPressed(Key.Escape))
            {
                EventBus.Instance.Broadcast(GameEvent.HideSubMenu, this);
            }
        }

        private void RedrawEverything()
        {
            this.Fill(Palette.BlackAlmost, Palette.BlackAlmost, ' ');
            this.DrawBox(new Microsoft.Xna.Framework.Rectangle(0, 0, this.Width, this.Height), BorderCell);
        }
    }
}