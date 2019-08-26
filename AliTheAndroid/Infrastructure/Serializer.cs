using System;
using Newtonsoft.Json;

namespace DeenGames.AliTheAndroid.Infrastructure
{
    public static class Serializer
    {
        internal const string SaveGameFileName = "save.dat";

        public static string Serialize(object target)
        {
            var settings = GetSerializerSettings();
            return JsonConvert.SerializeObject(target, Formatting.None, settings);
        }

        public static T Deserialize<T>(string serialized)
        {
            var settings = GetSerializerSettings();
            return JsonConvert.DeserializeObject<T>(serialized, settings);
        }
        
        private static JsonSerializerSettings GetSerializerSettings()
        {
            return new JsonSerializerSettings() {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects
            };
        }
    }
}