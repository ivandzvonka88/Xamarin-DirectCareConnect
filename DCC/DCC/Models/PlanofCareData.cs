using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class PlanOfCareData
    {
        public int clientId { get; set; }
        public int evaluationId { get; set; }
        public int evaluationServiceId { get; set; }
        public List<SelectOption> Options { get; set; }
        public List<SelectOption> frequencies { get; set; }

        public int frequencyId {get; set;}
        public string treatmentStart { get; set; }
        public string treatmentEnd { get; set; }
        public string treatmentDurationId { get; set; }
        public int numberOfVisits { get; set; }


        public int providerId { get; set; }
    }

}