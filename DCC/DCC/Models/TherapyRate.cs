using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class TherapyRate
    {
        public int RateId { get; set; }
        public int ServiceId { get; set; }
        public int Ratio { get; set; }
        public string IsQualifiedTherapistTxt { get; set; }
        public string IsClinicTxt { get ; set; }
        public int? BillingTierId { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public decimal Rate { get; set; }
        public string CurTxt { get; set; }
        public string Service { get; set; }
        public bool IsClinic { get; set; }
        public bool IsQualifiedTherapist { get; set; }
        public bool Cur { get; set; }
        public List<AZService> Services { get; set; } = new List<AZService>();
        public string ServiceName { get; set; }
    }

    public class AZService
    {
        public int ServiceId { get; set; }
        public string Name { get; set; }
    }
}