using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class Chart
    {
        public int chartId { get; set; }
        public string docType { get; set; }
        public string service { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string fileName { get; set; }
        public Er er = new Er();
    }

    public class Charts
    {
        public List<Chart> charts = new List<Chart>();
        public Er er = new Er();
    }

    public class ChartDelete
    {
        public int clsvId { get; set; }
        public int chartId { get; set; }
        public string fileName { get; set; }
        public Er er = new Er();
    }
}