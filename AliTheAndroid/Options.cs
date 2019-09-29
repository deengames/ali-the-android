using System.Collections.Generic;
using DeenGames.AliTheAndroid.Accessibility;
using DeenGames.AliTheAndroid.Infrastructure.Common;

namespace DeenGames.AliTheAndroid
{
    public static class Options
    {
        internal static string FileName = "Options.txt";
        
        // Options
        public static SelectablePalette CurrentPalette = SelectablePalette.StandardPalette;
        public static bool IsFullScreen = false;
        public static int EffectsDelayMultiplier = 1;
        public static int SoundEffectsVolume = 100; // 0-100%
        public static float GlobalSfxVolumeNerf = 0.55f; // SFX are really loud. Nerf it so 100% is a nice loudness.
        public static bool DeleteSaveGameOnDeath = true;

        // Set on B10
        public static bool EnableOmniSight = true;

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

        // Manual testing stuff
        public const bool StartWithAllWeapons = true;
        public const bool CanUseStairsFromAnywhere = true;
        public const bool PlayerStartsWithAllDataCubes = false;
        internal const int MaxEffectsDelayMultiplier = 4; // 4x
    }
}