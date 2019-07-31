using System.Collections.Generic;
using DeenGames.AliTheAndroid.Infrastructure.Common;

namespace DeenGames.AliTheAndroid.Tests.Helpers
{
    public class DeadKeyboard : IKeyboard
    {
        public bool IsKeyPressed(Key key)
        {
            return false;
        }

        public void Clear() { }

        public IEnumerable<Key> GetKeysPressed()
        {
            return new Key[0];
        }
    }
}