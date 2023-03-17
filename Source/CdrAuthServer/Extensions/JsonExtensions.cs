namespace CdrAuthServer.Extensions
{
    public static class JsonExtensions
    {
        public static string ToJson(this object data)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(data);
        }

        public static IDictionary<string, string>? FromJson(this string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }
    }
}
