using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class InsurancePreAuth
    {
        public string InsuranceCompany { get; set; }
        public int InsuranceCompanyId { get; set; }
        public List<PreAuth> preAuths { get; set; }
    }

    public class PreAuth
    {
        public bool? isApplicable { get; set; }
        public string start { get; set; }
        public string end { get; set; }

        public decimal authUnits { get; set; }

        public decimal remUnits { get; set; }

        public decimal usedUnits { get; set; }

    }

}