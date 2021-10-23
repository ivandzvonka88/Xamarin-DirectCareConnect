using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class ClientService
    {
        public int clsvId { get; set; }
        public int clsvidId { get; set; }
        public int serviceId { get; set; }
        public bool deleted { get; set; }
        public string svc { get; set; }

        public string svcLong { get; set; }
        public string deptCode { get; set; }
        public int locId { get; set; }
        public string locNm { get; set; }

        public bool allowManualInOut { get; set; }
        public string status { get; set; }
        public byte ppl { get; set; }
        public string assignedRateName { get; set; }
        public int assignedRateId { get; set; }
        public int billingType { get; set; }
        public decimal mu { get; set; }
        public int rid { get; set; }
        public string rateName { get; set; }
        public decimal mum { get; set; }
        public bool mo { get; set; }
        public bool tu { get; set; }
        public bool we { get; set; }
        public bool th { get; set; }
        public bool fr { get; set; }
        public bool isHourly { get; set; }
        public bool isTherapy { get; set; }
        public bool isEvaluation { get; set; }
        public bool hasCareAreas { get; set; }
        public bool hasProgressReport { get; set; }
        public bool hasMaxHours { get; set; }
        public bool allowSpecialRates { get; set; }

        public bool selectRate { get; set; }
        public bool hasWeeklySchedule { get; set; }
        public decimal ppbill { get; set; }

        public bool dddPay { get; set; }
        public bool ppPay { get; set; }
        public bool pInsPay { get; set; }
        public string nextReportDueDate { get; set; }
        public string nextATCMonitoringVisit { get; set; }

        public string ISPStart { get; set; }
        public string ISPEnd { get; set; }

        public string POCStart { get; set; }
        public string POCEnd { get; set; }

        public string contingencyPlan { get; set; }
        public string contingencyPlanId { get; set; }

        public int reportingPeriodId { get; set; }

        public List<SelectOption> reportPeriodEndDateOption { get; set; }
        public List<SelectOption> contingencyPlans { get; set; }

        public List<Auth> auths { get; set; }
        public List<SpecialRate> specialRates { get; set; }

        public List<InsurancePreAuth> insurancePreAuths{ get; set; }
        public List<SelectOption> assignableRates { get; set; }
    }

    public class NewServices
    {
        public List<SelectOption> serviceOptions { get; set; }

    }
  


    public class UploadChartModal
    {
        public List<SelectOption> chartDocTypes { get; set; }
        public List<SelectOption> serviceOptions { get; set; }
    }


    public class ManualInOutOn{
        public int clsvidId { get; set; }
        public bool on { get; set; }
        public Er er = new Er();
    }
}