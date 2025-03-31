namespace CdrAuthServer.Models
{
    public class Customer
    {
        public string LoginId { get; set; } = string.Empty;

        public Person? Person { get; set; }
    }
}
