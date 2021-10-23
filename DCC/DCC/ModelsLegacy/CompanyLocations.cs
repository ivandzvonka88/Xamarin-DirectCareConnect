using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models.CompanyLocations
{


    public class LocationOptions : ViewModelBase
    {
        public List<Option> billingLocationTypes { get; set; }
        public List<Option> billingRegions { get; set; }
        public List<Option> billingTiers { get; set; }
        public List<Option> districts { get; set; }

        public List<Option> ranges { get; set; }

        public Er er = new Er();
    }



    public class LocationList
    {

        public List<CompanyLocation> locations;

        public Er er = new Er();
    }



    public class CompanyLocation
    {
        public int locationId  {get; set;}      
        public string name { get; set; } 
        public int range { get; set; }
        public int contractCapacity { get; set; }
        public int billingLocationTypeId { get; set; }
        public string billingLocationType { get; set; }
        public string reg { get; set; }

        public string npi { get; set; }

        public string loc { get; set; }
        public int districtId { get; set; }
        public string district { get; set; }
        public string ad1 { get; set; }
        public string ad2 { get; set; }
        public string cty { get; set; }
        public string st { get; set; }
        public string zip { get; set; }
        public decimal lat { get; set; }
        public decimal lon { get; set; }

        public string locationType { get; set; }
        public int radius { get; set; }
        public bool isActive { get; set; }
        public string isActiveStr { get; set; }

    }

    public class Option
    {
        public string value { get; set; }
        public string name { get; set; }

    }

}