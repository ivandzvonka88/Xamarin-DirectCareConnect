using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DCC.Models;
using DCC.Models.SessionNotes;

namespace DCC.ModelsApi
{
   
    public class GenericResponse
    {
        public Int64 deviceSessionId { get; set; }
        public Int64 HCBSEmpHrsId { get; set; }

        public Er er = new Er();

    }


    public class UserConfig
    {

        public string timeZone { get; set; }
        public List<CompanyList> companies { get; set; }



        Er er = new Er();
    }

    public class StaffTracking
    {

        public string coId { get; set; }

        public int providerId { get; set; }
  
        public DateTime utcTime { get; set; }

        public int clientLocationId { get; set; }// key for client location 
        public int locationTypeId { get; set; } // 1,2 or x
        public decimal lat { get; set; }
        public decimal lon { get; set; }

    }
    public class SessionPosition
    {




        public string coId { get; set; }

        public int? sessionId { get; set; }
        public int providerId { get; set; }
        public int clientServiceId { get; set; }

        public DateTime startUtc { get; set; }
       

        public int isHCBS { get; set; }

        public DateTime timeStamp { get; set; }

        public int clientLocationId { get; set; }// key for client location 
        public int locationTypeId { get; set; } // 1,2 or 3
        public decimal lat { get; set; }

        public decimal lon { get; set; }

    }

    public class PendingDocumentation
    {
        public int docId { get; set; }
        public string docType { get; set; }
        public int serviceId { get; set; }
        public int clientServiceId { get; set; }
        public int clientId { get; set; }
        public bool completed { get; set; }
        public bool approved { get; set; }
        public bool lostSession { get; set; }
        public string clientName { get; set; }
        public string dueDt { get; set; }
        public string svc { get; set; }
        public string noteType { get; set; }
        public string status { get; set; }

    }

    public class SessionInfo
    {

        public int HCBSEmpHrsId { get; set; } // 0 if no session adssigned

     
        public string CoId { get; set; }

    //    public Int64 deviceSessionId { get; set; }
        public int ProviderId { get; set; }

        public int AtcRelId { get; set; }
        public DateTime StartUTC { get; set; }
        public DateTime? EndUTC { get; set; }
        public int ClientId { get; set; }
        public int ClientServiceId { get; set; }
        public int ServiceId { get; set; }
        public string ShortServiceName { get; set; }
        public bool IsTherapy { get; set; }
        public bool IsEvaluation { get; set; }


        public int? StartClientLocationId { get; set; }// key for client location 
        public int? StartLocationTypeId { get; set; } // 1,2 or x
        public decimal StartLat { get; set; }
        public decimal StartLon { get; set; }

        public int? EndClientLocationId { get; set; }// key for client location 
        public int? EndLocationTypeId { get; set; } // 1,2 or x
        public decimal? EndLat { get; set; }
        public decimal? EndLon { get; set; }



        public string NoteType { get; set; }

        public SessionNote sessionNote { get; set; }

       

    }




    public class CompanyList
    {
        public string name { get; set; }
        public string coId { get; set; }
        public int providerId { get; set; }
        public List<Client> clients { get; set; }

        public List<ClientAlert2> clientAlerts { get; set; }
        public List<Credential> credentials { get; set; }
      //  public List<ATCScoreTypes> atcScoring { get; set; }
      //  public List<HAHScoreTypes> hahScoring { get; set; }
        public List<MessagingContact> messagingContacts { get; set; }


        public List<PendingDocumentation> pendingDocumentation { get; set; }
    }

    public class ProviderConfig //configuraion
    {
      
        public Er er = new Er();  //
    }

    public class ClientAlert2
    {
        public int priority { get; set; } // 1 is red, 2 is amber
        public string alert { get; set; } // alert text

        public int clientId { get; set; } // client Id in case it is needed

        public string clwEm { get; set; }
        public string clwNm { get; set; }
        public string clwPh { get; set; }

    }

    public class Credential
    {
        public string coId { get; set; }
        public int providerId { get; set; }
        public int credId { get; set; } // if 0 it means we need a new one to be created - and we need a document upload, else a document upload is optional
        public int credTypeId { get; set; } // the id for type of credential
        public string credName { get; set; } // the name for the type of credential
        public string docId { get; set; } // Document id assigned by the provider eg if driver license then license number
        public string validFrom { get; set; } // date Entered by provide
        public string validTo { get; set; } // date Entered by provider

        public string status { get; set; } // will be either "Expiring" on validToDate/ "Expired" or "Not Verified" 

    }


    public class Designee  // client info
    {
       public int guardianId { get; set; }
        public int designeeId { get; set; }  // client primary key in database.

        public bool isPrimaryGuardian { get; set; }  
        public string firstName { get; set; } 
        public string lastName { get; set; }  
        public string email { get; set; } 
        public string pin { get; set; }  
    }

