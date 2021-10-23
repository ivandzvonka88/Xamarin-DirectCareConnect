using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class ShortTermGoal
    {
        public int goalId { get; set; }

        public string goalIndex { get; set; }
        public int step { get; set; }
        public string shortTermGoal { get; set; }
        public string teachingMethod { get; set; }
        public string goalStatus { get; set; }
        public string frequencyId { get; set; }
        public string frequency { get; set; }
        public string progress { get; set; }
        public string score { get; set; }

        public string trialPct { get; set; }
        public string completedDt { get; set; }
        public string recommendation { get; set; }

        public List<TherapyScore> therapyScores { get; set; }
        public Er er = new Er();
    }

    public class TherapyScore
    {
        public string date { get; set; }
        public string score { get; set; }
    }


}