using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class BillingLocations
    {
        public List<SelectOption> billingLocationTypes { get; set; }

        public List<SelectOption> billingLocations { get; set; }
    }
}