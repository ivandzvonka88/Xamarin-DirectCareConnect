using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC
{
    public class UserClaims
    {
        public int uid { get; set; } // user id 
        public string userName { get; set; }
        public string userGuid { get; set; }
        public int prid { get; set; } // employee id
        public string pcode { get; set; } // company alpha code
        public int coid { get; set; }  // company id

        public int iSolvedCompanyId { get; set; } // company id registered in iSolved

        public string npi{ get; set; }  // npi
        public string conStr { get; set; }
        public int supervisoryLevel { get; set; }

        public string sendBirdUserId { get; set; }
        public string userLevel { get; set; }
        public string dcwRole { get; set; }
        public string state { get; set; }

        public string staffname { get; set; }
        public string staffnpi { get; set; }
        public string stafftitle { get; set; }
        public string timeZone { get; set; }
        public string companyName { get; set; }

        public string blobStorage { get; set; }

        public List<Company> companies = new List<Company>();
    }


    public abstract class ViewModelBase
    {
        public string userLevel { get; set; }
        public int userPrid { get; set; }
        public string sendBirdUserId { get; set; }
        public string staffname { get; set; }
        public string companyName { get; set; }
        public List<Company> companies = new List<Company>();
        public int CompanyID { get; set; }
        public string dcwRole { get; set; }
    }

   
    public class EmptyView : ViewModelBase
    {
        public int s;
    }

    public class Company
    {
        public string name;
        public string coid;

        public string blobStorage { get; set; }
        public string conStr { get; set; }
        public string npi { get; set; }

        // for mobile
        public string prid { get; set; }
        public string userlevel { get; set; }

        public string dcwrole { get; set; }
        public string staffname { get; set; }
        public string staffnpi { get; set; }
        public string stafftitle { get; set; }



    }


}