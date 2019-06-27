using DeenGames.AliTheAndroid.Consoles;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Ninject;
using SadConsole;
using System;

namespace DeenGames.AliTheAndroid.Infrastructure.Sad
{
    public class SadConsoleProgram
    {
        private readonly int widthInTiles = 0;
        private readonly int heightInTiles = 0;


        public SadConsoleProgram(int widthInTiles, int heightInTiles)
        {
            this.widthInTiles = widthInTiles;
            this.heightInTiles = heightInTiles;

            DependencyInjection.kernel.Bind<IKeyboard>().To<SadKeyboard>();
        }
        
        public void Run()
        {
            //SadConsole.Settings.UnlimitedFPS = true;
            SadConsole.Settings.UseHardwareFullScreen = true;
            SadConsole.Settings.ResizeMode = Settings.WindowResizeOptions.Stretch;
            
            // Setup the engine and creat the main window.
            // 120x50
            SadConsole.Game.Create("Fonts/IBM.font", widthInTiles, heightInTiles);
            
            //SadConsole.Engine.Initialize("IBM.font", 80, 25, (g) => { g.GraphicsDeviceManager.HardwareModeSwitch = false; g.Window.AllowUserResizing = true; });

            // Hook the start event so we can add consoles to the system.
            SadConsole.Game.OnInitialize = Init;

            // Hook the update event that happens each frame so we can trap keys and respond.
            SadConsole.Game.OnUpdate = Update;

            // Hook the "after render" even though we're not using it.
            //SadConsole.Game.OnDraw = DrawFrame;

            // Start the game.
            SadConsole.Game.Instance.Run();

            //
            // Code here will not run until the game has shut down.
            //

            SadConsole.Game.Instance.Dispose();
        }
        
        private void Init()
        {
            // Any setup
            SadConsole.Game.Instance.Components.Add(new SadConsole.Game.FPSCounterComponent(SadConsole.Game.Instance));

            SadConsole.Game.Instance.Window.Title = "DemoProject OpenGL";

            // By default SadConsole adds a blank ready-to-go console to the rendering system. 
            // We don't want to use that for the sample project so we'll remove it.

            //Global.MouseState.ProcessMouseWhenOffScreen = true;

            Global.CurrentScreen = new CoreGameConsole(widthInTiles, heightInTiles);
        }

        private void Update(GameTime time)
        {
            // Global updates. Be vevy vevy careful about adding things here. Only truely global stuff should go here.
        }
    }
}
