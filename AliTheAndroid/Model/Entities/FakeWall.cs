using DeenGames.AliTheAndroid.Prototype.Enums;

namespace DeenGames.AliTheAndroid.Model.Entities
{
    public class FakeWall : AbstractEntity
    {
        // Values duplicated in AbstractEntity.Create
        public FakeWall(int x, int y) : base(x, y, '#', Palette.LightGrey)
        {
            // TODO: add logic to die when an exposion/missile hits us
        }
    }
}