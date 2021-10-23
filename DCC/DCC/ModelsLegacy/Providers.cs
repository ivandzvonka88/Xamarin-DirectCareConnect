using DCC.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Web;

namespace DCC.Models.Providers
{
    public class Provider
    {
        public string name { get; set; }
        public string payerId { get; set; }

        public int insuranceCompanyId { get; set; }

        public string line1 { get; set; }
        public string line2 { get; set; }

        public string city { get; set; }

        public string state { get; set; }

        public string postalCode { get; set; }
        public string phone { get; set; }
    }

    public class InsurancePolicy
    {
        public int InsurancePolicyId { get; set; }
        public string Company { get; set; }
        public string PolicyNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        public DateTime? InsuredDoB { get; set; }
        public string Gender { get; set; }
        public string MCID { get; set; }
        public string PatientIdNo { get; set; }
        public string Relationship { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string city { get; set; }
        public string state { get; set; }
        public string AddressLine { get; set; }
        public string PostalCode { get; set; }
    }

    public class ClientInfo
    {
        public string name { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        public DateTime dob { get; set; }
        public int clientId { get; set; }
        public int filteredCount { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string adLine { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string postalCode { get; set; }
        public string sex { get; set; }
        public string[] DiagnosisCodes { get; set; }
        public string claimIds { get; set; }
        public List<ClaimsCount> numberOfClaims { get; set; }
    }
    // Client Details //
    public class ClientDetailInfo
    {
        public string provider { get; set; }
        public string cptCode { get; set; }
        [DataType(DataType.Currency)]
        public decimal billedAmount { get; set; }
        [DataType(DataType.Currency)]
        public decimal paidAmount { get; set; }
        [DataType(DataType.Currency)]
        public decimal allowedAmount { get; set; }
        [DataType(DataType.Currency)]
        public decimal coInsuranceAmount { get; set; }
        public string policyNumber { get; set; }
        public string groupNumber { get; set; }
        //[DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        public string dddStartDate { get; set; }
        //[DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        public string dddEndDate { get; set; }
        //[DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        public DateTime StartDate { get; set; }
        //[DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        public DateTime EndDate { get; set; }
        public string dddUnit { get; set; }
        public long claimId { get; set; }
        public ICollection<ClaimComment> comments { get; set; }

        public int InsurancePolicyId { get; set; }
        public DateTime DateOfService { get; set; }
        public int ClaimStatusID { get; set; }
        public string ClaimStatus { get; set; }
        public string Client { get; set; }
        public int paymentId { get; set; }
        public DateTime? paymentDate { get; set; }
        public string PresStart { get; set; }

        public string PresEnd { get; set; }

        public string PAuthStart { get; set; }
        public string PAuthEnd { get; set; }

        public int serviceId { get; set; }
        public int staffId { get; set; }
        public int clientSessionTherapyID { get; set; }
        public int clientChartFileId { get; set; }
        public string clientChartFileExtension { get; set; }
        public string clientChartFileName { get; set; }
        public int progressReportFileId { get; set; }
        public string progressReportFileExtension { get; set; }
        public string progressReportFileName { get; set; }
        public int noteTherapyFileId { get; set; }
        public string noteTherapyFileExtension { get; set; }
        public string noteTherapyFileName { get; set; }
        public int insurancePriority { get; set; }
        public string therapistSupervisor { get; set; }

        public int SessionTherapyStatusID { get; set; }
        public int LocationTypeId { get; set; }
        public string DenialReason { get; set; }
        public string DenialReasonText { get; set; }
        public int DeductibleInd { get; set; }
        public string ClaimAgingBucket { get; set; }
    }

    public class ClaimComments
    {
        public ICollection<ClaimComment> comments { get; set; }
    }
    public class ClaimComment
    {
        public int StaffId { get; set; }
        public string staffName { get; set; }
        public string fn { get; set; }
        public string ln { get; set; }
        public DateTime? MadeAt { get; set; }
        public string Comments { get; set; }
    }

    public class PaymentAmount
    {
        public decimal billedamount { get; set; }
        public decimal coInsuranceAmount { get; set; }
        public decimal allowedAmount { get; set; }
        public decimal paidAmount { get; set; }
        public int policyId { get; set; }
        //public int claimStatusId { get; set; }
        public long claimId { get; set; }
        public int insurancePriority { get; set; }
        public long clientSessionTherapyId { get; set; }
        public DateTime? paymentDate { get; set; }
    }
    public class Option
    {
        public string name { get; set; }
        public string value { get; set; }

    }
    public class ClientDetails
    {
        [DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        public DateTime dos { get; set; }

        public List<ClientDetailInfo> ClientDetailsInfoList;
    }
    public class DenialReason
    {
        public string name { get; set; }
        public string id { get; set; }

    }
    public class ProviderInit : ViewModelBase
    {
        public List<Provider> providerList;
        public List<ClaimsStatus> claimStatusList;
        public List<ClientInfo> clientInfoList;
        public List<ClientDetails> ClientDetailsList;
        public InsurancePolicy InsurancePolicy;
        public List<Option> companyInsuranceList { get; set; }
        public Er er = new Er();
        public List<DenialReason> DenialReasonList;
        public List<ClaimPayment> ClaimPaymentList;
        public List<PaymentAmount> OtherClaimPayments { get; set; }
        public List<GovernmentProgram> GovernmentPrograms { get; set; }
        public List<Option> serviceList { get; set; }
        public string defaultReason { get; set; }
    }
    public class ClaimsStatus
    {
        public int claimstatusid { get; set; }
        public string name { get; set; }
        public string statusType { get; set; }
    }
    public class ClaimsApprove
    {
        public int providerId { get; set; }
        public Er er = new Er();
    }

    public class ClaimPayment
    {
        public string DOS { get; set; }
        public string Code { get; set; }
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal AllowedAmount { get; set; }
        public decimal CoInsAmount { get; set; }
        public int ClaimId { get; set; }
        public int GovernmentProgramId { get; set; }
        public int InsurancePolicyId { get; set; }
        public string InsuredIdNo { get; set; }
        public int InsuranceCompanyId { get; set; }
        public bool IsDenial { get; set; }
        public int PaymentId { get; set; }
        public string Payer { get; set; }
        public string Notes { get; set; }
        public DateTime ReceivedAt { get; set; }
        public int PaymentTypeId { get; set; }
        public DateTime VoidedAt { get; set; }
        public int DenialReasonId { get; set; }
        public string ReasonText { get; set; }
        public int StaffId { get; set; }
        public long OABatchID { get; set; }
        public int HCFAFileId { get; set; }
        public string InsuranceCompany { get; set; }
        public DateTime PaymentDate { get; set; }
        public int DeductibleInd { get; set; }
        public decimal DeductibleAmount { get; set; }
        public int DeductibleReasonCode { get; set; }
    }
    public class GovernmentProgram
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int InsuranceID { get; set; }
    }
    public class BillingInsuranceCompanyDTO
    {
        public int InsuranceCompanyId { get; internal set; }
        public string Name { get; internal set; }
        public string Code { get; internal set; }
        public bool? ExcludeRenderer { get; internal set; }
    }
    public class ClientDTO
    {
        public int ClientId { get; internal set; }
        public string LastName { get; internal set; }
        public string FirstName { get; internal set; }
        public string MiddleName { get; internal set; }
        public AddressDTO Address { get; internal set; }
        public DateTime? DoB { get; internal set; }
        public int? GenderId { get; internal set; }
        public List<DiagnosisCodeDTO> DiagnosisCodes { get; internal set; }
        public List<PolicyDTO> Policies { get; internal set; }
        //???
        public bool? IsTelehealth { get; internal set; }
        //???
        public DateTime TelehealthUpdatedAt { get; internal set; }
    }
    public class CompanyInfoDTO
    {
        public string Name { get; internal set; }
        public string TaxId { get; internal set; }
        public string Phone { get; internal set; }
        public string NPI { get; internal set; }
        public string CompanyCode { get; set; }
        public string NextDDDFileName { get; set; }
        public int CompanyID { get; set; }
        public string BlobStorageConnection { get; set; }
        public AddressDTO Address { get; internal set; }
        public string ProvAhcccsId { get; set; }
        public string DDDPrefix { get; set; }
        public string SkilledBillingAddress { get; set; }
        public string SkilledBillingContactName { get; set; }
        public string SkilledBillingPhone { get; set; }
        public string SkilledBillingEmail { get; set; }
        public string SkilledBillingZipCode { get; set; }
        public string SkilledBillingCity { get; set; }
        public string SkilledBillingState { get; set; }
    }
    public class AddressDTO
    {
        public string Line1 { get; internal set; }
        public string Line2 { get; internal set; }
        public string City { get; internal set; }
        public string State { get; internal set; }
        public string PostalCode { get; internal set; }
    }
    public class StaffDTO
    {
        public int UserId { get; set; }
        public string NPI { get; internal set; }
        public string MedicaidID { get; internal set; }
        public string LastName { get; internal set; }
        public string FirstName { get; internal set; }
        public AddressDTO Address { get; internal set; }
    }
    public class CredentialDTO { }
    public class TeleHealthDTO
    {
        public object ServiceCPTRateID { get; internal set; }
        public string CPTCode { get; internal set; }
        public string Mod1 { get; internal set; }
        public string Mod3 { get; internal set; }
        public string Mod2 { get; internal set; }
        public int ClaimId { get; internal set; }
    }
    public class InsurancePolicyDTO
    {
        public InsurancePolicyDTO()
        {
            this.Waivers = new List<WavierDTO>();
        }
        public int InsurancePolicyId { get; set; }
        public int ClientId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public long? AuditActionId { get; set; }
        public int? InsuranceRelationshipId { get; set; }
        public AddressDTO Address { get; set; }
        //public string Number { get; set; }
        public DateTime InsuredDoB { get; set; }
        public string InsuredIdNo { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int InsuranceCompanyId { get; set; }
        public string Phone { get; set; }
        public string MCID { get; set; }
        public string Version { get; set; }
        public DateTime LastChecked { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? GenderId { get; set; }
        public string PatientIdNo { get; set; }
        public long ClaimId { get; set; }
        public string PolicyNumber { get; set; }
        public string InsuranceCompanyName { get; set; }
        public string ClientFirst { get; set; }
        public string ClientMiddle { get; set; }
        public string ClientSuffix { get; set; }
        public string ClientLast { get; set; }
        public DateTime? ClientDOB { get; set; }
        public int WaiverCount { get; set; }
        public List<WavierDTO> Waivers { get; set; }
        public int InsuranceTierId { get; set; }
        public List<ClientServiceCPTDTO> ClientServiceCPTs { get; set; } = new List<ClientServiceCPTDTO>();
        public bool IsGovt { get; set; }
        public string InsCompanyCode { get; set; }
    }
    public class WavierDTO
    {
        public int PolicyWaiverId { get; set; }
        public int? ClientServiceId { get; set; }
        public int InsurancePolicyId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsApplicable { get; set; }
        public string Units { get; set; }
        public bool? IsDDD { get; set; }

    }
    public class ClientClaimDTO
    {
        public ClaimDTO Claim { get; internal set; }
        public int ClaimId { get; internal set; }
        public bool? IsNonTelehealth { get; internal set; }
    }
    public class ClaimDTO
    {
        public ClaimDTO()
        {
            this.Appointments = new List<AppointmentDTO>();
            this.Payments = new List<ClaimPaymentDTO>();
            this.Comments = new List<ClaimCommentDTO>();
            //this.ClientServiceCPTRates = new List<ClientServiceCPTDTO>();
            this.DiagnosisCodes = new List<ClientDiagnosisCodeDTO>();
            this.ClaimComments = new List<ClaimCommentDTO>();
        }
        public List<ClientDiagnosisCodeDTO> DiagnosisCodes { get; set; }
        public InsurancePolicyDTO InsurancePolicy { get; set; }
        public int? InsurancePolicyId { get; set; }
        public int? StaffId { get; set; }
        public int ApproverStaffId { get; set; }
        public string InsuredIdNo { get; set; }
        public int? InsuranceCompanyId { get; set; }

        public int? GovernmentProgramId { get; set; }
        public PolicyDTO Policy { get; internal set; }
        public int ClientId { get; internal set; }
        //????
        public List<AppointmentDTO> Appointments { get; internal set; }
        public DateTime StatusUpdatedAt { get; internal set; }
        public List<ClaimPaymentDTO> Payments { get; internal set; }
        public int? LocationTypeId { get; internal set; }
        public AddressDTO PlaceOfService { get; set; }
        public int ApproverUserId { get; internal set; }
        public int? StaffUserID { get; internal set; }
        public DateTime ClaimDate { get; internal set; }
        public int ClaimId { get; internal set; }
        public bool? IsNonTelehealth { get; internal set; }
        //public decimal AmountDue { get; internal set; }
        public string ClientGovtProgramId { get; internal set; }
        public int StatusId { get; internal set; }
        public string ProviderNPI { get; internal set; }
        public string ProviderStateMedicaid { get; internal set; }
        public string ProviderFirstName { get; internal set; }
        public string ProviderLastName { get; internal set; }
        public AddressDTO ProviderAddress { get; internal set; }
        //public object ApproverStaffId { get; internal set; }
        public string OrderingPhysicianNPI { get; internal set; }
        public string OrderingPhysicianFirstName { get; internal set; }
        public string OrderingPhysicianLastName { get; internal set; }
        public object Comments { get; internal set; }
        public List<ClaimCommentDTO> ClaimComments { get; set; }
        public object SubStatus { get; internal set; }
        public decimal AmountDue
        {
            get
            {
                decimal toReturn = 0;
                if (this.Appointments == null || !this.Appointments.Any())
                {
                    return toReturn;
                }
                this.Appointments.ForEach(x =>
                {
                    toReturn += (x.StatusId == (int)AppointmentStatusEnum.NoShow ? x.Amount / 2 : x.Amount).GetValueOrDefault();

                });
                return toReturn;
            }
            internal set { }
        }

        public string ActivityId { get; internal set; }
        public long AppointmentId { get; internal set; }
        public int DeductibleInd { get; set; }
        public decimal? DeductibleAmt { get; set; }
        public int? DeductibleReasonCode { get; set; }

        public EDIClaim ToEDIClaim(List<BillingInsuranceCompanyDTO> insComp, bool isAZDDD = false)
        {
            var matchingInsComp = insComp.FirstOrDefault(x => x.InsuranceCompanyId == InsuranceCompanyId);
            var insPayerId = matchingInsComp?.Code ?? "0";
            var excludeProviderInd = matchingInsComp?.ExcludeRenderer ?? false;
            var appointment = Appointments.First();
            var patientPymt = Payments.Where(py => !py.VoidedAt.HasValue && py.Amount.GetValueOrDefault(0) > 0 && py.PaymentTypeId == (int)PaymentTypeEnum.Private)?.Sum(py => py.Amount.GetValueOrDefault(0));

            EDIClaim ediClaim = new EDIClaim
            {
                ClaimId = ClaimId,
                StatusUpdatedAt = StatusUpdatedAt,
                InsurancePolicyId = (int)InsurancePolicyId,
                InsuranceCompanyName = matchingInsComp.Name.Trim(),
                InsuranceCompanyPayerId = insPayerId,
                InsurancePolicyRelationshipId = InsurancePolicy?.InsuranceRelationshipId,
                InsurancePolicySequence = InsurancePolicy?.InsuranceTierId,
                InsurancePolicySubscriberFirstName = InsurancePolicy?.FirstName,
                InsurancePolicySubscriberLastName = InsurancePolicy?.LastName,
                InsurancePolicySubscriberAddressLine1 = InsurancePolicy?.Address.Line1,
                InsurancePolicySubscriberAddressCity = InsurancePolicy?.Address.City,
                InsurancePolicySubscriberAddressState = InsurancePolicy?.Address.State,
                InsurancePolicySubscriberAddressPostalCode = InsurancePolicy?.Address.PostalCode,
                InsurancePolicySubscriberIdNumber = InsurancePolicy?.InsuredIdNo,
                ExcludeRenderingProviderInd = excludeProviderInd,
                ClientFirstName = InsurancePolicy?.ClientFirst,
                ClientMiddleName = InsurancePolicy?.ClientMiddle,
                ClientLastName = InsurancePolicy?.ClientLast,
                ClientAddressLine1 = InsurancePolicy?.Address?.Line1,
                ClientCity = InsurancePolicy?.Address?.City,
                ClientState = InsurancePolicy?.Address?.State,
                ClientPostalCode = InsurancePolicy?.Address?.PostalCode,
                ClientDOB = InsurancePolicy?.ClientDOB ?? InsurancePolicy?.InsuredDoB,
                ClientGender = InsurancePolicy != null && InsurancePolicy.GenderId.HasValue ? (GenderTypeEnum)InsurancePolicy?.GenderId.Value : GenderTypeEnum.Other,
                ClientDiagnosisCodes = DiagnosisCodes.Select(x => x.Code).ToArray(),
                ClientGovtProgramId = ClientGovtProgramId,
                ClientAHCCSId = ClientGovtProgramId,
                OrderingPhysicianFirstName = OrderingPhysicianFirstName,
                OrderingPhysicianLastName = OrderingPhysicianLastName,
                OrderingPhysicianNPI = OrderingPhysicianNPI,
                ProviderFirstName = ProviderFirstName,
                ProviderLastName = ProviderLastName,
                ProviderAHCCCSId = ProviderStateMedicaid,
                ProviderNPI = ProviderNPI,
                ProviderAdressLine1 = ProviderAddress.Line1,
                ProviderCity = ProviderAddress.City,
                ProviderState = ProviderAddress.State,
                ProviderPostalCode = ProviderAddress.PostalCode,
                AmountDue = AmountDue,
                LocationTypeId = (int)LocationTypeId,
                ClaimDateStart = ClaimDate,
                IsTeletherapy = appointment.IsTeletherapy,
                CPTCode = appointment.CPTServiceRates != null && appointment.CPTServiceRates.Count > 0 ? appointment.CPTServiceRates?.First()?.CPTCode : null,
                ModifierCodes = appointment.CPTServiceRates != null && appointment.CPTServiceRates.Count > 0 ? new string[]
                {
                    appointment.CPTServiceRates?.First()?.Modifier1?.Trim(),
                    appointment.CPTServiceRates?.First()?.Modifier2?.Trim(),
                    appointment.CPTServiceRates?.First()?.Modifier3?.Trim()
                } : null,
                Units = isAZDDD ? appointment.GovtUnits.Value : appointment.Units.Value,
                PatientAmountPaid = patientPymt.HasValue && patientPymt.Value > 0 ? patientPymt : null,
                COBPayments = Payments != null && Payments.Count > 0 ? Payments.Select(x => new EDIClaimPayment
                {
                    RelationshipCode = x.InsurancePolicyRelationshipId,
                    PolicyNumber = x.InsurancePolicyNumber,
                    InsuranceSubscriberFirstName = x.SubscriberFirstName,
                    InsuranceSubscriberLastName = x.SubscriberLastName,
                    InsuranceSubscriberInsuredIdNumber = x.InsuredIdNo,
                    InsuranceSubscriberAddress = x.SubscriberAddress,
                    InsurancePriorityCode = x.InsurancePolicyPriorityId.Value,
                    InsuranceCompanyIdCode = isAZDDD ? x.MCID : x.InsuranceCompanyId.Value.ToString(),
                    InsuranceCompanyName = insComp.FirstOrDefault(y => y.InsuranceCompanyId == x.InsuranceCompanyId.Value)?.Name,
                    IsDenial = x.IsDenial,
                    DenialReasonCode = x.DenialReasonId,
                    PaymentAmount = x.Amount.GetValueOrDefault((decimal)0.00),
                    PaymentDate = x.PayDate.GetValueOrDefault(),
                    AllowedAmount = x.AllowedAmount
                }).ToList() : null
            };

            return ediClaim;
        }
    }
    public abstract class BaseDTO
    {
        public long ID { get; set; }
    }
    public class ClaimCommentDTO : BaseDTO
    {
        public long ClaimId { get; set; }
        public string Comments { get; set; }
        public DateTime MadeAt { get; set; }
        public int StaffId { get; set; }
        public int StaffUserId { get; set; }
    }
    public class ClientDiagnosisCodeDTO : BaseDTO
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public int InsurancePolicyId { get; set; }
        public int? ClientServiceId { get; set; }
    }
    public class ErrorLogDTO
    {
        public int CompanyId { get; set; }
        public DateTime? TimeStamp { get; set; }
        public string Exception { get; set; }
        public string Message { get; set; }
        public string ClassInScope { get; set; }
        public string MethodInScope { get; set; }
        public Exception ExceptionFull { get; set; }
    }
    public class AppointmentDTO : BaseDTO
    {

        public string DisciplineCode { get; internal set; }
        public List<CPTRateDTO> CPTRates { get; internal set; }
        public List<ClientServiceCPTDTO> CPTServiceRates { get; internal set; }
        public decimal? Amount { get; internal set; }
        public int ServiceId { get; set; }
        public int StatusId { get; internal set; }
        public decimal? GovtUnits { get; internal set; }
        public bool IsTeletherapy { get; internal set; }
        public object GovtCategory { get; internal set; }

        public long AppointmentId { get; set; }
        public int ClientServiceID { get; set; }
        public int StaffID { get; set; }
        public int StaffUserID { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }

        public string CancelReason { get; set; }
        public long? ClaimId { get; set; }
        public bool IsApproved { get; set; }

        public decimal? Units { get; set; }

        public string ServiceName { get; set; }



        private decimal UnitsField;
        public int SessionNoteId { get; set; }
        public int? LocationTypeId { get; set; }
        public int ApproverId { get; set; }
        public int ApproverUserId { get; set; }
        public int? ClientId { get; set; }
        // public List<ClientServiceCPTDTO> CPTRates { get; set; }
        public ClaimDTO ClaimDTO { get; set; }
        public SessionNoteDTO Session { get; set; }
        public string ClientFirst { get; set; }
        public string ClientMiddle { get; set; }
        public string ClientLast { get; set; }
        public int BillingTierId { get; set; }
        public int Ratio { get; set; }
        public bool IsQualifiedTherapist { get; set; }
        public bool IsClinic { get; set; }

        public int SvId { get; set; }
        public bool IsNextInsurance { get; set; }

        internal void AdjustCPTsToUnits()
        {
            if (CPTRates == null) return;
            var cpts = CPTRates.FindAll(c => c.Amount.GetValueOrDefault(0) > 0);

            if (cpts.Count == 0) return;
            cpts.Sort((a, b) => b.Amount.GetValueOrDefault(0).CompareTo(a.Amount.GetValueOrDefault(0)));
            decimal unitsSum = cpts.Sum(c => c.Amount.GetValueOrDefault(0));
            //tocheck unitsfields value
            if (unitsSum != UnitsField)
            {
                foreach (var c in CPTRates)
                {
                    //Round to nearest 0.25 after adjusting CPT units to be share of total computed units
                    c.Amount = Math.Round(4 * c.Amount.GetValueOrDefault(0) * UnitsField / unitsSum, 0) / 4;

                }
            }
            if (cpts[0].Amount.Value == 0) cpts[0].Amount = 0.25M;
        }
    }
    public class ClientServiceCPTDTO
    {
        public int ClientServiceId { get; set; }
        public string CPTCode { get; set; }
        public decimal? Amount { get; set; }
        public long ClaimId { get; set; }
        public long AppointmentId { get; set; }
        public string Modifier1 { get; set; }
        public string Modifier2 { get; set; }
        public string Modifier3 { get; set; }
        public string Units { get; set; }
        public int? InsurancePolicyId { get; set; }
        public int ServiceCPTRateId { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ClientId { get; set; }
        public decimal UnitsIncrement { get { return Convert.ToDecimal(this.Units); } }



    }
    public class SessionNoteDTO
    {
        public int? NoteId { get; set; }
        public int? AppointmentId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int? ActionType { get; set; }
        public string Reason { get; set; }
        public int? WorkStatusId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime ApprovedAt { get; set; }
        public int StaffId { get; set; }
        public int? AuditActionId { get; set; }
        public int? SupervisorId { get; set; }
        public DateTime? EndAt { get; set; }

        public AppointmentDTO Appointment { get; set; }
    }
    public class PolicyDTO
    {
        public int InsurancePolicyId { get; internal set; }
        public string PolicyNumber { get; internal set; }
        public string LastName { get; internal set; }
        public string FirstName { get; internal set; }
        public string InsuredIdNo { get; internal set; }
        public AddressDTO Address { get; internal set; }
        public int InsuranceCompanyId { get; internal set; }
        public int InsuranceRelationshipId { get; internal set; }
    }
    public class ClaimPaymentDTO
    {
        public int? PaymentTypeId { get; internal set; }
        public int? InsurancePolicyId { get; internal set; }
        public string InsurancePolicyNumber { get; internal set; }
        public DateTime? VoidedAt { get; internal set; }
        public decimal? Amount { get; internal set; }
        public bool IsDenial { get; internal set; }
        public string DenialReasonId { get; internal set; }
        public DateTime? PayDate { get; internal set; }
        public string MCID { get; internal set; }

        public long ClaimId { get; set; }
        public int? GovernmentProgramId { get; set; }
        public string InsuredIdNo { get; set; }
        public string SubscriberFirstName { get; set; }
        public string SubscriberLastName { get; set; }
        public AddressDTO SubscriberAddress { get; set; }
        public int? InsurancePolicyRelationshipId { get; set; }
        public int? InsuranceCompanyId { get; set; }
        public int? InsurancePolicyPriorityId { get; set; }
        public string Payer { get; set; }
        public string Notes { get; set; }
        public string ReasonText { get; set; }
        public decimal? AllowedAmount { get; set; }
        public decimal? CoInsuranceAmount { get; set; }
        public long AppointmentId { get; internal set; }
    }
    public enum PaymentTypeEnum : int
    {
        Insurance = 0,
        Govt = 1,
        Private = 2,
    }
    public enum LocationEnum : int
    {
        ClientHome = 1,
        ProviderHome = 2,
        Clinic = 4,
    }
    public enum AppointmentStatusEnum : int
    {
        Scheduled = 0,
        PendingNotes = 1,
        Completed = 2,
        Cancelled = 3,
        NoShow = 4,
    }
    public enum ClaimStatusEnum : int
    {
        PendInsSubmission = 0,
        PendInsPay = 1,
        PendGovtSubmission = 2,
        PendGovtSubAppv = 8,
        PendGovtPay = 3,
        Paid = 5,
        Closed = 6,
        PendingWaiver = 7,
        PendingInvoicing = 9,
        Invoiced = 4,
        PendingGovtIssue = 10
    }
    public enum InsuranceRelationshipEnum : int
    {
        Self = 0,
        Spouse = 1,
        Child = 2,
        Other = 3,
    }
    public class DiagnosisCodeDTO
    {
        public string Code { get; internal set; }
        public int ClientServiceId { get; internal set; }
        public int InsurancePolicyId { get; internal set; }
    }
    public class CPTBreakOutDTO
    {
        string _mods;
        string[] _modList = null;
        public CPTBreakOutDTO(string cPTCode, decimal v, string mod1, string mod2, string mod3, string spacer = ":")
        {
            this.Code = cPTCode;
            this.Amount = v;

            StringBuilder sb = new StringBuilder();
            List<string> ml = new List<string>();
            if (!string.IsNullOrWhiteSpace(mod1))
            {
                sb.AppendFormat("{1}{0}", mod1.Trim(), spacer);
                ml.Add(mod1.Trim());
            }
            if (!string.IsNullOrWhiteSpace(mod2))
            {
                sb.AppendFormat("{1}{0}", mod2.Trim(), spacer);
                ml.Add(mod2.Trim());
            }
            if (!string.IsNullOrWhiteSpace(mod3))
            {
                sb.AppendFormat("{1}{0}", mod3.Trim(), spacer);
                ml.Add(mod3.Trim());
            }
            if (ml.Count > 0) _modList = ml.ToArray();
            _mods = sb.ToString();
        }

        public string Code { get; internal set; }
        public decimal Units { get; internal set; }
        public decimal Amount { get; internal set; }
        public object ServiceCPTRateId { get; internal set; }
        public string Mods { get { return _mods; } internal set { } }
    }
    public class CPTRateDTO
    {
        public int ClientServiceId { get; set; }
        public decimal? Amount { get; internal set; }
        public string CPTCode { get; internal set; }
        public string Modifier1 { get; internal set; }
        public string Modifier2 { get; internal set; }
        public string Modifier3 { get; internal set; }
        public string Units { get; set; }
        public int InsurancePolicyId { get; set; }
        public int ServiceCPTRateId { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ClientId { get; set; }
        public decimal UnitsIncrement { get { return Convert.ToDecimal(this.Units); } }
    }
    public class ExportSummaryDTO
    {
        public int RecordCount { get; internal set; }
        public decimal UnitCount { get; internal set; }
        public decimal TotalDue { get; internal set; }
    }
    public class CompanyExportInfoDTO
    {
        public string NPI { get; internal set; }
        public string SystemId { get; internal set; }
        public string TaxId { get; internal set; }
    }
    public class AccountData
    {
        public int CompanyID { get; internal set; }
        public string NPI { get; internal set; }
        public string TaxId { get; internal set; }
        public string ProvAhcccsId { get; internal set; }
        public CompanyInfoDTO Company { get; set; }
    }

    public class ReconcileRequest
    {
        public string Source { get; set; }
        public byte[] FileData { get; set; }
    }
    public class ReconcileResponse : ViewModelBase
    {
        public bool IsFailure { get; set; }
        public int RecordCount { get; set; }
        public List<string> ErrorMessages { get; set; }
        public List<GenericEntity> EntityList { get; set; }
        public ReconcileResponse()
        {
            this.EntityList = new List<GenericEntity>();
            this.ErrorMessages = new List<string>();
        }
    }
    public class PaymentInfo
    {
        public long PaymentId { get; set; }
        public string Description { get; set; }
        public System.DateTime MadeOn { get; set; }
        public PaymentTypeEnum Type { get; set; }
        public decimal Amount { get; set; }
        public GenericEntity ProcessedBy { get; set; }
        public string Notes { get; set; }
        public string AccountSysId { get; set; }
        public List<ClaimPaymentRC> Claims { get; set; }
        public string TransactionId { get; set; }
    }
    public class ClaimInfo
    {
        public long ClaimId { get; set; }
        public ClaimStatusEnum Status { get; set; }
        public LocationEnum Location { get; set; }
        public DateTime UpdatedAt { get; set; }
        public System.DateTime ClaimDate { get; set; }
        public GenericEntity PendingWith { get; set; }
        public List<ClaimPaymentRC> Payments { get; set; }
        public GenericEntity Approver { get; set; }
        public System.Nullable<bool> UseProvider { get; set; }
        public List<Comment> Comments { get; set; }
        public List<ClaimAppointmentInfo> Appointments { get; set; }
        public string SubStatus { get; set; }
        public GenericEntity Provider { get; set; }
        public GenericEntity Client { get; set; }
        public string ActivityId { get; set; }
        public string TransactionId { get; set; }
        public System.Nullable<bool> IsTelehealthClient { get; set; }
        public System.Nullable<System.DateTime> TelehealthUpdatedAt { get; set; }
        public decimal AmountPaid
        {
            get
            {

                if (this.Payments == null) return 0;
                var aps = this.Payments.FindAll(p => !p.VoidedAt.HasValue && !p.Denial);
                if (aps.Count == 0) return 0;
                return aps.Sum(p => p.Amount.GetValueOrDefault(0));
            }
        }

        public decimal AmountDue
        {
            get
            {

                if (this.Appointments == null) return 0;
                return this.Appointments.Sum(p => p.Amount);
            }
        }

        public bool? IsNonTelehealth { get; set; }
    }

    public class ClaimPaymentRC
    {
        public long PaymentId { get; set; }
        public bool Denial { get; set; }
        public GenericEntity Source { get; set; }
        public GenericEntity Claim { get; set; }
        public GenericEntity Client { get; set; }
        public System.Nullable<System.DateTime> ClaimDate { get; set; }
        public System.Nullable<System.DateTime> PayDate { get; set; }
        public System.Nullable<decimal> Amount { get; set; }
        public List<CPTRate> CPTs { get; set; }
        public System.Nullable<decimal> AmountDue { get; set; }
        public string Comment { get; set; }
        public System.Nullable<System.DateTime> VoidedAt { get; set; }
        public GenericEntity DenialReason { get; set; }
    }

    public class ClaimAppointmentInfo
    {
        public long AppointmentId { get; set; }
        public GenericEntity Service { get; set; }
        public AppointmentStatusEnum Status { get; set; }
        public System.DateTime Start { get; set; }
        public decimal Amount { get; set; }
        public decimal Units { get; set; }
        public string GovtCategory { get; set; }
        public List<CPTRate> CPTs { get; set; }
        public decimal? GovtUnits { get; set; }
        public int? ServiceDisciplineId { get; set; }
        public string DisciplineCode { get; set; }
        public bool? IsTelehealthClient { get; set; }
        public DateTime? TelehealthUpdatedAtClient { get; set; }
    }

    public class CPTRate
    {
        public GenericEntity CPT { get; set; }
        public System.Nullable<decimal> Amount { get; set; }
        public string Mod1 { get; set; }
        public string Mod2 { get; set; }
        public string Mod3 { get; set; }
        public int? ServiceCPTRateId { get; set; }
    }
    public class Comment
    {
        public DateTime CommentDate { get; set; }
        public string CommentId { get; set; }
        public int ClaimCommentId { get; set; }
        public DateTime MadeAt { get; set; }
        public string CommentText { get; set; }
        public GenericEntity Commentor { get; set; }
    }
    public class PersonField
    {
        public string UniqueId { get; set; }
        public string Context { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Suffix { get; set; }
    }
    public class AccountsReceivable
    {
        public List<ARChart> ArChart { get; set; }
        public List<ARClaims> ClaimList { get; set; }
    }
    public class ARChart 
    {
        public string AgingBucket { get; set; }
        public int ClaimCount { get; set; }
        [DataType(DataType.Currency)]
        public decimal ClaimAmount { get; set; }
        public decimal Percentage { get; set; }
        public int ClaimAgeRange { get; set; }
    }
    public class ARClaims
    {
        public int ClaimId { get; set; }
        public DateTime ClaimDate { get; set; }
        public int ClaimAge { get; set; }
        [DataType(DataType.Currency)]
        public decimal AmountDue { get; set; }
        public ClaimsStatus ClaimStatus { get; set; }
        public string InsuranceCompanyName { get; set; }
        public string ClientName { get; set; }
        public int InsurancePriorityId { get; set; }
        public string ServiceName { get; set; }
        public int ClientId { get; set; }
        public int InsuranceCompanyId { get; set; }
    }
    public class GenericEntity
    {
        public string UniqueId { get; set; }
        public string Name { get; set; }
        public string Context { get; set; }
        public string AlternateId { get; set; }
    }

    public class ClaimsCount
    {
        public int Count { get; set; }
        public int ClaimStatusId { get; set; }
        public int ClientId { get; set; }
        public int InsurancePolicyId { get; set; }
        public int InsuranceCompanyId { get; set; }
    }
}