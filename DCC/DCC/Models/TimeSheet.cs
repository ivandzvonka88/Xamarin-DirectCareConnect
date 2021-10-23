using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class ValidDate
    {
        public int yr { get; set; }
        public int mn { get; set; }
        public int sdy { get; set; }
        public int edy { get; set; }
    }
    public class Period
    {
        public string startDate { get; set; }
        public string endDate { get; set; }

        public int periodId { get; set; }
    }

    public class AssignedProviderService
    {
        public string service { get; set; }
        public string serviceId { get; set; }


    }

    public class AssignedProvider
    {
        public string providerName { get; set; }

        public int providerId { get; set; }
    }

 


    public class TherapySession
    {
        public int clientSessionTherapyId { get; set; }

        public string svcDate { get; set; }

        public string svcDateAdj { get; set; }
        public string svcType { get; set; }

        public string inDate { get; set; }
        public string outDate { get; set; }
        public string inTime { get; set; }
        public string outTime { get; set; }

        public int clientId { get; set; }

        public string inDateAdj { get; set; }
        public string outDateAdj { get; set; }
        public string inTimeAdj { get; set; }
        public string outTimeAdj { get; set; }
        public decimal units { get; set; }
        public string serviceName { get; set; }
        public string serviceId { get; set; }

        public bool allowManualInOut { get; set; }
        public string locationId { get; set; }
        public string locationName { get; set; }
        public bool completedNote { get; set; }
        public bool approvedNote { get; set; }
        public string noteType { get; set; }
        public bool isEvaluation { get; set; }

        public bool designeeApproved { get; set; }
        public bool isEVV { get; set; }
        public int inCallTYpe { get; set; }

        public int outCallType { get; set; }
    }
    public class ProviderTimeSheetData : ViewModelBase
    {
        public int providerId;

        public string periodStartDate { get; set; }
        public string periodEndDate { get; set; }
        public string periodStartDateISO { get; set; }
        public string periodEndDateISO { get; set; }
        public List<TherapySession> sessions { get; set; }
        public List<SelectOption> locations { get; set; }

        public List<SelectOption> services { get; set; }

        public List<ValidDate> validDates { get; set; } = new List<ValidDate>();

        public Er er = new Er();

    }

}