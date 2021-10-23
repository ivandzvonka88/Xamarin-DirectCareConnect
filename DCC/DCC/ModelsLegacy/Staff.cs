using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using DCC.Models;

namespace DCC.Models.Staff
{
    public class StaffMember
    {
        public string name { get; set; }
        public string id { get; set; }

    }
    public class StaffMessagingInit : ViewModelBase
    {
        public string sendbirdId { get; set; }
        public List<StaffMember> staffList { get; set; }

        public string staffListJson
        {
            get
            {
                return Json.Encode(staffList);
            }
        }


        public Er er = new Er();
    }



    public class Staff
    {
        public string name { get; set; }
        public int id { get; set; }
        public string sendBirdUserId { get; set; }
        public bool deleted { get; set; }
        public bool registered { get; set; }
    }

    public class StaffInit : ViewModelBase
    {
        public List<Staff> staffList { get; set; }
        public List<Option> employeeRoles { get; set; }

        public string staffListJson
        {
            get
            {
                return Json.Encode(staffList);
            }
        }


        public Er er = new Er();
    }
    
    public class Option
    {
        public string id { get; set; }
        public string name { get; set; }
    }
    public class Checkbox
    {
        public string id { get; set; }
        public string name { get; set; }

        public bool isChecked {get; set;}
    }

    public class StaffData
    {
        public string tgtView = "staffView";

        public string userLevel { get; set; }
        public int userPrId { get; set; }
        public bool isNewStaff { get; set; }
        public int prId { get; set; }

        public int roleId { get; set; }
        public string roleName { get; set; }
        public int roleSupervisoryLevel { get; set; }
        public string name { get; set; }
        public string fn { get; set; } //first name
        public string ln { get; set; } // last name
        public string mi { get; set; } // middle initial
        public string cl { get; set; } // cell phone
        public string ph { get; set; } // land line 
        public string em { get; set; } // email
        public string ad1 { get; set; } // address
        public string ad2 { get; set; } // apt #
        public string cty { get; set; } // city
        public string st { get; set; } // state
        public string z { get; set; } // zip
        public string sex { get; set; } // M or F
        public string dobf { get; set; } // date of birth
        public string dobfISO { get; set; } // date of birth
        public string ssnf { get; set; } // social security

        public string title { get; set; }
        public string ahcccsId { get; set; } // AHCCCS Id
        public string npi { get; set; } // NPI
        public string CAQH { get; set; }
        public string MedicaidId { get; set; }
        public string hiredtf { get; set; } // hire date
        public string termdt { get; set; } // termination date
        public string CRverf { get; set; } // central registry date
        public string hiredtfISO { get; set; } // hire date
        public string termdtISO { get; set; } // termination date
        public string CRverfISO { get; set; } // central registry date
        public string employeeType { get; set; } // employee/contractor
        public string classification { get; set; } // executive/direct care etc
      //  public string providesTransport { get; set; } // is a company driver
      //  public string ownVehicle { get; set; } // uses their own vehicle
        public string providerHome { get; set; } // provider uses own home
        public string refOfficeId { get; set; } // reff Id for provider home
        public bool profLicReq { get; set; } // professional license required
        public bool profLiabilityReq { get; set; } // professional license required
        public string prdept { get; set; } //pay roll dept name
        public int prdeptId { get; set; } //payroll dept id
        public bool isSalary { get; set; } //payroll dept id
        public string eId { get; set; } // employee payroll Id

        public string iSolvedID { get; set; } // employee iSolved payroll Id

        public string supName { get; set; } // supervisor
        public int supRoleId { get; set; } // supervisor role
        public int supId { get; set; } // supervisor role Id

        public string tempsupName { get; set; } // supervisor
        public int tempsupRoleId { get; set; } // supervisor role
        public int tempsupId { get; set; } // supervisor role Id


        
        public bool qualifiedTherapist { get; set; }
        public bool assistantTherapist { get; set; }
        public bool physicalTherapy { get; set; }
        public bool occupationalTherapy { get; set; }
        public bool speechTherapy { get; set; }
        public bool basicProvider { get; set; }
      public bool BCBA { get; set; }
        public bool RBT { get; set; }
        public int userId { get; set; }
        public string oldEmail { get; set; }

