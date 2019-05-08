using DeenGames.AliTheAndroid.Infrastructure.Common;

namespace DeenGames.AliTheAndroid.Tests.Helpers
{
    public class DeadKeyboard : IKeyboard
    {
        public bool IsKeyPressed(Key key)
        {
            return false;
        }
    }
}