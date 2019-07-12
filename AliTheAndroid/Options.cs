using DeenGames.AliTheAndroid.Accessibility.Colour;

namespace DeenGames.AliTheAndroid
{
    public static class Options
    {
        public const bool ShowFakeWalls = true; // Should always be true
        public static bool DisplayOldStyleAsciiCharacters = false;
        public static SelectablePalette CurrentPalette = SelectablePalette.SaturatedPalette;

        // Debug stuff
        public const bool StartWithAllWeapons = true;
        public const bool EnableOmniSight = true;
        public const bool CanUseStairsFromAnywhere = true;
        public const bool PlayerStartsWithAllDataCubes = true;
        public const int MonsterMultiplier = 1; // 0, 1x, 10x, 20x more monsters?
    }
}