    public class Client  // client info
    {
        public int clientId { get; set; }  // client primary key in database.
        public string clientFirstName { get; set; }  // for the Apps benefit
        public string clientLastName { get; set; }  // for the Apps benefit
        public List<Service> services { get; set; }
        public List<Location> locations  { get; set; }
        public List<Designee> designees { get; set; }

    }

    public class Service
    {
        public int serviceId { get; set; }   // primary keys for service
        public int clientServiceId { get; set; }   // primary keys for service
        public bool isTherapy { get; set; }
        public bool isEvaluation { get; set; }
        public bool isHCBS { get; set; }
        public string name { get; set; } // service name 
        public string shortName { get; set; }  // three letter service code
        public List<Auth2> authorizations { get; set; } // authorization info 
        public string noteType { get; set; }  // the note type required from the provider - will select either ATCNote,RSPNote or HAHNote



        public ClientNote clientNote { get; set; } 


    }

    public class Location
    {
        public int clientLocationId { get; set; }// key for client location 

        public int locationTypeId { get; set; } // 1,2 or 3

     //   public string locationType { get; set; } // client home, provider home or clinic
    //    public string locationName { get; set; }

        public string address { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zip { get; set; }
        public decimal lat { get; set; }   // latitude
        public decimal lon { get; set; }   // longitude
        public int radius { get; set; } // Geo-fence radius

    }

    public class Auth2
    {
        public string startDt { get; set; } // Date the auth starts
        public string endDt { get; set; }  // Date the auth end
        public decimal remainingHours { get; set; } // remaining units on this authorization in .25 increments
        public decimal weeklyHours { get; set; } // hours availabe per week
        public decimal remainingDailyHours { get; set; } // Hour available for current day
    }

   

  

  
    public class SessionNote
    {
        public int HCBSEmpHrsId { get; set; } // 0 if no session adssigned
        public Int64 deviceSessionId { get; set; }
        public string noteType { get; set; }
        public int providerId { get; set; } // used when returning note

        public string coId { get; set; }
      

        public int clientId { get; set; } // used when returning note
        public int clientServiceId { get; set; } // used when returning note
     
        public List<Note> notes { get; set; }


        public List<TaskScore> taskScores { get; set; }
      
    }

    public class TaskScore
    {
        public string frequency { get; set; }

        public int taskId { get; set; }


        public string title { get; set; }
        public string subText { get; set; }
        public string score { get; set; }


        public string lastDate { get; set; }
    }



    public class Note
    {
        public string title { get; set; }
        public string note { get; set; }
    }



    public class RSPNote // can be used as template to send note
    {
        public int providerId { get; set; } // used when returning note

        public string coId { get; set; }


        public int clientId { get; set; } // used when returning note
        public int clientServiceId { get; set; } // used when returning note
        public string shortName { get; set; } // used when returning note

        public string date { get; set; }
        public int clRSPNoteId { get; set; }
        public string note { get; set; }
    }
   
    public class HAHNote
    {
        public int providerId { get; set; } // used when returning note
        public string coId { get; set; }
        public int clientId { get; set; } // used when returning note
        public int clientServiceId { get; set; } // used when returning note
        public string shortName { get; set; } // used when returning note

        public string date { get; set; }
        public int clHAHNoteId { get; set; }

        public string note { get; set; } // text fillable by provider
        public List<HAHGoal> hahGoals { get; set; }

    }




    public class HAHGoal
    {
        public int goalId { get; set; } // Id
        public string goal { get; set; } // Actual goal objective
        public string teachingMethod { get; set; } // strategies to be employed by provider
        public string lastDate { get; set; }// last date this objective was reported on by provider
        public string frequency { get; set; }
        public string score { get; set; } // scoring fillable by provider - see HAHScore    
    }

    public class HAHScoreTypes{
        public string scoreValue { get; set; } // 1- 3 characters
        public string scoreName { get; set; } // meaning of the 1-3 characters above
    }

    public class ATCNote
    {
        public int providerId { get; set; } // used when returning note

        public string coId { get; set; }
        public int clientId { get; set; } // used when returning note
        public int clientServiceId { get; set; } // used when returning note
        public string shortName { get; set; } // used when returning note
        public string date { get; set; }

        public int clATCNoteId { get; set; }
        public string note { get; set; }
        public List<ATCCareArea> atcCareAreas { get; set; }
    }

   


    public class ATCCareArea
    {
        public int careId { get; set; } // Id
        public string careArea { get; set; } // care area eg brushing teeth
        public string score { get; set; } // scoring fillable by provider - see ATCScore    
    }
    public class ATCScoreTypes
    {
        public string scoreValue { get; set; } // either a '+' or a '-'
        public string scoreName { get; set; } // meaning of the 1-3 characters above
    }

    public class MessagingContact
    {
        public int contactId { get; set; }
        public string contactName { get; set; }

        public string contactRole { get; set; }

    }

    public class CredentialsUpdateResponse
    {

        public List<Credential> credentials { get; set; }
        public Er er = new Er();

    }
}