namespace CdrAuthServer.Models
{
    public record ArrangeRevocationRequest
    {
        public string? Body { get; set; }

        public string? Headers { get; set; }

        public string? Url { get; set; }

        public string? Method { get; set; }

        public string? ContentType { get; set; }
    }
}
