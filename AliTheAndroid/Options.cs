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
        public static int SoundEffectsVolume = 100; // 0-100%

        public static bool DeleteSaveGameOnDeath = true;

        // Sorted because the order matters when we display it for key-rebinding
        public static SortedDictionary<GameAction, Key> KeyBindings = new SortedDictionary<GameAction, Key>()
        {
            { GameAction.MoveUp, Key.W },
            { GameAction.MoveLeft, Key.A },
            { GameAction.MoveDown, Key.S },
            { GameAction.MoveRight, Key.D },
            { GameAction.TurnCounterClockWise, Key.Q },
            { GameAction.TurnClockWise, Key.E },
            { GameAction.Fire, Key.F },

            { GameAction.SelectBlaster, Key.NumPad1 },
            { GameAction.SelectMiniMissile, Key.NumPad2 },
            { GameAction.SelectZapper, Key.NumPad3 },
            { GameAction.SelectGravityCannon, Key.NumPad4 },
            { GameAction.SelectPlasmaCannon, Key.NumPad5 },
            { GameAction.SelectTeleporter, Key.T },

            { GameAction.DescendStairs, Key.X },
            { GameAction.AscendStairs, Key.Z },
            { GameAction.OpenMenu, Key.Escape },
            { GameAction.SkipTurn, Key.Space },
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