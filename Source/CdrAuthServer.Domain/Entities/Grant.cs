namespace CdrAuthServer.Domain.Entities
{
    public class Grant
    {
        public string Key { get; set; } = string.Empty;

        public string GrantType { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public string SubjectId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public string Scope { get; set; } = string.Empty;

        public virtual IDictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }
}
