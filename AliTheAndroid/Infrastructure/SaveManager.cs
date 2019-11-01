using System.IO;
using DeenGames.AliTheAndroid.Model;

namespace DeenGames.AliTheAndroid.Infrastructure
{
    public static class SaveManager
    {
        public static void SaveGame()
        {
            var dungeon = Dungeon.Instance;
            
            // Rare race condition: saving, with effects, causes deserialization error on load
            dungeon.CurrentFloor.EffectEntities.Clear();

            var serialized = Serializer.Serialize(dungeon);
            File.WriteAllText(Serializer.SaveGameFileName, serialized);
        }
    }
}