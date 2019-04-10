using DeenGames.AliTheAndroid.Prototype;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;

namespace DeenGames.AliTheAndroid
{
    public static class Program
    {
        // TODO: for production, use 120x50, it's a nice, modern size.
        private const int GameWidthInTiles = 80;
        private const int GameHeightInTiles = 34;

        [STAThread]
        static void Main()
        {
            //SadConsole.Settings.UnlimitedFPS = true;
            //SadConsole.Settings.UseHardwareFullScreen = true;

            // Setup the engine and creat the main window.
            // 120x50
            SadConsole.Game.Create("Fonts/IBM.font", GameWidthInTiles, GameHeightInTiles);
            
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
        
        private static void Init()
        {
            // Any setup
            SadConsole.Game.Instance.Components.Add(new SadConsole.Game.FPSCounterComponent(SadConsole.Game.Instance));

            SadConsole.Game.Instance.Window.Title = "DemoProject OpenGL";

            // By default SadConsole adds a blank ready-to-go console to the rendering system. 
            // We don't want to use that for the sample project so we'll remove it.

            //Global.MouseState.ProcessMouseWhenOffScreen = true;

            // We'll instead use our demo consoles that show various features of SadConsole.
            Global.CurrentScreen = new SadConsole.ScreenObject();

            // Initialize the windows
            // Global.CurrentScreen.Children.Add(new PrototypeGameConsole(GameWidthInTiles, GameHeightInTiles));
            Global.CurrentScreen.Children.Add(new MainGameConsole(GameWidthInTiles, GameHeightInTiles));
        }

        private static void Update(GameTime time)
        {
            // Global updates. Be vevy vevy careful about adding things here. Only truely global stuff should go here.
        }
    }
}
