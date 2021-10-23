using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Helpers
{
    public enum GenderTypeEnum
    {
        Female = 0,
        Male = 1,
        Other = 2
    }

    public enum InsuranceTierEnum
    {
        Primary = 1,
        Secondary = 2,
        Tertiary = 3
    }
    public enum AccessTypeEnum
    {
        FTP = 0,
        WebCrawling = 1
    }

    public enum PlaceOfService
    {
        Teletherapy = 2,
        Clinic = 11,
        Home = 12,
        Other = 9
    }
}