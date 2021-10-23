using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class PolicyWaiverDTO
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Units { get; set; }
        public int InsurancePolicyId { get; set; }
        public bool IsWaiverApplicable { get; set; }
        public bool IsAlreadyAdded { get; set; }
        public bool IsExist { get; set; }
        public bool IsDDDValue { get; set; }
        public int PolicyWaiverId { get; set; }


    }
}