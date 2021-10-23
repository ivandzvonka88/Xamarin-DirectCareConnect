using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DCC.Models.SessionNotes;
namespace DCC.Models.Home
{

    public class PageInitializer : ViewModelBase
    {

    }

    public class HomeStaffPage
    {
        public string fn { get; set; }
        public string ln { get; set; }
        public int userPrId { get; set; }
        public string userLevel { get; set; }
        public int prId { get; set; }
        public string staffLevel { get; set; }
        public string tgtView { get; set; }
        public List<ClientAlert> clientAlerts { get; set; }
        public List<StaffAlert> staffAlerts { get; set; }
        public List<CommentHistory> commentHistory { get; set; }
        public List<NewAuthorization> newAuthorizations { get; set; }

        public List<ScheduleChangeRequest> scheduleChangeRequests { get; set; } = new List<ScheduleChangeRequest>();


        public List<Staff> supervisors { get; set; }
        public List<Staff> providers { get; set; }
        public List<Client> clients { get; set; }
        // public List<StaffCredential> staffCredentials { get; set; }
        public List<Credential> credentials { get; set; }
        public List<RequiredCredential> requiredCredentials { get; set; }// required credentials
        public List<PendingDocumentation> pendingDocumentation { get; set; }
        public ProviderBillingWeek billingData { get; set; }

        //      public ProviderBilling[] providerBilling { get; set; }
        public StaffClientService[] staffClientServices { get; set; }
    
        public ProviderHours providerHours = new ProviderHours();
  

        public Er er = new Er();
    }

   

   

    public class PendingDocumentation
    {
        public int docId { get; set; }
        public string docType { get; set; }

        public int clientId { get; set; }
        public bool completed { get; set; }
        public bool approved { get; set; }
        public bool lostSession { get; set; }
        public string clientName { get; set; }
        public string providerName { get; set; }
        public string providerName2 { get; set; }
        public string dueDt { get; set; }

        public string svc { get; set; }
        public string noteType { get; set; }
        public string status { get; set; }

        public bool requiresLocation { get; set; }
        public int clientLocationId { get; set; }

    }
    public class ProviderBillingWeek
    {
        public string ws { get; set; }
        public string we { get; set; }
        public ProviderBilling[] providerBilling { get; set; }
        public decimal colR2to1 { get; set; }
        public decimal colR1to1 { get; set; }
        public decimal colR1to2 { get; set; }
        public decimal colR1to3 { get; set; }
        public decimal colTot { get; set; }
    }

    public class ProviderBilling
    {
        public string svc { get; set; }
        public decimal R2to1 { get; set; }
        public decimal R1to1 { get; set; }
        public decimal R1to2 { get; set; }
        public decimal R1to3 { get; set; }
        public decimal rowTot { get; set; }
    }

    public class NewAuthorization
    {
        public int clsvId { get; set; }
        public string nm { get; set; }
        public string svc { get; set; }
        public string stDate { get; set; }
        public string edDate { get; set; }
        public decimal units { get; set; }
    }

    public class Staff
    {
        public string prId { get; set; }
        public string sNm { get; set; }
        public string userLevel { get; set; }
        public string sendBirdUserId { get; set; }
    }
    public class Client
    {
        public string clsvId { get; set; }
        public string cNm { get; set; }
    }



  
    public class StaffClientService
    {
        public int id { get; set; }
        public int clsvId { get; set; }
        public string clnm { get; set; }
        public string prnm { get; set; }
        public string svc { get; set; }
        public string rel { get; set; }
    }

    public class Credential
    {
        public int credId { get; set; }
        public int credTypeId { get; set; }
        public string credName { get; set; }
        public string docId { get; set; }
        public string validFrom { get; set; }
        public string validTo { get; set; }
        public string verifiedBy { get; set; }
        public string verificationDate { get; set; }

        public string status { get; set; }
        public string statusColor { get; set; }
        public bool btnAddNew { get; set; }
        public bool btnEdit { get; set; }
        public bool btnVerify { get; set; }
        public bool btnMail { get; set; }
        public bool btnView { get; set; }

    }
    public class CredentialVerificationUpdate
    {
        public int prId { get; set; }
        public int credId { get; set; }
        public bool verified { get; set; }
        public string tgtView { get; set; }
    }

  
    public class ProviderNoteRequest
    {
        public int clsvId { get; set; }
        public int docId { get; set; }


    }

