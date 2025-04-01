using CdrAuthServer.Extensions;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Models
{
    public class CdrArrangementGrant : Grant
    {
        public List<string> AccountIds
        {
            get
            {
                if (!this.Data.Any())
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
                if (!this.Data.Any())
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
                if (!this.Data.Any())
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
