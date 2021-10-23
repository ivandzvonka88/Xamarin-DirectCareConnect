using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class ServiceObjective
    {
        public int clientId { get; set; }
        public int serviceId { get; set; }
        public int clsvidId { get; set; }
        public bool isTherapy { get; set; }
        public string svcName { get; set; }
        public List<LongTermObjective> longTermObjectives { get; set; }
        public Er er = new Er();
    }
}