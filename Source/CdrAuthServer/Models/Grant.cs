using CdrAuthServer.Extensions;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Models
{
    public class Grant
    {
        public Grant()
        {
            this.Data = new Dictionary<string, object>();
        }

        public string Key { get; set; } = string.Empty;

        public string GrantType { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public string SubjectId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public string Scope { get; set; } = string.Empty;

        public virtual IDictionary<string, object> Data { get; set; }

        public bool IsExpired
        {
            get
            {
                return DateTime.UtcNow > ExpiresAt;
            }
        }

        public object? GetDataItem(string dataItemKey)
        {
            if (Data.TryGetValue(dataItemKey, out var value))
            {
                return value;
            }

            return null;
        }

        protected void SetData(string key, object? value)
        {
            if (this.Data.ContainsKey(key))
            {
                this.Data[key] = value ?? string.Empty;
            }
            else
            {
                this.Data.Add(key, value ?? string.Empty);
            }
        }
    }
}
