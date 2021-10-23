using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class SpecialRate
    {
        public int clsvId { get; set; }
        public int spRtId { get; set; }
        public int clsvidId { get; set; }
        public string fn { get; set; }
        public string ln { get; set; }
        public string svcName { get; set; }
        public decimal ratio { get; set; }
        public decimal rate { get; set; }

    }
}