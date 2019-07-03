using System;
using System.Collections.Generic;
using System.Linq;
using DeenGames.AliTheAndroid.Infrastructure.Common;
using Microsoft.Xna.Framework.Input;
using SadConsole.Input;

namespace DeenGames.AliTheAndroid.Infrastructure.Sad
{
    public class SadKeyboard : IKeyboard
    {
        public bool IsKeyPressed(Key key)
        {
            var sadKey = (Keys)Enum.Parse(typeof(Keys), key.ToString());
            return SadConsole.Global.KeyboardState.IsKeyPressed(sadKey);
        }

        public List<Key> GetKeysReleased()
        {
            var keys = SadConsole.Global.KeyboardState.KeysReleased;
            var toReturn = new List<Key>();

            // I hate you.
            // Reflection, y u no work elegantly here?
            if (keys.Contains(AsciiKey.Get(Keys.NumPad1)))
            {
                toReturn.Add(Key.NumPad1);
            }
            if (keys.Contains(AsciiKey.Get(Keys.NumPad2)))
            {
                toReturn.Add(Key.NumPad2);
            }
            if (keys.Contains(AsciiKey.Get(Keys.NumPad3)))
            {
                toReturn.Add(Key.NumPad3);
            }
            if (keys.Contains(AsciiKey.Get(Keys.NumPad4)))
            {
                toReturn.Add(Key.NumPad4);
            }
            if (keys.Contains(AsciiKey.Get(Keys.NumPad5)))
            {
                toReturn.Add(Key.NumPad5);
            }
            if (keys.Contains(AsciiKey.Get(Keys.NumPad6)))
            {
                toReturn.Add(Key.NumPad6);
            }
            if (keys.Contains(AsciiKey.Get(Keys.NumPad7)))
            {
                toReturn.Add(Key.NumPad7);
            }
            if (keys.Contains(AsciiKey.Get(Keys.NumPad8)))
            {
                toReturn.Add(Key.NumPad8);
            }
            if (keys.Contains(AsciiKey.Get(Keys.NumPad9)))
            {
                toReturn.Add(Key.NumPad9);
            }
            

            return toReturn;
        }

        public void Clear()
        {
            SadConsole.Global.KeyboardState.Clear();
        }
    }
}