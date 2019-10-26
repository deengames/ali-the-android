using System;
using System.IO;
using DeenGames.AliTheAndroid.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DeenGames.AliTheAndroid.Consoles
{
    class SplashConsole : AbstractConsole
    {
        private Texture2D texture;
        private Vector2 position;
        private DateTime startedOn;
        
        public SplashConsole(int width, int height) : base(width, height)
        {
            FileStream fileStream = new FileStream("Content/dg-logo.png", FileMode.Open);
            this.texture = Texture2D.FromStream(SadConsole.Global.GraphicsDevice, fileStream);
            fileStream.Dispose();

            this.position = new Vector2(
                (SadConsole.Global.WindowWidth - texture.Width) / 2,
                (SadConsole.Global.WindowHeight - texture.Height) / 2);

            startedOn = DateTime.Now;
            AudioManager.Instance.Play("Nightingale");
        }

        override public void Update(TimeSpan delta)
        {
            if ((DateTime.Now - startedOn).TotalSeconds >= 4.5)
            {
                SadConsole.Global.CurrentScreen = new TitleConsole(this.Width, this.Height);
            }
        }

        override public void Draw(System.TimeSpan timeElapsed)
        {
            base.Draw(timeElapsed);
            var drawCall = new SadConsole.DrawCalls.DrawCallTexture(texture, position);
            SadConsole.Global.DrawCalls.Add(drawCall);            
        }
    }
}