using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class LongTermObjective
    {
        public int objectiveId { get; set; }

        public string objIndex { get; set; }
        public string longTermVision { get; set; }
        public string longTermGoal { get; set; }
        public int goalAreaId { get; set; }
        public string goalAreaName { get; set; }
        public bool goalAreaActive { get; set; }
        public string objectiveStatus { get; set; }
        public string completedDt { get; set; }
        public string changes { get; set; }
        public List<ShortTermGoal> shortTermGoals { get; set; }

        public Er er = new Er();
    }
}