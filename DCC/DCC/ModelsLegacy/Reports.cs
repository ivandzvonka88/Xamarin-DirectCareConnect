using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models.Reports
{
    public class ServiceOptions :ViewModelBase
    {

        public List<Option> services { get; set; }
        public List<Question> allQuestions { get; set; }
        public Er er = new Er();
    }


    public class Option
    {
        public string value { get; set; }
        public string name { get; set; }


    }


    public class ServiceInfo
    {
        public int serviceId { get; set; }

        public bool isTherapy { get; set; }
        public bool isEvaluation { get; set; }

        public List<GoalArea> goalAreas { get; set; }
        public List<Question> periodicQuestions { get; set; }
        public List<Question> sessionQuestions { get; set; }

       // public List<Question> allQuestions { get; set; }

        public Er er = new Er();
    }

    public class Question
    {
        public int questionId { get; set; }
        public string question { get; set; }
        public bool sharedQuestion { get; set; }
        public int orderNumber { get; set; }
        public bool prepopulate { get; set; }
        public bool isRequired { get; set; }

        public bool supervisorOnly { get; set; }
    }


    public class GoalArea
    {
        public int goalAreaId { get; set; }
        public string name { get; set; }
        public bool isActive { get; set; }
        public int serviceId { get; set; }
    }



}