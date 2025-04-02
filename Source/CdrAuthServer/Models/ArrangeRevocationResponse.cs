namespace CdrAuthServer.Models
{
    public record ArrangeRevocationResponse
    {
        public string? Content { get; set; }

        public string? Headers { get; set; }

        public int? StatusCode { get; set; }
    }
}
