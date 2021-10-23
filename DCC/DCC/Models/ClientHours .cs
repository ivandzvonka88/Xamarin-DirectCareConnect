using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class ClientHours
    {
        public int PeriodId { get; set; }

        public List<Period> Periods { get; set; }

        public bool has_1_1 { get; set; }
        public bool has_1_2 { get; set; }
        public bool has_1_3 { get; set; }
        public bool has_1_4 { get; set; }
        public bool has_1_5 { get; set; }
        public bool has_1_6 { get; set; }
        public ClientHours()
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


       

    }
   



}