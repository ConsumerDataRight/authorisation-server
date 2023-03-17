namespace CdrAuthServer.Domain.Repositories
{
    public interface ITokenRepository
    {
        Task AddToBlacklist(string id);

        Task<bool> IsTokenBlacklisted(string id);
    }
}
