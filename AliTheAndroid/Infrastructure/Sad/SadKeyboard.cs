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
        private static Dictionary<string, Key> keyMappings = new Dictionary<string, Key>();

        static SadKeyboard() {
            var validKeys = Enum.GetNames(typeof(Key));
            foreach (var name in validKeys)
            {
                var asKey = (Key)Enum.Parse(typeof(Key), name);
                keyMappings[name] = asKey;
            }
        }
        public bool IsKeyPressed(Key key)
        {
            var sadKey = (Keys)Enum.Parse(typeof(Keys), key.ToString());
            return SadConsole.Global.KeyboardState.IsKeyPressed(sadKey);
        }

        public void Clear()
        {
            SadConsole.Global.KeyboardState.Clear();
        }

        public IEnumerable<Key> GetKeysPressed()
        {
            // Convert AsciiKey to Key. Gets nulls for unmapped values, so filter those out.
            // Requires converting from Key? to Key.
            return SadConsole.Global.KeyboardState.KeysPressed.Select(s => AsciiKeyToCommonKey(s))
                .Where(t => t.HasValue).Select(u => u.Value);
        }

        private static Key? AsciiKeyToCommonKey(AsciiKey key)
        {
            object toReturn = null;
            // Simple characters like A/S/D/F
            if (Enum.TryParse(typeof(Key), key.Character.ToString(), true, out toReturn))
            {
                return (Key)toReturn;
            }
            // Complex keys like . (OemPeriod)
            else if (Enum.TryParse(typeof(Key), key.Key.ToString(), true, out toReturn))
            {
                return (Key)toReturn;
            }
            else
            {
                return null;
            }
        }
    }
}