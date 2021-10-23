using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class CareArea
    {
        public int careId { get; set; }
        public int clsvId { get; set; }
        public int serviceId { get; set; }
        public int clsvidId { get; set; }
        public string careArea { get; set; }
        public string careAreaId { get; set; }
        public string score { get; set; }
        public string lastDate { get; set; }
    }
}