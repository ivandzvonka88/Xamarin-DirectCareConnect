using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.ModelsLegacy
{
    public class GovernmentProgramInsuranceCompany
    {
        public int ID { get; set; }
        public string Code { get; set; }

        public string Name { get; set; }
        public int CompanyId { get; set; }
    }
}