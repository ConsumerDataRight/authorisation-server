using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CdrAuthServer.Repository.Entities
{
    public class Token
    {
        public string Id { get; set; } = string.Empty;
        public bool BlackListed { get; set; }
    }
}