    public class ProgressReport
    {
        public int docId { get; set; }
        public string signee { get; set; }
        public string signeeCredentials { get; set; }
        public bool isTherapistSupervisor { get; set; }
        public bool approval { get; set; }
        public string reportingPeriod { get; set; }
        public string formType { get; set; }
        public string clientName { get; set; }
        public int clientId { get; set; }
        public string dt { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string goalsToAdd { get; set; }
        public string treatmentFrequencyId { get; set; }
        public string treatmentStart { get; set; }
        public string treatmentEnd { get; set; }
        public int numberOfVisits { get; set; }
        public string treatmentFrequency { get; set; }
        public string treatmentDurationId { get; set; }

        public int serviceId { get; set; }
        public int clientServiceId { get; set; }
        public string serviceName { get; set; }
        public string svc { get; set; }

        public string rejectedReason { get; set; }
        public string completedDt { get; set; }
        public string attachmentName { get; set; }
        public string extension { get; set; }
        public bool hasAttachment { get; set; }
        public bool teletherapy { get; set; }
        public List<Question> questions { get; set; }
        public List<LongTermObjective> longTermObjectives { get; set; }
        public List<Option> goalAreas { get; set; }
        public List<Option> frequencies { get; set; }
        public List<Option> durations { get; set; }
        public List<Option> statuses { get; set; }
        public int? providerId { get; set; }

        public NoteAcceptance approvalNote { get; set; }

        // added for evaluation as eval is session based and location or in/out adjustments may be needed
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
        public AdjustmentInfo adjustmentInfo { get; set; }

     

        public Er er = new Er();
    }
 
    public class Appointment
    {
        public string date { get; set; }

        public string time { get; set; }
        public string provider { get; set; }

        public string title { get; set;}
        public string status { get; set; }
        public bool teletherapy { get; set; }
        public int locationTypeId { get; set; }

        public string location { get; set; }
        public int ratio { get; set; }
    }

    public class ProgressReportPdf
    {
        
        public string reportingPeriod { get; set; }
        public string formType { get; set; }
        public string clientName { get; set; }
        public string setting { get; set; }
        public string serviceStartDate { get; set; }
        public int year { get; set; }
        public string month { get; set; }

        public string completedBy { get; set; }
        public string completedByCredentials { get; set; }
        public string completedByTitle { get; set; }
        public string completedByPhone { get; set; }
        public string completedByEmail { get; set; }
        public string completedDt { get; set; }
        public string approvedBy { get; set; }
        public string approvedByCredentials { get; set; }

        public string format { get; set; }
        public string clId { get; set; }
        public string dt { get; set; }
        public string npi { get; set; }
        public string agency { get; set; }
        public string agencyPhone { get; set; }

        public string goalsToAdd { get; set; }
        public string treatmentStart { get; set; }
        public string treatmentEnd { get; set; }
        public int numberOfVisits { get; set; }
        public string treatmentDuration { get; set; }
        public string treatmentFrequency { get; set; }
        public int duration { get; set; }

        public string dob { get; set; }
        public string diagnosis { get; set; }
        public string physician { get; set; }
        public string physicianAgency { get; set; }
        public string physicianPhone { get; set; }

        public string physicianEmail { get; set; }
        public string physicianNpi { get; set; }

        public string clientWorker { get; set; }

        public string therapySupervisor { get; set; }
        public string therapySupervisorTitle { get; set; }
        public string therapySupervisorPhone { get; set; }

        public string responsiblePerson { get; set; }
        public string responsiblePersonPhone { get; set; }
        public string responsiblePersonRelationship { get; set; }
        public int sessionCount { get; set; }

        public List<Provider> providers { get; set; }

        public string lastEvaluationDate { get; set; }
        public int serviceId { get; set; }
        public int clsvId { get; set; }
        public string serviceName { get; set; }
        public string svc { get; set; }
        public int docId { get; set; }
        public string note { get; set; }
        public string attachmentName { get; set; }
        public string extension { get; set; }
        public bool hasAttachment { get; set; }

        public string fileIdentifier { get; set; }

        public List<Appointment> appointments { get; set; }
        public List<Question> questions { get; set; }
        public List<LongTermObjective> longTermObjectives { get; set; }
        public List<Option> scoring { get; set; }
        public int providerId { get; set; }

        public string currentDate { get; set; }
        public string currentTime { get; set; }

        public Er er = new Er();
    }
    
    
    public class Question
    {
        public int questionId { get; set; }
        public string title { get; set; }
        public string answer { get; set; }

        public bool isRequired { get; set; }

        public bool supervisorOnly { get; set; }
    }

    public class Provider
    {
        public string name { get; set; }
        public string title { get; set; }

