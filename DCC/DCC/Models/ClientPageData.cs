using DCC.Models.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class ClientPageData
    {
        public string tgtView { get; set; }
        public string userLevel { get; set; }
        public int userPrid { get; set; }
        public ClientProfile clientProfile { get; set; }
        public List<ClientService> services { get; set; }
        public List<GeoLocation> geoLocations { get; set; }
        public List<Chart> charts { get; set; }

        public string documentationStart { get; set; }
        public string documentationEnd { get; set; }
        public bool documentationSelNotes { get; set; }
        public bool documentationSelReports { get; set; }



        public List<Documentation> documentation = new List<Documentation>();
        public List<CommentHistory> commentHistory { get; set; }
        public List<CareAreaList> careAreas { get; set; }
        public List<ServiceObjective> serviceObjectives { get; set; }

        public List<ProviderService> providerServices = new List<ProviderService>();

        public List<InsurancePolicyDTO> policies = new List<InsurancePolicyDTO>();


        public List<ClientAlert> clientAlerts { get; set; }
        public List<StaffAlert> staffAlerts { get; set; }

        public List<Period> periods { get; set; }

        public List<Period> payPeriods { get; set; }
        public List<AssignedProvider> assignedProviders { get; set; }
        
      public List<ClientDetailInfo> clientClaims { get; set; }

        public bool hasCareAreas { get; set; }

        public ClientHours clientHours = new ClientHours();
        public List<Guardian> guardians = new List<Guardian>();
        public Er er = new Er();
    }
}