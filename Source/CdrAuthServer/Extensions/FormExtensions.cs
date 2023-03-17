namespace CdrAuthServer.Extensions
{
    public static class FormExtensions
    {
        public static string? GetFormFieldValue(this HttpRequest request, string field)
        {
            if (request.Form == null)
            {
                return null;
            }

            return request.Form[field];
        }
    }
}
