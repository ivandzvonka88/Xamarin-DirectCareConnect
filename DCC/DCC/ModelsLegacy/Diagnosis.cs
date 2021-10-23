using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.ModelsLegacy
{
    public class Diagnosis
    {
        public string DiagnosisCode { get; set; }

        public string Description { get; set; }

        public int ID { get; set; }
    }

    public class DiagnosisInit : ViewModelBase
    {
        public List<Diagnosis> diagnosisList { get; set; } = new List<Diagnosis>();
    }
}