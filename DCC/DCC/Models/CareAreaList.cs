using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class CareAreaList
    {
        public int careId { get; set; }
        public string careArea { get; set; }
        public string lastDate { get; set; }
        public bool deleted { get; set; }
    }
}