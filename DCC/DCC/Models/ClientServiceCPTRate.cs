using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DCC.Models
{
    public class ClientServiceCPTRate
    {

        public int ClientServiceId { get; set; }
        public string CPTCode { get; set; }
        public decimal Amount { get; set; }
        public int ClaimId { get; set; }
        public int AppointmentId { get; set; }
        public string Mod1 { get; set; }
        public string Mod2 { get; set; }
        public string Mod3 { get; set; }
        public int InsuranceCompanyId { get; set; }
        public int InsurancePolicyId { get; set; }
        public string Units { get; set; }
        public List<string> DiagnosisCodes { get; set; }
        public List<DiagnosisCode> DiagnosisCodeList { get; set; } = new List<DiagnosisCode>();
        public List<SelectListItem> DiagCodes { get; set; } = new List<SelectListItem>();

    }
    public class DiagnosisCode
    {
        public string Code { get; set; }
        public int ClientServiceID { get; set; }
        public string Name { get; set; }


    }
}