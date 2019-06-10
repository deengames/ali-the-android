using DeenGames.AliTheAndroid.Enums;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class FakeWall : AbstractEntity
    {
        public bool IsBacktrackingWall { get; private set; } = false;

        // Values duplicated in AbstractEntity.Create
        public FakeWall(int x, int y, bool isBacktrackingWall = false) : base(x, y, '#', Palette.LightGrey)
        {
            this.IsBacktrackingWall = isBacktrackingWall;
        }
    }
}