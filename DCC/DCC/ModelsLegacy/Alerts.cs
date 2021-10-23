using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models.Alerts
{
    public class AlertList : ViewModelBase
    {

        public List<Role> roles { get; set; }
        public List<AlertType> alertTypes { get; set; }
        public Er er = new Er();



    }
    public class Role
    {
        public int roleId { get; set; }
        public string roleName { get; set; }
      
    }


    public class AlertType
    {
        public int alertTypeId { get; set; }
        public string alertName { get; set; }
        public string alertType { get; set; }

        public List<AlertSetting> alertSettings { get; set; }

    }

    public class AlertSetting
    {
        public bool redEnabled { get; set; }
        public int redValue { get; set; }
        public bool amberEnabled { get; set; }
        public int amberValue { get; set; }
    }

    public class AlertTableResp
    {
        public List<AlertRow> alertRows { get; set; }
    }



    public class AlertRow
    {
        public int roleId { get; set; }
        public int alertTypeId { get; set; }
        public bool redEnabled { get; set; }
        public int redValue { get; set; }
        public bool amberEnabled { get; set; }
        public int amberValue { get; set; }
    }

}