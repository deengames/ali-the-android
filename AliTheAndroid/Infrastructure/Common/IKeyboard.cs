using System.Collections.Generic;

namespace DeenGames.AliTheAndroid.Infrastructure.Common
{
    public interface IKeyboard
    {
        bool IsKeyPressed(Key key);
        List<Key> GetKeysReleased();
        void Clear();
    }
}