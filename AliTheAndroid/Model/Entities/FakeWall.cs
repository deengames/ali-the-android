using DeenGames.AliTheAndroid.Enums;
using Microsoft.Xna.Framework;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class FakeWall : AbstractEntity
    {
        public static readonly Color Colour = Palette.Brown;
        public bool IsBacktrackingWall { get; private set; } = false;

        // Values duplicated in AbstractEntity.Create
        public FakeWall(int x, int y, bool isBacktrackingWall = false)
            : base(x, y, Options.DisplayTerrainAsSolid ? AbstractEntity.WallCharacter["solid"] : AbstractEntity.WallCharacter["ascii"], Palette.Grey)
        {
            this.IsBacktrackingWall = isBacktrackingWall;
        }
    }
}