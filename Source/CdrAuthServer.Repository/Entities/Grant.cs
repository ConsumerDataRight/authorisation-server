namespace CdrAuthServer.Repository.Entities
{
    public class Grant
    {
        public string Key { get; set; } = string.Empty;

        public string GrantType { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public string? SubjectId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public string? Scope { get; set; }

        public string Data { get; set; } = string.Empty;
    }
}
