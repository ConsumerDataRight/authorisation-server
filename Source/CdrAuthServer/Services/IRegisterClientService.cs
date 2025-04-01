using CdrAuthServer.Models.Json;
using CdrAuthServer.Models.Register;

namespace CdrAuthServer.Services
{
    /// <summary>
    /// Provides functionality for interacting with CDR register.
    /// </summary>
    public interface IRegisterClientService
    {
        /// <summary>
        /// Retrieve Data Recipients from the register.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>That data recipients or <c>null</c> if the request was unsuccessful.</returns>
        Task<RegisterResponse<LegalEntity>?> GetDataRecipients(CancellationToken cancellationToken = default);
    }
}
