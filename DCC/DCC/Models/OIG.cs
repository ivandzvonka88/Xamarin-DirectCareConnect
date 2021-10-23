using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class OIGPageData : ViewModelBase
    {
        public string OIGFileDate { get; set; }

        public List<OIGMatch> OIGMatches { get; set; }

    }

    public class OIGMatch
    {
        public string firstName { get; set; }
        public string lastName { get; set; }

        public string address { get; set; }

        public string city { get; set; }

        public string state { get; set; }

        public string zip { get; set; }

        public string dob { get; set; }

    }


  
}