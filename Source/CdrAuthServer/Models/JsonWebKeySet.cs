namespace CdrAuthServer.Models
{
    public class JsonWebKeySet
    {
        public JsonWebKey[] keys { get; set; }
    }

    public class JsonWebKey
    {
        public string kty { get; set; } = string.Empty;
        public string use { get; set; } = string.Empty;
        public string kid { get; set; } = string.Empty;
        public string x5t { get; set; } = string.Empty;
        public string? e { get; set; }
        public string? n { get; set; }
        public IList<string> x5c { get; set; }
        public string alg { get; set; } = string.Empty;
        public string? x { get; set; }
        public string? y { get; set; }
        public string? crv { get; set; }
    }

}
