using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class ProviderService
    {
        public int relId { get; set; } // relationship Id
        public int clsvId { get; set; } //Client Id
        public string providerName { get; set; } // Client Name
        public int clsvidId { get; set; } // Client Service Id
        public string service { get; set; } // Service Name
        public string relationship { get; set; } // client/staff relationship data for attendant care

    }

}