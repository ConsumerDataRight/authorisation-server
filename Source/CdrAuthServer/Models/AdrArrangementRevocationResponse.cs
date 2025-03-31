namespace CdrAuthServer.Models
{
    public record AdrArrangementRevocationResponse
    {
        public ArrangeRevocationRequest? ArrangeRevocationRequest { get; set; }

        public ArrangeRevocationResponse? ArrangeRevocationResponse { get; set; }
    }
}
