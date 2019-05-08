using System;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using Microsoft.Xna.Framework.Input;

namespace DeenGames.AliTheAndroid.Infrastructure.Sad
{
    public class SadKeyboard : IKeyboard
    {
        public bool IsKeyPressed(Key key)
        {
            var sadKey = (Keys)Enum.Parse(typeof(Keys), key.ToString());
            return SadConsole.Global.KeyboardState.IsKeyPressed(sadKey);
        }
    }
}