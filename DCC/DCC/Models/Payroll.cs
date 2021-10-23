using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class Payroll : ViewModelBase
    {
        public int PeriodId { get; set; }

        public List<Period> Periods { get; set; }

        public List<ProviderSelect> Providers { get; set; }

        public bool hasISolved { get; set; }

    }
    public class ProviderSelect
    {
        public string providerName { get; set; }

        public int providerId { get; set; }
    }

    public class ProviderPayrollTimesheet
    {
        public string ProviderName { get; set; }

        public int ProviderId { get; set; }

        public List<TimeSheetEntry> TimeSheetEntries { get; set; }

        public List <PayrollCode> PayrollCodes { get; set; }

        public List<ValidDate> validDates { get; set; } = new List<ValidDate>();

    }

    public class TimeSheetEntry
    {
        public int id { get; set; }
        public string NoteDate { get; set; }
        public string ApprovedDate { get; set; }
        public string SupervisorApprovedDate { get; set; }
        public string SessionDate { get; set; }
        public string Code { get; set; }
        public string Start { get; set; }

        public string End { get; set; }

        public decimal Units { get; set; }


        public bool IsEditable { get; set; }
    }


    public class PayrollCode
    {
        public string Code { get; set; }

        public int RequiresHours { get; set; }



    }

    public class ISolvedPayrollLine
    {
        public string iSolvedID;
        public string PayRollCode;
        public decimal units;
        public string error;
    }

    public class ISolvedPayroll
    {
        public int payrollId { get; set; }
        public ISolvedPayrollLine[] payrollLines;
        public ISolvedPayrollStatus status;
    }
}