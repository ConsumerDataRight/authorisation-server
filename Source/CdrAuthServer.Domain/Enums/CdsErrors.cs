using CdrAuthServer.Domain.Models;

namespace CdrAuthServer.Domain.Enums
{
    /// <summary>
    /// This has been taken directly from common code used in RAAP. As these definitions should directly match CDS standards, it should be appropriate in this enum CdsError
    /// </summary>
    public enum CdsError
    {
        [CdrError(Constants.ErrorTitles.ExpectedError, Constants.ErrorCodes.Cds.ExpectedError)]
        ExpectedError,

        [CdrError(Constants.ErrorTitles.UnexpectedError, Constants.ErrorCodes.Cds.UnexpectedError)]
        UnexpectedError,

        [CdrError(Constants.ErrorTitles.ServiceUnavailable, Constants.ErrorCodes.Cds.ServiceUnavailable)]
        ServiceUnavailable,

        [CdrError(Constants.ErrorTitles.MissingRequiredField, Constants.ErrorCodes.Cds.MissingRequiredField)]
        MissingRequiredField,

        [CdrError(Constants.ErrorTitles.MissingRequiredHeader, Constants.ErrorCodes.Cds.MissingRequiredHeader)]
        MissingRequiredHeader,

        [CdrError(Constants.ErrorTitles.InvalidField, Constants.ErrorCodes.Cds.InvalidField)]
        InvalidField,

        [CdrError(Constants.ErrorTitles.InvalidHeader, Constants.ErrorCodes.Cds.InvalidHeader)]
        InvalidHeader,
        /// <summary>
        /// The InvalidDate error applies to any invalid DateString, TimeString or DateTimeString, which is why the string values don't match exactly
        /// </summary>
        [CdrError(Constants.ErrorTitles.InvalidDate, Constants.ErrorCodes.Cds.InvalidDateTime)]
        InvalidDate,

        [CdrError(Constants.ErrorTitles.InvalidPageSize, Constants.ErrorCodes.Cds.InvalidPageSize)]
        InvalidPageSize,

        [CdrError(Constants.ErrorTitles.InvalidVersion, Constants.ErrorCodes.Cds.InvalidVersion)]
        InvalidVersion,

        [CdrError(Constants.ErrorTitles.ADRStatusNotActive, Constants.ErrorCodes.Cds.AdrStatusNotActive)]
        ADRStatusIsNotActive,

        [CdrError(Constants.ErrorTitles.RevokedConsent, Constants.ErrorCodes.Cds.RevokedConsent)]
        ConsentIsRevoked,

        [CdrError(Constants.ErrorTitles.InvalidConsent, Constants.ErrorCodes.Cds.InvalidConsent)]
        ConsentIsInvalid,

        [CdrError(Constants.ErrorTitles.ResourceNotImplemented, Constants.ErrorCodes.Cds.ResourceNotImplemented)]
        ResourceNotImplemented,

        [CdrError(Constants.ErrorTitles.ResourceNotFound, Constants.ErrorCodes.Cds.ResourceNotFound)]
        ResourceNotFound,

        [CdrError(Constants.ErrorTitles.UnsupportedVersion, Constants.ErrorCodes.Cds.UnsupportedVersion)]
        UnsupportedVersion,

        [CdrError(Constants.ErrorTitles.InvalidConsentArrangement, Constants.ErrorCodes.Cds.InvalidConsentArrangement)]
        InvalidConsentArrangement,

        [CdrError(Constants.ErrorTitles.InvalidPage, Constants.ErrorCodes.Cds.InvalidPage)]
        InvalidPage,

        [CdrError(Constants.ErrorTitles.InvalidResource, Constants.ErrorCodes.Cds.InvalidResource)]
        InvalidResource,

        [CdrError(Constants.ErrorTitles.UnavailableResource, Constants.ErrorCodes.Cds.UnavailableResource)]
        UnavailableResource,

        [CdrError(Constants.ErrorTitles.InvalidBrand, Constants.ErrorCodes.Cds.InvalidBrand)]
        InvalidBrand,

        [CdrError(Constants.ErrorTitles.InvalidIndustry, Constants.ErrorCodes.Cds.InvalidIndustry)]
        InvalidIndustry,

        [CdrError(Constants.ErrorTitles.InvalidSoftwareProduct, Constants.ErrorCodes.Cds.InvalidSoftwareProduct)]
        InvalidSoftwareProduct,
    }
}
