namespace CdrAuthServer.Validation
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }

        public string? Error { get; set; }

        public string? ErrorDescription { get; set; }

        public int? StatusCode { get; set; }

        public ValidationResult(bool isValid)
        {
            this.IsValid = isValid;
        }

        public ValidationResult(bool isValid, string error, string? errorDescription)
        {
            this.IsValid = isValid;
            this.Error = error;
            this.ErrorDescription = errorDescription;
        }

        public ValidationResult(bool isValid, string error, string? errorDescription, int? statusCode)
        {
            this.IsValid = isValid;
            this.Error = error;
            this.ErrorDescription = errorDescription;
            this.StatusCode = statusCode;
        }

        public static ValidationResult Pass(int statusCode = 200)
        {
            return new ValidationResult(true) { StatusCode = statusCode };
        }

        public static ValidationResult Fail(string error, string? errorDescription)
        {
            return new ValidationResult(false, error, errorDescription, 400);
        }

        public static ValidationResult Fail(string error, string? errorDescription, int statusCode)
        {
            return new ValidationResult(false, error, errorDescription, statusCode);
        }

        public static ValidationResult Fail(
            string errorCode,
            List<System.ComponentModel.DataAnnotations.ValidationResult> validationResults)
        {
            // Return the first client metadata error.
            return ValidationResult.Fail(errorCode, validationResults[0].ErrorMessage);
        }
    }
}
