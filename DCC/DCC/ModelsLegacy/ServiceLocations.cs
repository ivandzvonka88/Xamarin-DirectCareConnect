using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models.ServiceLocations
{
    public class ServiceLocationInit :ViewModelBase
    {

        public List<Option> services { get; set; }

        public Er er = new Er();
    }


    public class ServiceLocationsList
    {
        public List<Location> locations { get; set; }
  

        public Er er = new Er();
    }

    public class Location
    {
       public int locId { get; set; }
        public string loc { get; set; }
        public int svId { get; set; }
        public string deptCode { get; set; }
        public string name { get; set; }
        public string ds { get; set; }
        public int rng { get; set; }
        public int cap { get; set; }
        public bool isBilling { get; set; }
        public bool isPayroll { get; set; }
        public bool billingRegionId{ get; set; }
    }



    public class Option
    {
        public int value { get; set; }
        public string name { get; set; }
    }

   

}