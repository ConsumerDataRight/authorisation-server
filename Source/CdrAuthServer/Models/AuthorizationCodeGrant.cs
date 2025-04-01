using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Models
{
    public class AuthorizationCodeGrant : Grant
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

        private string? _accountIdDelimitedList;

        public string? AccountIdDelimitedList
        {
            get
            {
                if (!string.IsNullOrEmpty(_accountIdDelimitedList))
                {
                    return _accountIdDelimitedList;
                }

                if (!this.Data.Any())
                {
                    return null;
                }

                _accountIdDelimitedList = GetDataItem(ClaimNames.AccountId) as string;
                return _accountIdDelimitedList;
            }

            set
            {
                _accountIdDelimitedList = value ?? string.Empty;
                SetData(ClaimNames.AccountId, value);
            }
        }

        public List<string> GetAccountIds()
        {
            if (!string.IsNullOrEmpty(_accountIdDelimitedList))
            {
                return _accountIdDelimitedList.Split(',').ToList();
            }

            if (!this.Data.Any())
            {
                return new List<string>();
            }

            _accountIdDelimitedList = GetDataItem(ClaimNames.AccountId) as string;
            return !string.IsNullOrEmpty(_accountIdDelimitedList) ? _accountIdDelimitedList.Split(',').ToList() : new List<string>();
        }
    }
}
