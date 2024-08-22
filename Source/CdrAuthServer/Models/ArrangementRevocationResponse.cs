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

    public record ArrangeRevocationResponse
    {
        public string? Content { get; set; }
        public string? Headers { get; set; }
        public string? StatusCode { get; set; }
    }

    public record AdrArrangementRevocationResponse
    {
        public ArrangeRevocationRequest? ArrangeRevocationRequest { get; set; }
        public ArrangeRevocationResponse? ArrangeRevocationResponse { get; set; }
    }

}