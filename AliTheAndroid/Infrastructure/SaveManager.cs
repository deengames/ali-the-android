using System.IO;
using DeenGames.AliTheAndroid.Model;

namespace DeenGames.AliTheAndroid.Infrastructure
{
    public static class SaveManager
    {
        public static void SaveGame()
        {
            var dungeon = Dungeon.Instance;
            var serialized = Serializer.Serialize(dungeon);
            File.WriteAllText(Serializer.SaveGameFileName, serialized);
        }
    }
}