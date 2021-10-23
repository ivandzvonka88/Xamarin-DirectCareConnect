using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{

    public class Auth
    {
        public int auId { get; set; }
        public string service { get; set; }
        public int clsvId { get; set; }
        public int clsvidId { get; set; }
        public string stdt { get; set; }
        public string eddt { get; set; }
        public decimal au { get; set; }
        public decimal tempAddedUnits { get; set; }

        public decimal uu { get; set; }
        public decimal ou { get; set; }
        public decimal ru { get; set; }
        public decimal wk { get; set; }
        public decimal weeklyHourOverride { get; set; }
        public int exp { get; set; }
    }
}