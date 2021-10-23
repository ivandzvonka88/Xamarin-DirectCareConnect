using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models.Frequencies
{
    public class FrequencyList: ViewModelBase
    {
        public List<FreqDur> freqDur { get; set; }
        public List<DurationDiscipline> durationDiscipline { get; set; }
        public List<Discipline> discipline { get; set; }
        public Er er = new Er();
    }


    public class Discipline
    {
        public int disciplineId { get; set; }
        public string discipline { get; set; }

    }




    public class DurationDiscipline
    {
        public int durationId { get; set; }
        public int disciplineId { get; set; }

    }
    public class FreqDur
    {
        public int durationId { get; set; }
        public string name { get; set; }
        public short duration { get; set; }
        public string frequency { get; set; }
        public short weeks { get; set; }
        public string isActiveStr { get; set; }
        public bool isActive { get; set; }
        public bool beenUsed { get; set; }

        public int[] disciplines { get; set; }
        public Er er = new Er();
    }
}