using System.Collections.Generic;
using DeenGames.AliTheAndroid.Accessibility;
using DeenGames.AliTheAndroid.Infrastructure.Common;

namespace DeenGames.AliTheAndroid
{
    public static class Options
    {
        internal static string FileName = "Options.txt";
        
        // Options
        public static bool DisplayOldStyleAsciiCharacters = false;
        public static SelectablePalette CurrentPalette = SelectablePalette.StandardPalette;
        public static bool IsFullScreen = false;
        public static int EffectsDelayMultiplier = 1;
        // Sorted because the order matters when we display it for key-rebinding
        public static SortedDictionary<ConfigurableControl, Key> KeyBindings = new SortedDictionary<ConfigurableControl, Key>()
        {
            { ConfigurableControl.MoveUp, Key.W },
            { ConfigurableControl.MoveLeft, Key.A },
            { ConfigurableControl.MoveDown, Key.S },
            { ConfigurableControl.MoveRight, Key.D },
            { ConfigurableControl.TurnCounterClockWise, Key.Q },
            { ConfigurableControl.TurnClockWise, Key.E },
            { ConfigurableControl.Fire, Key.F },

            { ConfigurableControl.SelectBlaster, Key.NumPad1 },
            { ConfigurableControl.SelectMiniMissile, Key.NumPad2 },
            { ConfigurableControl.SelectZapper, Key.NumPad3 },
            { ConfigurableControl.SelectGravityCannon, Key.NumPad4 },
            { ConfigurableControl.SelectPlasmaCannon, Key.NumPad5 },
            { ConfigurableControl.SelectTeleporter, Key.T },

            { ConfigurableControl.DescendStairs, Key.X },
            { ConfigurableControl.AscendStairs, Key.Z },
            { ConfigurableControl.OpenMenu, Key.Escape },
            { ConfigurableControl.SkipTurn, Key.Space },
        };

        // Constants
        public const bool ShowFakeWalls = true; // Should always be true

        // Debug stuff
        public const bool StartWithAllWeapons = false;
        public const bool EnableOmniSight = false;
        public const bool CanUseStairsFromAnywhere = false;
        public const bool PlayerStartsWithAllDataCubes = false;
        internal const int MaxEffectsDelayMultiplier = 4; // 4x
    }
}