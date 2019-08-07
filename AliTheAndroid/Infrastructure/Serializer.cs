using Newtonsoft.Json;

namespace DeenGames.AliTheAndroid.Infrastructure
{
    public static class Serializer
    {
        

        public static string Serialize(object target)
        {
            var settings = new JsonSerializerSettings() {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects
            };

            return JsonConvert.SerializeObject(target, settings);
        }

        public static T Deserialize<T>(string serialized)
        {
            return JsonConvert.DeserializeObject<T>(serialized);
        }

    }
}