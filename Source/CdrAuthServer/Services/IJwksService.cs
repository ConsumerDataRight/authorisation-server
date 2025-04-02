namespace CdrAuthServer.Services
{
    public interface IJwksService
    {
        Task<Microsoft.IdentityModel.Tokens.JsonWebKeySet?> GetJwks(Uri jwksUri);

        Task<Microsoft.IdentityModel.Tokens.JsonWebKeySet?> GetJwks(Uri jwksUri, string kid);
    }
}
