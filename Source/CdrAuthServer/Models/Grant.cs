using CdrAuthServer.Extensions;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Models
{
    public class Grant
    {
        public string Key { get; set; } = String.Empty;
        public string GrantType { get; set; } = String.Empty;
        public string ClientId { get; set; } = String.Empty;
        public string SubjectId { get; set; } = String.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public string Scope { get; set; } = String.Empty;
        public virtual IDictionary<string, object> Data { get; set; }

        public bool IsExpired
        {
            get
            {
                return DateTime.UtcNow > ExpiresAt;
            }
        }

        public Grant()
        {
            this.Data = new Dictionary<string, object>();
        }

        public object? GetDataItem(string dataItemKey)
        {
            if (this.Data.ContainsKey(dataItemKey))
            {
                return this.Data[dataItemKey];
            }

            return null;
        }

        protected void SetData(string key, object? value)
        {
            if (this.Data.ContainsKey(key))
            {
                this.Data[key] = value ?? string.Empty;
            }
            else
            {
                this.Data.Add(key, value ?? string.Empty);
            }
        }
    }

    public class RefreshTokenGrant : Grant
    {
        private string? _cdrArrangementId;

        public string? CdrArrangementId
        {
            get
            {
                if (!string.IsNullOrEmpty(_cdrArrangementId))
                {
                    return _cdrArrangementId;
                }

                if (!base.Data.Any())
                {
                    return null;
                }

                _cdrArrangementId = GetDataItem(ClaimNames.CdrArrangementId) as string;
                return _cdrArrangementId;
            }
            set
            {
                _cdrArrangementId = value;
                SetData(ClaimNames.CdrArrangementId, value);
            }
        }

        private string? _responseType;

        public string? ResponseType
        {
            get
            {
                if (!string.IsNullOrEmpty(_responseType))
                {
                    return _responseType;
                }

                if (!base.Data.Any())
                {
                    return null;
                }

                _responseType = GetDataItem(ClaimNames.ResponseType) as string;
                return _responseType;
            }
            set
            {
                _responseType = value;
                SetData(ClaimNames.ResponseType, value);
            }
        }

        private string? _authorizationCode;

        public string? AuthorizationCode
        {
            get
            {
                if (!string.IsNullOrEmpty(_authorizationCode))
                {
                    return _authorizationCode;
                }

                if (!base.Data.Any())
                {
                    return null;
                }

                _authorizationCode = GetDataItem(ClaimNames.AuthorizationCode) as string;
                return _authorizationCode;
            }
            set
            {
                _authorizationCode = value;
                SetData(ClaimNames.AuthorizationCode, value);
            }
        }
    }

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

                if (!base.Data.Any())
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

                if (!base.Data.Any())
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

            if (!base.Data.Any())
            {
                return new List<string>();
            }

            _accountIdDelimitedList = GetDataItem(ClaimNames.AccountId) as string;
            return !string.IsNullOrEmpty(_accountIdDelimitedList) ? _accountIdDelimitedList.Split(',').ToList() : new List<string>();
        }
    }

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

                if (!base.Data.Any())
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

    public class CdrArrangementGrant : Grant
    {
        public List<string> AccountIds
        {
            get
            {
                if (!base.Data.Any())
                {
                    return new List<string>();
                }

                return (GetDataItem(ClaimNames.AccountId) as List<string>) ?? new List<string>();
            }
            set
            {
                SetData(ClaimNames.AccountId, value);
            }
        }

        public string? RefreshToken
        {
            get
            {
                if (!base.Data.Any())
                {
                    return null;
                }

                return GetDataItem(ClaimNames.RefreshToken) as string;
            }
            set
            {
                SetData(ClaimNames.RefreshToken, value);
            }
        }

        public int Version
        {
            get
            {
                if (!base.Data.Any())
                {
                    return 1;
                }

                return Convert.ToInt32(GetDataItem(ClaimNames.CdrArrangementVersion));
            }
            set
            {
                SetData(ClaimNames.CdrArrangementVersion, value);
            }
        }

    }
}
