using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class GeoLocation
    {

        public int clsvId { get; set; }
        public int clLocId { get; set; }


        public string name { get; set; }

        public string type { get; set; }
        public int locationTypeId { get; set; }
        public int locationId { get; set; }

        public string ad1 { get; set; }
        public string ad2 { get; set; }
        public string cty { get; set; }
        public string st { get; set; }
        public string zip { get; set; }
        public decimal lat { get; set; }
        public decimal lon { get; set; }
        public string locationType { get; set; }
        public int radius { get; set; }

        public string landline { get; set; }

        public string billingTier { get; set; }

    }
}