        public bool registered { get; set; } // has gone through online registration
        public bool deleted { get; set; } // is inactive

        public List<IprComment> comments { get; set; }

        public List<Credential> credentials { get; set; }
        public List<StaffService> staffServices { get; set; }
        public List<Option> staffRoles { get; set; }
        public List<Option> weeks { get; set; }

        public List<Checkbox> otherRequiredCredentials { get; set; }

        public List<Option> allStaffServices { get; set; }

       
        public string zipCodes { get; set; }


        public List<Option> relationships { get; set; }

        public List<Option> employeeRoles { get; set; }
 

        public ProviderHours providerHours = new ProviderHours();
        public StaffSupervisorList staffSupervisorList = new StaffSupervisorList();

        public bool mfaEnabled { get; set; }

        public Er er = new Er();

    }

    public class EmployeeRoleList
    {
        public List<Option> employeeRoles { get; set; }
        public Er er = new Er();
    }

    


    public class StaffMessage
    {
        public string prId { get; set; }
        public string fn { get; set; }
        public string ln { get; set; }

        public string message { get; set; }


    }


    public class NewStaffMemberResponse
    {
        public string fn { get; set; }
        public string ln { get; set; }
        public int prId { get; set; }
        public Er er = new Er();

    }


    public class NewStaffMember
    {
        public string fn { get; set; }
        public string ln { get; set; }
        public string cl { get; set; }
        public string em { get; set; }

        public bool qualifiedTherapist { get; set; }
        public bool assistantTherapist { get; set; }
        public bool basicProvider { get; set; }
        public bool occupationalTherapy { get; set; }
        public bool physicalTherapy { get; set; }
        public bool speechTherapy { get; set; }

        public bool BCBA { get; set; }
        public bool RBT{ get; set;}
        public string role { get; set; }
    }


 

    public class CredentialList
    {
        int prId { get; set; }
        public List<RequiredCredential> requiredCredentials { get; set; }// requied credentials
        public Credential[] credentials { get; set; }
        public Er er = new Er();
    }

    public class Credential
    {
        public int prId { get; set; }
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

    public class IprCommentList
    {
        public List<IprComment> comments { get; set; }
        public Er er = new Er();
    }

    public class IprComment
    {
        public int id { get; set; }
        public string comment { get; set; }
        public string name { get; set; }
        public string date { get; set; }
        public int prId { get; set; }
    }

    public class StaffServiceList
    {

        public List<StaffService> staffServices { get; set; }
        public Er er = new Er();
    }

    public class StaffService
    {
        public int relId { get; set; } // relationship Id
        public int clsvId { get; set; } //Client Id
        public string clientName { get; set; } // Client Name
        public int clsvidId { get; set; } // Client Service Id

        public string svcLong { get; set; }
        public string service { get; set; } // Service Name
        public string relationship { get; set; } // client/staff relationship data for attendant care
        public int prIdr { get; set; } // staff relationship to client
        public int prId { get; set; } // staff Id
    }

    public class StaffPayrollQuery
    {
        public int prId { get; set; }
        public string startDate { get; set; }
    }

    public class StaffPayRoll
    {
        public string payRollCode { get; set; }
        public decimal hours { get; set; }

    }


    public class CredentialVerificationUpdate
    {
        public int prId { get; set; }
        public int credId { get; set; }
        public bool verified { get; set; }
    }

    public class Message
    {
        public int prId { get; set; }
        public string message { get; set; }
    }

    public class StaffActive
    {
        public int prId { get; set; }
        public bool deleted { get; set; }
        public Er er = new Er();
    }
    public class StaffSupervisorList
    {
        public bool hasSupervisors { get; set; }
        public List<Option> supervisors{ get; set; }
        public Er er = new Er();
    }
}