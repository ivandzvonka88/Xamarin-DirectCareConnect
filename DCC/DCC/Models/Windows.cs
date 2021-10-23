using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{


    public class TaskPageMainWindows
    {
        public string taskPageHeader { get; set; }
        public string taskPageAlerts { get; set; }
        public string taskPageView { get; set; }

        public Er er = new Er();

    }



    public class Windows
    {
        public string alertTgt { get; set; }
        public string clientProfile { get; set; }
        public string clientAlerts { get; set; }
        public string staffAlerts { get; set; }
        public string pendingDocumentation { get; set; }
        public string clientServices { get; set; }
        public string clientCharts { get; set; }
        public Er er = new Er();

    }

    public class StaffPageWindows
    {
        public string staffEmployment { get; set; }
        public string staffCredentials { get; set; }
        public Er er = new Er();

    }
    public class ClientPageWindows
    {
        public string clientHeader { get; set; }
        public string clientProfile { get; set; }
        public string clientServices { get; set; }
        public string clientCharts { get; set; }

        public Er er = new Er();

    }
}