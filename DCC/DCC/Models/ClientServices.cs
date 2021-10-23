using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DCC.Models
{
    public class ClientServices
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<SelectListItem> Units { get; set; }
        public List<SelectListItem> DiagnosisCodes { get; set; }
        public string ServiceStartDate { get; set; }
        public string ServiceEndDate { get; set; }
        public string CompanyServiceId { get; set; }
        public string CPTCode { get; set; }
        public string Mod1 { get; set; }
        public string Unit { get; set; }
    }

}