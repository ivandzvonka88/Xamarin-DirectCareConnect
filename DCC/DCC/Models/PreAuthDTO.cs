using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class PreAuthDTO
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Units { get; set; }
        public int PreAuthorizationId { get; set; }

        public bool NotApplicable { get; set; }

    }
}