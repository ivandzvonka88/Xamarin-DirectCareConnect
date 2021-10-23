using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{

    public class GuardianWindows
    {
        public string nameList { get; set; }
        public string guardianPage { get; set; }

        public string guardianHeader { get; set; }
        public string guardianProfile { get; set; }

        public Er er = new Er();

    }




    public class GuardianList: ViewModelBase
    {
        public List<Guardian> guardians;
        public Er er = new Er();
    }

  



    public class Guardian
    {
        public string name { get; set; }
        public int id { get; set; }
        public bool deleted{ get; set; }

        public bool registered { get; set; }
        public int clientCount { get; set; }
    }


    public class GuardianPageData
    {
        public string userLevel { get; set; }
        public GuardianProfile guardianProfile { get; set; }
        

        public List<GuardianClient> guardianClients { get; set; }
        public Er er = new Er();
    }

    public class GuardianClient
    {
        public string clientName { get; set; }
        public int clientId { get; set; }

        public string relationship { get; set; }


    }

    public class AddClientModal
    {
        public List<SelectOption> clients { get; set; }

        public List<SelectOption> relationships { get; set; }

        public Er er = new Er();
    }


    public class GuardianProfile
    {
        public int guardianUId { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string userName { get; set; }
        public string email{ get; set; }
        public string phone { get; set; }
        public string addressLine1 { get; set; }
        public string addressLine2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string postalCode { get; set; }
        public bool deleted { get; set; }
        public bool registered { get; set; }
        public bool mfaEnabled { get; set; }

        public Er er = new Er();
    }
}