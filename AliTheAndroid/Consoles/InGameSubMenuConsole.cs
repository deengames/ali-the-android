using System;
using DeenGames.AliTheAndroid.Enums;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using DeenGames.AliTheAndroid.Model.Events;
using Ninject;

namespace DeenGames.AliTheAndroid.Consoles
{
    public class InGameSubMenuConsole : SadConsole.Console
    {
        private const int DefaultWidth = 35;
        private const int DefaultHeight = 20;
        private readonly SadConsole.Cell BorderCell = new SadConsole.Cell(Palette.White);
        private IKeyboard keyboard;

        public InGameSubMenuConsole() : base(DefaultWidth, DefaultHeight)
        {
            this.keyboard = DependencyInjection.kernel.Get<IKeyboard>();
        }

        override public void Update(System.TimeSpan delta)
        {
            Console.Write("!");
            this.RedrawEverything();

            if (this.keyboard.IsKeyPressed(Key.Escape))
            {
                Console.WriteLine("IN: hide");
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