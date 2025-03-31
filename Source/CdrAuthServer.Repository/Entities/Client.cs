namespace CdrAuthServer.Repository.Entities
{
    public class Client
    {
        public Client()
        {
            this.ClientId = Guid.NewGuid().ToString();
        }

        public string ClientId { get; set; }

        public long ClientIdIssuedAt { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public string? ClientDescription { get; set; }

        // every Client has zero or more claims (one-to-many)
        public virtual ICollection<ClientClaims>? ClientClaims { get; set; }
    }
}
