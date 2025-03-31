namespace CdrAuthServer.Infrastructure.Models
{
    public class CdrSwaggerOptions
    {
        public string SwaggerTitle { get; set; } = string.Empty;

        public bool IncludeAuthentication { get; set; } = true;

        public string VersionedApiGroupNameFormat { get; set; } = Constants.Versioning.GroupNameFormat; // default for group name format
    }
}
