using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class StaffAlert
    {
        public int priority { get; set; }
        public string msg { get; set; } // deprecated
        public string alert { get; set; }

        public string name { get; set; }

        public string id { get; set; }
        public string sNm { get; set; }

    }
}