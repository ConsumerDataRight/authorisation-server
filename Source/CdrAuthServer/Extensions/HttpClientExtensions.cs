using Newtonsoft.Json;

namespace CdrAuthServer.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task<T?> ReadAsJson<T>(this HttpContent content)
            where T : class, new()
        {
            var contentAsString = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(contentAsString);
        }
    }
}
