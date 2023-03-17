using CdrAuthServer.Infrastructure;
using Newtonsoft.Json;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Models
{
    public class CdsErrorList
    {
        [JsonProperty("errors")]
        public List<CdsError> Errors { get; set; }

        public CdsErrorList()
        {
            this.Errors = new List<CdsError>();
        }

        public CdsErrorList(CdsError error)
        {
            this.Errors = new List<CdsError>() { error };
        }

        public CdsErrorList(string errorCode, string errorTitle, string errorDetail)
        {
            var error = new CdsError(errorCode, errorTitle, errorDetail);
            this.Errors = new List<CdsError>() { error };
        }

        public static CdsErrorList MissingRequiredField(string fieldName)
        {
            var error = new CdsError(ErrorCodes.MissingRequiredField, "Missing Required Field", fieldName);
            return new CdsErrorList(error);
        }

        public static CdsErrorList InvalidField(string fieldName)
        {
            var error = new CdsError(ErrorCodes.InvalidField, "Invalid Field", fieldName);
            return new CdsErrorList(error);
        }

        public static CdsErrorList InvalidConsentArrangement(string cdrArrangementId)
        {
            var error = new CdsError(ErrorCodes.InvalidConsentArrangement, "Invalid Consent Arrangement", cdrArrangementId);
            return new CdsErrorList(error);
        }

    }
}
