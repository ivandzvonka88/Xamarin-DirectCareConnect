using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.ModelsLegacy
{
    public class ExistanceCheck
    {
        public bool IsDateRangeOverlapped { get; set; }
        public bool IsPreviousTierRequired { get; set; }
        public bool CanProceed { get; set; }
        public bool CanUpdate { get; set; }
    }
}