using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Models
{
    public class RequestUriGrant : Grant
    {
        private string? _request;

        public string? Request
        {
            get
            {
                if (!string.IsNullOrEmpty(_request))
                {
                    return _request;
                }

                if (!this.Data.Any())
                {
                    return null;
                }

                _request = GetDataItem(ClaimNames.Request) as string;
                return _request;
            }

            set
            {
                _request = value;
                SetData(ClaimNames.Request, value);
            }
        }
    }
}
