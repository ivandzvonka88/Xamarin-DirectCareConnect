using DCC.ModelsLegacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DCC.Models.Clients
{

    public class Client
    {
        public string name { get; set; }
        public int id { get; set; }
        public bool deleted { get; set; }
    }

  
 
  
    public class ClientInit : ViewModelBase
    {



        public List<Client> clientList;




        
        public List<Option> serviceList { get; set; }

       public List<PolicyWaiver> policyWaivers = new List<PolicyWaiver>();


        public Er er = new Er();
    }

    public class Option
    {
        public string name { get; set; }
        public string value { get; set; }
        public string InsCode { get; set; }
        public string MCID { get; set; }

    }

   

    
    public class ProviderOptions
    {
        public List<Option> providers { get; set; }
        public bool requiresATCRelationship { get; set; }
        public bool requiresBillingLocationType { get; set; }

        public Er er = new Er();
    }


    public class RelId
    {
        public int clsvId { get; set; }
        public int relId { get; set; }
    }
    public class ClientStaffRelationship
    {
        public int clsvId { get; set; }
        public int clsvidId { get; set; }
        public int prId { get; set; }
        public int atcRelId { get; set; }
        public int billingLocationTypeId { get; set; }


    }

    public class GoalInfo
    {
        public int goalId { get; set; }
        public string shortTermGoal { get; set; }
        public string teachingMethod { get; set; }
        public string frequency { get; set; }
        public int step { get; set; }

        public string goalStatus { get; set; }
        public bool completed { get; set; }
    }


    public class Goal
    {
        public string goal { get; set; }
        public string score { get; set; }



    }
  


    public class GuardianDelReq
    {
        public int clsvId { get; set; }
        public int id { get; set; }
    }
    public class GuardianResp
    {
        public List<Guardian> guardians = new List<Guardian>();
        public Er er = new Er();
    }




    public class NewClientReq
    {
        public string clientFirstName { get; set; }
        public string clientLastName { get; set; }
        public string assistNumber { get; set; }
        public int clientId { get; set; }
        public Er er = new Er();
    }


 
  
}