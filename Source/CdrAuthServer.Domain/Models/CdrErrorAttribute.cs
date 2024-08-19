namespace CdrAuthServer.Domain.Models
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class CdrErrorAttribute : Attribute
    {
        public CdrErrorAttribute(string title, string errorCode)
        {
            Title = title;
            ErrorCode = errorCode;
        }

        public string Title { get; set; }

        public string ErrorCode { get; set; }
    }
}
