
namespace CdrAuthServer.Infrastructure.Models
{
    public class Pkce
    {
        public string? CodeVerifier { get; set; }
        public string? CodeChallenge { get; set; }
        public string CodeChallengeMethod { get; private set; }

        public Pkce()
        {
            CodeChallengeMethod = Constants.Infosec.CODE_CHALLENGE_METHOD;
        }
    }
}
