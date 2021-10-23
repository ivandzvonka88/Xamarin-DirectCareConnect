using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class PolicyWaiver
    {
        public int PolicyWaiverId { get; set; }
        public int ServiceID { get; set; }
        public string ServiceName { get; set; }
        public int InsurancePolicyID { get; set; }
        public string ToDate { get; set; }
        public string FromDate { get; set; }
        public bool IsApplicable { get; set; }
        public bool PolicyIsDDD { get; set; }
        public string Units { get; set; }
        public int ClientId { get; set; }
    }

}