        public string phone { get; set; }

    }
   

 
    public class AtcMonitoringResponse
    {
        public string cnm { get; set; }
        public string dt { get; set; }
        public int clsvId { get; set; }
        public int clsvidId { get; set; }
        public string serviceStartDate { get; set; }
        public int providerId { get; set; }

        public string clwNm { get; set; }
        public string guardian { get; set; }
        public string svc { get; set; }
        public int atcMonitorId { get; set; }

        public bool anc { get; set; }
        public bool afc { get; set; }
        public bool hsk { get; set; }
        public bool days5 { get; set; }
        public bool days30 { get; set; }
        public bool days60 { get; set; }
        public bool days90 { get; set; }
        public string visitDt { get; set; }
        public int nextVisit { get; set; }

        public List<AtcQuestion> atcQuestions { get; set; }
        public List<Option> providers { get; set; }

        public int prId { get; set; }
        public Er er = new Er();
    }

    public class AtcQuestion
    {
        public int atcQuestId { get; set; }
        public string question { get; set; }
        public string qNum { get; set; }
        public bool yes { get; set; }
        public bool no { get; set; }
        public bool na { get; set; }
        public string cmt { get; set; }
    }
    
    public class NoteAcceptance
    {
        public string providerId { get; set; }
        public int docId { get; set; }

        public int sequenceNumber { get; set; }
        public bool rejected { get; set; }
        public string rejectedReason { get; set; }
    }


    public class HabProgressReport
    {
        public int docId { get; set; }

        public string companyName { get; set; }
        public int sequenceNumber { get; set; }
        public bool approval { get; set; }
        public int providerId { get; set; }
        public string client { get; set; }
        public string assistId { get; set; }
        public string clientWorker { get; set; }
        public string dob { get; set; }
        public string service { get; set; }
        public string provider { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public int year { get; set; }


        public List<ScoringKey> scoringKeys { get; set; }
        public ProgressReportGoal[] progressReportGoals { get; set; }
        public Er er = new Er();
    }
    public class ScoringKey
    {
        public string key { get; set; }
        public string name { get; set; }
    }

    public class ProgressReportGoal
    {
        public int objectiveId { get; set; }
        public int goalId { get; set; }
        public string goal { get; set; }
        public string shortTermGoal { get; set; }
        public string teachingMethod { get; set; }
        public string note { get; set; }

        public MonthlyScores[] monthlyScores { get; set; }
    }

    public class MonthlyScores
    {
        public int monthNumber { get; set; }
        public string month { get; set; }
        public string[] score = new string[31];

    }


    
  
    public class SessioNote
    {
        public int docId { get; set; }
        public bool complete { get; set; }
        public bool noShow { get; set; }
        public bool teletherapy { get; set; }
        public int providerId { get; set; }
        
        public int clientId { get; set; }
        public int clientServiceId { get; set; }

        public int serviceId { get; set; }

        public string svc { get; set; }

        public bool supervisorPresent { get; set; }
        public string note { get; set; }
        public List<SessionScore> sessionScores { get; set; }


        public AdjustmentInfo adjustmentInfo { get; set; }
        public string attachmentName { get; set; }
    }

    public class AdjustmentInfo
    {
        public string dt { get; set; }

    
        public string utcIn { get; set; }
        public string utcOut { get; set; }

        public string adjDt { get; set; }

        public string adjUtcIn { get; set; }
        public string adjUtcOut { get; set; }

        public string clientLocationValue { get; set; }
    }

    public class SessionScore {
        public int goalId { get; set; }
        public string score { get; set; }
        public string trialPct { get; set; }
    }
 
    public class HabObjective
    {
        public int clientId { get; set; }
        public int longTermGoalId { get; set; }
        public string longTermGoalAreaId { get; set; }
        public string serviceGoalArea { get; set; }
        public string longTermVision { get; set; }
        public string longTermGoal { get; set; }
        public List<GoalInfo> goalInfo { get; set; }
        public string completedDate { get; set; }
        public string longTermGoalStatus { get; set; }
        public bool deleted { get; set; }

        public string goalType { get; set; }
    }

    public class GoalInfo
    {
        public int shortTermGoalId { get; set; }
        public string shortTermGoal { get; set; }
        public string teachingMethod { get; set; }
        public string frequency { get; set; }
        public int frequencyId { get; set; }
        public int step { get; set; }

        public string shortTermGoalStatus { get; set; }
        
    }

 

    public class Option
    {
        public string name { get; set; }
        public string value { get; set; }

    }

  
}