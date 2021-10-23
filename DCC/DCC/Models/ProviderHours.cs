using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class ProviderHours
    {
        public int PeriodId { get; set; }

        public List<Period> Periods { get; set; }

        public bool has_1_1 { get; set; }
        public bool has_1_2 { get; set; }
        public bool has_1_3 { get; set; }
        public bool has_1_4 { get; set; }
        public bool has_1_5 { get; set; }
        public bool has_1_6 { get; set; }

        public ProviderHours()
        {
            Units_1_1_Total = 0M;
            Units_1_2_Total = 0M;
            Units_1_3_Total = 0M;
            Units_1_4_Total = 0M;
            Units_1_5_Total = 0M;
            Units_1_6_Total = 0M;

            has_1_1 = false;
            has_1_2 = false;
            has_1_3 = false;
            has_1_4 = false;
            has_1_5 = false;
            has_1_6 = false;

        }

        public List<BillableMatrixItem> matrixItems = new List<BillableMatrixItem>();
        public decimal Units_1_1_Total { get; set; }
        public decimal Units_1_2_Total { get; set; }
        public decimal Units_1_3_Total { get; set; }

        public decimal Units_1_4_Total { get; set; }
        public decimal Units_1_5_Total { get; set; }
        public decimal Units_1_6_Total { get; set; }


        public decimal Units_Total_All { get; set; }


        public List<Visit> Visits = new List<Visit>();

    }
    public class Visit
    {
        public string SessionType { get; set; }
        public int StaffSessionId { get; set; }
        public int ClientSessionId { get; set; }

        public string Service { get; set; }

        public string Date { get; set; }
        public string StartAt { get; set; }
        public string EndAt { get; set; }
        public string ClientName { get; set; }
        public string Ratio { get; set; }
        public string Units { get; set; }


        public string Status { get; set; }

        public bool IsEVV { get; set; }

        public decimal StartLat { get; set; }
        public decimal StartLon { get; set; }
        public string StartLocationAddress { get; set; }
        public string StartLocationType { get; set; }
        public decimal EndLat { get; set; }
        public decimal EndLon { get; set; }
        public string EndLocationAddress { get; set; }
        public string EndLocationType { get; set; }
        public int LocationTypeId { get; set; }
        public int ClientLocationId { get; set; }
        public string ClientLocationType { get; set; }
        public string BillingLocationType { get; set; }

        public bool NotPayable { get; set; }

    }

    public class BillableMatrixItem
    {
        public string svc { get; set; }
        public decimal Units_1_1 { get; set; }
        public decimal Units_1_2 { get; set; }
        public decimal Units_1_3 { get; set; }
        public decimal Units_1_4 { get; set; }
        public decimal Units_1_5 { get; set; }
        public decimal Units_1_6 { get; set; }


        public decimal Units_Total { get; set; }
    }


    public class SessionLocation
    {
        public decimal StartLat { get; set; }
        public decimal StartLon { get; set; }
        public decimal EndLat { get; set; }
        public decimal EndLon { get; set; }

        public string StartLocation { get; set; }
        public string EndLocation { get; set; }
    }

}