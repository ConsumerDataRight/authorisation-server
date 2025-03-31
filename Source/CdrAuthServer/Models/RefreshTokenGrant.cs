using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Models
{
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

                if (!this.Data.Any())
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

                if (!this.Data.Any())
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

                if (!this.Data.Any())
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
}
