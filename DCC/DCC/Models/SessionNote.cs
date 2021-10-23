using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Web;

namespace DCC.Models.SessionNotes
{
    public class ClientNote
    {
        public string coId { get; set; }
        public string docType { get; set; }

        public int docId { get; set; }
        public DateTime? startUTC { get; set; }


        public string signee { get; set; }

        public string signeeCredentials { get; set; }

        public bool verification { get; set; }

        public string clientName { get; set; }
        public int clientId { get; set; }

        public int clientServiceId { get; set; }
        public int serviceId { get; set; }
        public string providerName { get; set; }
        public int providerId { get; set; }
        public string dt { get; set; }

        public string svc { get; set; }


        public string note { get; set; }

        public bool hasAttachment { get; set; }

        public string attachmentName { get; set; }

        public string extension { get; set; }

        public List<LongTermObjective> longTermObjectives { get; set; }
        public List<Scoring> scoring { get; set; }
        public List<CareArea> careAreas { get; set; }
        public bool completed { get; set; }
        public bool noShow { get; set; } // means client No Show

        // XXXX New statuses to be used with note
        public bool designeeUnableToSign { get; set; }
        public bool designeeRefusedToSign { get; set; }
        public bool clientRefusedService { get; set; }
        public bool unsafeToWork { get; set; }
        public int guardianId { get; set; }  //guardianId  and designeeId required if the above 5 are false
        public int designeeId { get; set; }  //guardianId  and designeeId required if the above 5 are false
        public decimal designeeLat { get; set; } // latitude when pin entered
        public decimal designeeLon { get; set; }  // longitude when pin entered

        public int designeeLocationId { get; set; } // if with geofence when pin entered else 0 - corresponds to clientLocationIds

        public int designeeLocationTypeId { get; set; }  // if with geofence when pin entered else 0 - corresponds to clientLocationIds
        // XXXX end new Stuff


        public bool teletherapy { get; set; }
        public bool supervisorPresent { get; set; }

        public bool rejected { get; set; }
        public string rejectedReason { get; set; }


        public string clientLocationValue { get; set; }
        public string location { get; set; }
        public string inTime { get; set; }

        public string outTime { get; set; }

        public string adjDt { get; set; }
        public string adjInTime { get; set; }

        public string adjOutTime { get; set; }
        public string isoDate { get; set; }
        public bool isEVV { get; set; }
        public bool locationDetermined { get; set; }

        public List<SelectOption> locations { get; set; }


        public Er er = new Er();
    }
    public class ClientNotePdf
    {
        public string agency { get; set; }
        public string npi { get; set; }
        public string clientName { get; set; }
        public string dt { get; set; }

        public string timeOfService { get; set; }
        public string svc { get; set; }
        public string serviceName { get; set; }
        public string dob { get; set; }
        public string diagnosis { get; set; }

        public string clientWorker { get; set; }
        public string clId { get; set; }
        public string completedBy { get; set; }
        public string completedByCredentials { get; set; }
        public string completedByTitle { get; set; }
        public string completedByPhone { get; set; }

        public string approvedBy { get; set; }
        public string approvedByCredentials { get; set; }
        public string approvedByTitle { get; set; }

        public string note { get; set; }

        public List<CareArea> careAreas { get; set; }
        public List<LongTermObjective> longTermObjectives { get; set; }
        public List<Scoring> scoring { get; set; }

        public bool noShow { get; set; }

        // XXXX New statuses to be used with note
        public bool designeeUnableToSign { get; set; }
        public bool designeeRefusedToSign { get; set; }
        public bool clientRefusedService { get; set; }
        public bool unsafeToWork { get; set; }
        public int guardianId { get; set; }  //guardianId  and designeeId required if the above 5 are false
        public int designeeId { get; set; }  //guardianId  and designeeId required if the above 5 are false
        public decimal designeeLat { get; set; } // latitude when pin entered
        public decimal designeeLon { get; set; }  // longitude when pin entered

        public int designeeLocationId { get; set; } // if with geofence when pin entered else 0 - corresponds to clientLocationIds

        public int designeeLocationTypeId { get; set; }  // if with geofence when pin entered else 0 - corresponds to clientLocationIds
        // XXXX end new Stuff
        public bool IsEVV { get; set; }
        public bool teletherapy { get; set; }
        public bool supervisorPresent { get; set; }

        public Er er = new Er();
    }


    public class Scoring
    {
        public string value { get; set; }
        public string name { get; set; }
    }

    /*
      public class LongTermObjective
      {
          public int objectiveId { get; set; }

      //    public int serviceId { get; set; }
      //    public int clsvId { get; set; }
     //     public int clsvidId { get; set; }
          public string longTermVision { get; set; }
          public string longTermGoal { get; set; }
          public int goalAreaId { get; set; }
          public string objectiveStatus { get; set; }

          public string changes { get; set; }
          public List<ShortTermGoal> shortTermGoals { get; set; }

          public Er er = new Er();
      }


      public class ShortTermGoal
      {
          public int goalId { get; set; }
          public int step { get; set; }
          public string shortTermGoal { get; set; }
          public string teachingMethod { get; set; }
          public string goalStatus { get; set; }
          public string frequencyId { get; set; }
          public string frequency { get; set; }
          public string progress { get; set; }
          public string score { get; set; }

          public string trialPct { get; set; }
          public List<TherapyScore> therapyScores { get; set; }
          public string recommendation { get; set; }
          public Er er = new Er();
      }
     
    public class TherapyScore
    {
        public string date { get; set; }
        public string score { get; set; }
    }
   */

}