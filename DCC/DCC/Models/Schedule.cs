using DCC.Models.Clients;
using DCC.Models.Services;
using DHTMLX.Scheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class Schedule
    {
        public int id { get; set; }
        public string text { get; set; }

        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public long? event_length { get; set; }
        public int? event_pid { get; set; }
        public string rec_type { get; set; }

        public string client_fn { get; set; }
        public string client_ln { get; set; }
        public string ClientFullName { get; set; } = "";
        public string service_name { get; set; }

        public int client_id { get; set; }

        public bool isActive { get; set; }
        public string ActionIcons { get; set; }
        public string AdditionalInfo { get; set; }
        public string ClientPhoneNumber { get; set; }
        public string ClientEmail { get; set; }
        public string Location { get; set; }

        public int? provider_id { get; set; }
        public string providerName { get; set; }
        public int? service_id { get; set; }

        public bool? missedVisit { get; set; }
        public int? resolutionCodeId { get; set; }
        public int? reasonCodeId { get; set; }
    }
    public class ScheduleInit:ViewModelBase
    {
        public List<Schedule> scheduleList;
        public List<Client> Clients;
        public Schedule schedule;
        public List<Service> services;
        public List<AZSandataVisitChangeReasonCode> reasonCodes { get; set; }
        public List<AZSandataResolutionCode> resolutionCodes { get; set; }

        public Er er = new Er();
    }

    public class AZSandataVisitChangeReasonCode
    {
        public int ReasonCodeID { get; set; }
        public string Description { get; set; }
        public bool NoteRequired { get; set; }
    }
    public class AZSandataResolutionCode
    {
        public int ResolutionCodeId { get; set; }
        public string Description { get; set; }
    }
}