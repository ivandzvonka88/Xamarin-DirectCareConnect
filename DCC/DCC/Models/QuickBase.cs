using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models.QuickBase
{


    public class QuickBaseCompanyInfo
    {

        public int coId { get; set; }
        public string companyName { get; set; }
        public string companyConnection { get; set; }
        public string QBClientSkilledTbl { get; set; }
        public string QBClientSkilledCompanyId { get; set; }
        public string QBClientSkilledCompanyFieldId { get; set; }
        public string QBClientSkilledActiveField { get; set; }
        public string QBClientNonskilledTbl { get; set; }
        public string QBClientNonskilledCompanyId { get; set; }
        public string QBClientNonskilledCompanyFieldId { get; set; }
        public string QBClientNonskilledActiveField { get; set; }


        public string QBStaffNonSkilledTbl { get; set; }

        public string QBStaffSkilledTbl { get; set; }
        public string QBCompanyNameSkilled { get; set; }
        public int QBCompanySkilledAppType { get; set; }
        public string userToken { get; set; }
        public string quickbaseDomain { get; set; }

    }
    public class QBStaff
    {
        public int TCStaffId { get; set; }
        public int StaffId { get; set; }
        public int RoleId { get; set; }
        public string Position { get; set; }
        public bool IsActive { get; set; }
        public DateTime Modified { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SSN { get; set; }
        public DateTime DOB { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public string Dept1 { get; set; }
        
        public string payrollId { get; set; }
        public string phone { get; set; }
    }


   

    public class QuickBaseClientInfo
    {


        public List<Fields> fields{get; set;}
    }



    public class Fields
    {
        public int id { get; set; }
        public string label { get; set; }
        public string type { get; set; }
    }
    public class QBQuery
    {

        public string from;
        public string where;
        public int[] select;
        //   public SortBy[] sortBy;
        //    public GroupBy[] groupBy;

        public Option options;
    }

  


    public class GroupBy
    {
        public int fieldId;
        public string grouping;
    }
    public class SortBy
    {
        public int fieldId;
        public string order;
    }


    public class Option
    {
        public int skip;
        public int top;
    }
    public class QBData
    {
        public List<Field> fields { get; set; }
        //       public Metadata metadata { get; set; }
    }

    public class Field
    {
        public int id { get; set; }
        public string label { get; set; }
        public string type { get; set; }
        public Properties properties { get; set; }

    }

    public class Properties
    {
        public List<CompositeField> compositeFields { get; set; }
    }

    public class CompositeField
    {
        public int id { get; set; }

    }

    public class Metadata
    {
        public int totalRecords { get; set; }
        public int numRecords { get; set; }
        public int numFields { get; set; }

        public int id { get; set; }
    }

    public class QBError
    {
        public QBError()
        {
            code = 0;
            msg = "QuickBase API Successfully executed";

        }

        public int code { get; set; }
        public string msg { get; set; }
    }

    public class Er
    {
        public int code { get; set; }
        public string msg { get; set; }
    }
}