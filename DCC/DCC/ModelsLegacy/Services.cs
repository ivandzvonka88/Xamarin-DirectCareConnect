using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models.Services
{
    public class ServiceList
    {
        public List<Service> services { get; set; }
        public Er er = new Er();
    }

    public class Service
    {
        public int serviceId { get; set; }
        public string name { get; set; }
        public string serviceModel { get; set; }
        public string discipline { get; set; }
        public bool allowClientHome { get; set; }
        public bool allowProviderHome { get; set; }
        public bool allowClinic { get; set; }
        public string isActive { get; set; }
        public Er er = new Er();
    }

    public class ServiceInfo
    {
        public int serviceId { get; set; }
        public string name { get; set; }
        public int serviceModelId { get; set; }
        public int disciplineId { get; set; }
        public bool isEvaluation { get; set; }
        public int minutesPerUnit { get; set; }
        public bool allowClientHome { get; set; }
        public bool allowProviderHome { get; set; }
        public bool allowClinic { get; set; }
        public int reportingPeriodId { get; set; }
        public string isActive { get; set; }

        public List<Discipline> disciplineList { get; set; }
        public List<Duration> durationList { get; set; }
        public List<ServiceModel> serviceModelList { get; set; }
        public List<DurationDiscipline> durationDisciplines { get; set; }
        public List<ServiceDuration> serviceDurations { get; set; }
        public List<Question> questions { get; set; }
        public List<Question> allQuestions { get; set; }
        public Er er = new Er();
    }


    public class Duration
    {
        public int disciplineId { get; set; }
        public int durationId { get; set; }
        public string name { get; set; }
    }
    public class Discipline
    {
        public int disciplineId { get; set; }
        public string name { get; set; }
    }

    public class ServiceModel
    {
        public int serviceModelId { get; set; }
        public string name { get; set; }
    }

    public class DurationDiscipline
    {
        public int durationId { get; set; }
        public int disciplineId { get; set; }
    }

    public class ServiceDuration
    {
        public int durationId { get; set; }
        public int serviceId { get; set; }
    }
    public class Question
    {
        public int questionId { get; set; }
        public string question { get; set; }

        public int orderNumber { get; set; }
        public bool prepopulate { get; set; }
        public bool isRequired { get; set; }
    }
}