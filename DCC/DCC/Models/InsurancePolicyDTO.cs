using DCC.Helpers;
using DCC.Models.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DCC.Models
{

    public class InsurancePolicyDTO
    {
      
        public bool isAddNew { get; set; }
        public int companyId { get; set; }
        public string phone { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string addressLine1 { get; set; }

        public string addressLine2 { get; set; }
        public int genderId { get; set; }

        /*
                {
                    get
                    {
                        var response = -1;
                        switch (this.Gender)
                        {
                            case "Female"://GenderTypeEnum.Female:
                                response = 0;
                                break;
                            case "Male":
                                response = 1;
                                break;
                            default:
                                response = 2;
                                break;
                        }
                        return response;
                    }
                    set
                    {
                    }
                }
        */
        public string Gender { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string insuredIdNo { get; set; }
        public string postalCode { get; set; }
        public string mcid { get; set; }
        public string dob { get; set; }
        public string policyGroupNumber { get; set; }
  
        public int insRelId { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string companyName { get; set; }
        public string hasWaivers { get; set; }
        public int insurancePolicyID { get; set; }
        public string insuredName { get; set; }
        public int clientId { get; set; }
        public string AssistID { get; set; }
     


        public int InsurancePriorityID { get; set; }

        public string IsDeletable { get; set; }

        public bool Expired { get; set; }

        public bool Inactive { get; set; }

        public List<ClientServiceCPTRate> ClientServiceCPTRates { get; set; }
        public PolicyWaiver PolicyWaiverList { get; set; }
        public List<SelectListItem> DiagnosisCodes { get; set; }
        public List<Option> InsuranceRelationShips { get; set; }
        public List<Option> companyInsuranceList { get; set; }
        public List<Option> serviceList { get; set; }
        public List<ClientServices> clientServices { get; set; }
        public List<PolicyWaiverDTO> PolicyWaivers { get; set; }
        public List<PreAuthDTO> PreAuths { get; set; }
        
        public List<DDDClientAuth> DDDClientAuths { get; set; }

        public List<PolicyItem> PolicyList { get; set; }

    }

    public class PolicyItem
    {
        public int InsId { get; set; }
        public int InsPriorityId { get; set; }

        public string InsName { get; set; }

        public string InsStart { get; set; }
        public string InsEnd { get; set; }

    }


    public class DDDClientAuth
    {
        public int ID { get; set; }
        public string ServiceName { get; set; }
        public string HCPC { get; set; }
        public string HasWaiver { get; set; }
        public decimal DDDUnits { get; set; }

        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string DiagnosisCode { get; set; }
    }
}