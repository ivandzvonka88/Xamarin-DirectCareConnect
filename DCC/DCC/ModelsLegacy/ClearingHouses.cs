using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.ModelsLegacy
{
    public class ClearingHouses
    {
        public int CompanyID { get; set; }
        public int ClearingHouseId { get; set; }
        public string ClearingHouseLogin { get; set; }
        public string ClearingHousePasscode { get; set; }
        public string ClearingHouseRTPass { get; set; }
        public string ClearingHouseRTUser { get; set; }
    }


    public class ClearingHousesInit : ViewModelBase
    {
        List<ClearingHouses> clearingHouses = new List<ClearingHouses>();
    }
}