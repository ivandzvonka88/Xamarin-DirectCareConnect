using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.ModelsLegacy
{
    public class InsuranceCompany
    {
        public string PayerId { get; set; }
        public string InsuranceCompanyPayerId { get; set; }
        public string Name { get; set; }
        public int InsuranceCompanyId { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string InsPayerId { get; set; }
        public int EligibilityCheckId { get; set; }
        public int StatusCheckId { get; set; }
        public int ClearinghouseId { get; set; }
        public string MCID { get; set; }
        public bool IsGovt { get; set; }
        public string InsCode { get; set; }
        public List<GovernmentProgramInsuranceCompany> GovtProgramLinks { get; set; }
    }

    public class GovtProgramLinksList
    {
        public int ID { get; set; }
        public int Code { get; set; }
    }

    public class InsuranceCompanyInit : ViewModelBase
    {
        public List<Options> GovtProgramLinks { get; set; }
        public InsuranceCompany InsuranceCompany { get; set; }
        public List<InsuranceCompany> InsuranceCompanyList { get; set; }
    }
    public class Options
    {
        public string Name { get; set; }
        public int Id { get; set; }

    }
}