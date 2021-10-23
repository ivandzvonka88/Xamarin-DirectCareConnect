using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class BillingInsuranceCompany
    {
        public int InsuranceCompanyId { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
        public long? AuditActionId { get; set; }
        public bool ExcludeRenderer { get; set; }
        public bool EnableEligibility { get; set; }
        public int StatusDelay { get; set; }
        public int StatusFreq { get; set; }
        public int PatientCount { get; set; }
        public byte? InsurancePriority { get; set; }
        public bool IsGovt { get; set; }
        public string InsCode { get; set; }

    }

    public class BillingInsuranceCompanyInit : ViewModelBase
    {
        public List<BillingInsuranceCompany> BillingInsuranceCompanies { get; set; } = new List<BillingInsuranceCompany>();
    }
}