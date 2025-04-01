namespace CdrAuthServer.Repository.Entities
{
    public class ClientClaims
    {
        public int Id { get; set; }

        // every ClientClaim belongs to exactly one Client using foreign key
        public string ClientId { get; set; } = string.Empty;

        public virtual Client Client { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}
