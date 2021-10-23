using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class ObjectivesModal
    {
        public ServiceObjective serviceObjective { get; set; }

        public List<SelectOption> goalAreas { get; set; }
        public List<SelectOption> frequencies { get; set; }
        public List<SelectOption> durations { get; set; }
        public List<SelectOption> statuses { get; set; }

        public Er er = new Er();
    }

}