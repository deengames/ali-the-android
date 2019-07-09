using DeenGames.AliTheAndroid.Accessibility.Colour;

namespace DeenGames.AliTheAndroid
{
    public static class Options
    {
        public const int MonsterMultiplier = 1; // 0, 1x, 10x, 20x more monsters?
        public const bool ShowFakeWalls = true; // Should always be true
        public static bool DisplayTerrainAsSolid = true;
        // TODO: turn into enum. either Standard or Saturated.
        public static SelectablePalette CurrentPalette = SelectablePalette.SaturatedPalette;

        // Debug stuff
        public const bool EnableOmniSight = true;
        public const bool CanUseStairsFromAnywhere = true;
        public const bool PlayerHasAllDataCubes = false;        
    }
}