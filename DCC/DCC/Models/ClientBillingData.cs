using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class ClientBillingData
    {
        public string ws { get; set; }
        public string we { get; set; }
        public List<ClientBillingItem> billingItems { get; set; }
    }

    public class ClientBillingItem
    {
        public string provider { get; set; }
        public string svc { get; set; }
        public decimal un { get; set; }
        public string rat { get; set; }
    }

}