using DCC.Helpers;
using DCC.Models.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace DCC.Models
{
    public class EDI837P
    {
        #region Public Properties
        
        public bool isAZDDD { get; set; }
        public int currentUserId { get; set; }
        public string billingCompanyName { get; set; }
        public string billingCompanyTaxId { get; set; }
        public string billingCompanyPhone { get; set; }
        public string billingCompanyEmail { get; set; }
        public string billingCompanyAHCCSId { get; set; }
        public string billingCompanyNPI { get; set; }
        public AddressDTO billingCompanyAddress { get; set; }

        public List<EDIClaim> claims { get; set; }
        public Dictionary<int, string> errors { get; internal set; }

        #endregion

        #region Private Properties & Methods

        // AZ DDD Constants
        private readonly string DDDInterchangeIDQualifier = "ZZ";
        private readonly string DDDInterchangeReceiverId = "HAR_837_Upload";
        private readonly string DDDReceiverCompanyName = "HAR_837_Upload";
        private readonly string DDDSubscriberClaimFilingIndCode = "ZZ";
        private readonly string DDDPayerOrganizationName = "AHCCCS";
        private readonly string DDDPayerIdentificationCode = "866004791";
        
        // OA Contants
        private readonly string OAInterchangeIDQualifier = "30";
        private readonly string OAInterchangeReceiverId = "330897513";
        private readonly string OAReceiverCompanyName = "OFFICE ALLY";
        private readonly string OASubscriberClaimFilingIndCode = "CI";

        private string billingCompanyIdentificationCode { 
            get { return isAZDDD ? billingCompanyAHCCSId?.Trim() : billingCompanyTaxId?.Replace("-", "").Trim(); }
        }

        private string interchangeIDQualifier
        {
            get { return isAZDDD ? DDDInterchangeIDQualifier : OAInterchangeIDQualifier; }
        }

        private string interchangeReceiverId
        {
            get { return isAZDDD ? DDDInterchangeReceiverId : OAInterchangeReceiverId; }
        }

        private string receiverCompanyName
        {
            get { return isAZDDD ? DDDReceiverCompanyName : OAReceiverCompanyName; }
        }

        private string interchangeControlNumber
        {
            get { return string.IsNullOrEmpty(currentUserId.ToString()) ? string.Format("SYS{0}", DateTime.UtcNow.ToString("yyMMdd")) : currentUserId.ToString(); }
        }

        private string subscriberHierarchialChildCode
        {
            get { return isAZDDD ? "0" : "1"; }
        }

        private string subscriberClaimFilingIndCode
        {
            get { return isAZDDD ? DDDSubscriberClaimFilingIndCode : OASubscriberClaimFilingIndCode; }
        }

        private string GetBillingCompanyAddress()
        {
            return ValueToUpperTrimmed(billingCompanyAddress.Line1);
        }

        private string GetBillingCompanyCity()
        {
            return ValueToUpperTrimmed(billingCompanyAddress.City, 30);
        }

        private string GetBillingCompanyState()
        {
            return ValueToUpperTrimmed(billingCompanyAddress.State);
        }

        private string GetBillingCompanyPostalCode()
        {
            return ValueToUpperTrimmed(billingCompanyAddress.PostalCode, 5);
        }

        private string GetBillingCompanyEmail()
        {
            return ValueToUpperTrimmed(billingCompanyEmail);
        }

        private string GetBillingCompanyNPI()
        {
            return ValueToUpperTrimmed(billingCompanyNPI.Replace("-", string.Empty));
        }

        private string GetBillingCompanyTaxId()
        {
            return ValueToUpperTrimmed(billingCompanyTaxId.Replace("-", string.Empty));
        }

        private string GetInsuranceCompanyPayerId(string id)
        {
            return isAZDDD ? DDDPayerIdentificationCode : id;
        }

        private string GetInsuranceCompanyName(string name)
        {
            return isAZDDD ? DDDPayerOrganizationName : name;
        }

        private string FormatDate(DateTime dte)
        {
            return dte.ToString("yyyyMMdd");
        }
        private string FormatDate(DateTime? dte)
        {
            return dte.HasValue ? dte.Value.ToString("yyyyMMdd") : "";
        }
        private string FormatDateRange(DateTime startDte, DateTime? endDte)
        {
            if(!endDte.HasValue || startDte == endDte.Value) return startDte.ToString("yyyyMMdd");
            else return string.Format("{0}-{1}", startDte.ToString("yyyyMMdd"), endDte.Value.ToString("yyyyMMdd"));
        }

        private string FormatDateShort(DateTime dte)
        {
            return dte.ToString("yyMMdd");
        }

        public string FormatTime(DateTime dte)
        {
            return dte.ToString("HHmm");
        }

        public string FormatPhoneNbr(string nbr)
        {
            return Regex.Replace(nbr ?? "", "[^0-9]", "");
        }

        public string FormatAmount(decimal? amt)
        {
            if (amt.HasValue)
            {
                return amt.Value.ToString("F2");
            }
            else return "";
        }

        public string FormatUnits(decimal units)
        {
            return units.ToString("0.00");
        }
        private string ValueToUpperTrimmed(string baseValue, int? maxLength = null)
        {
            if (!string.IsNullOrEmpty(baseValue))
            {
                if (maxLength.HasValue && maxLength.Value <= baseValue.Length)
                {
                    baseValue = baseValue.Substring(0, maxLength.Value);
                }

                return baseValue.Trim().ToUpper();
            }
            else return "";
        }

        public int GetPlaceOfServiceCode(int location, bool? isTeletherapy = false)
        {
            if (isTeletherapy.HasValue && isTeletherapy.Value) return (int)PlaceOfService.Teletherapy;

            switch(location)
            {
                case (int)LocationEnum.ClientHome:
                case (int)LocationEnum.ProviderHome:
                    return (int)PlaceOfService.Home;
                case (int)LocationEnum.Clinic:
                    return (int)PlaceOfService.Clinic;
                default:
                    return (int)PlaceOfService.Other;
            }
        }

        public string GetDiagnosisCodes(string[] codes)
        {
            var retVal = new StringBuilder();

            for(int i = 0; i < codes.Length; i++)
            {
                if (isAZDDD) codes[i] = codes[i].Replace(".", string.Empty);

                if (i == 0) retVal.Append(string.Format("ABF:{0}", codes[i].Trim().ToUpper()));
                else retVal.Append(string.Format("*ABK:{0}", codes[i].Trim().ToUpper()));
            }

            return retVal.ToString();
        }

        public string GetRenderingProviderNPI(string npi)
        {
            if (string.IsNullOrEmpty(npi)) return "";
            else
            {
                return string.Format("****XX*{0}", npi);
            }
        }
        #endregion

        #region Public Methods
        public string GenerateEDI837P()
        {
            var nw = DateTime.UtcNow;
            string groupTracking = nw.ToString("yyMMddmm");
            StringBuilder finalResult = new StringBuilder();
            errors = new Dictionary<int, string>();

            try
            {
                int segmentCount = 1;
                int svcLine = 1;

                //Generate standard submitter lines so that we do not have to repeat
                StringBuilder submitterLines = new StringBuilder();
                if (billingCompanyName.Length > 60) billingCompanyName = billingCompanyName.Substring(0, 60);
                billingCompanyName = billingCompanyName.ToUpper();

                int submitterLineCount = 0;
                //submitter line
                submitterLines.AppendLine(string.Format("NM1*41*2*{0}*****46*{1}", billingCompanyName, billingCompanyIdentificationCode));
                submitterLineCount++;

                //submitter contact line
                submitterLines.AppendLine(string.Format("PER*IC**EM*{0}***TE*{1}", GetBillingCompanyEmail(), FormatPhoneNbr(billingCompanyPhone)));
                submitterLineCount++;
                
                //receiver line
                submitterLines.AppendLine(string.Format("NM1*40*2*{0}*****46*{1}", receiverCompanyName, interchangeReceiverId));
                submitterLineCount++;

                //Interchange control and functional group headers
                finalResult.AppendLine(string.Format("ISA*00*          *00*          *{0}*{1}*{2}*{3}*{4}*{5}*^*00501*{6}*0*P*:", interchangeIDQualifier, 
                    billingCompanyIdentificationCode.PadRight(15), interchangeIDQualifier, interchangeReceiverId.PadRight(15), this.FormatDateShort(nw), this.FormatTime(nw), interchangeControlNumber.PadLeft(9, '0')));
                finalResult.AppendLine(string.Format("GS*HC*{0}*{1}*{2}*{3}*{4}*X*005010X222A1", billingCompanyIdentificationCode.PadRight(15), interchangeReceiverId, this.FormatDateShort(nw), this.FormatTime(nw), groupTracking));

                foreach(var claim in claims)
                {
                    StringBuilder result = new StringBuilder();
                    int lineCount = 0;

                    #region Validations
                    if(billingCompanyAddress == null || string.IsNullOrEmpty(billingCompanyAddress.Line1) || string.IsNullOrEmpty(billingCompanyAddress.City) 
                        || string.IsNullOrEmpty(billingCompanyAddress.State) || string.IsNullOrEmpty(billingCompanyAddress.PostalCode))
                    {
                        errors.Add(claim.ClaimId, "Invalid billing company address, city, state, or postal code");
                        continue;
                    } else
                    {
                        if (billingCompanyAddress.City.Length > 30) billingCompanyAddress.City = billingCompanyAddress.City.Substring(0, 30);
                    }

                    if (!string.IsNullOrEmpty(claim.ClientGovtProgramId) && claim.ClientGovtProgramId.Length != 10)
                    {
                        errors.Add(claim.ClaimId, string.Format("Client {0} has an invalid Client DDD ID. Cannot generate insurance submissions for this client.", claim.ClientFullName));
                        continue;
                    }

                    if (!string.IsNullOrEmpty(claim.OrderingPhysicianNPI) && claim.OrderingPhysicianNPI.Length != 10)
                    {
                        errors.Add(claim.ClaimId, string.Format("Client {0} has an invalid Ordering Physician NPI. Cannot generate insurance submissions for this client.", claim.ClientFullName));
                        continue;
                    }

                    if (!string.IsNullOrEmpty(claim.ProviderNPI) && claim.ProviderNPI.Length != 10)
                    {
                        errors.Add(claim.ClaimId, string.Format("Staff member {0} has an invalid NPI. Cannot generate insurance submissions for this client.", claim.ProviderFullName));
                        continue;
                    }

                    if (!string.IsNullOrEmpty(claim.ProviderAHCCCSId) && claim.ProviderAHCCCSId.Length != 6)
                    {
                        errors.Add(claim.ClaimId, string.Format("Staff member {0} has an invalid Medicaid ID. Cannot generate insurance submissions for this client.", claim.ProviderFullName));
                        continue;
                    }

                    if(claim.ClientDiagnosisCodes == null || claim.ClientDiagnosisCodes.Length <= 0)
                    {
                        errors.Add(claim.ClaimId, string.Format("Client {0} is missing diagnosis code(s). Cannot generate insurance submissions for this client.", claim.ClientFullName));
                        continue;
                    }
                    if(string.IsNullOrEmpty(claim.CPTCode))
                    {
                        errors.Add(claim.ClaimId, string.Format("Client {0} is missing CPT code(s). Cannot generate insurance submissions for this client.", claim.ClientFullName));
                        continue;
                    }
                    #endregion

                    //Start transaction set
                    string transCtrlNbr = claim.ClaimId < 1000 ? claim.ClaimId.ToString().PadLeft(4, '0') : claim.ClaimId.ToString();
                    result.AppendLine(string.Format("ST*837*{0}*005010X222A1", transCtrlNbr));
                    lineCount++;

                    //Transaction header
                    string refID = string.Format("CF-{0}-{1}-{2}", claim.InsurancePolicyId, claim.ClaimId, this.FormatDateShort(nw));
                    result.AppendLine(string.Format("BHT*0019*00*{0}*{1}*{2}*CH", refID, this.FormatDate(claim.StatusUpdatedAt), this.FormatDate(claim.StatusUpdatedAt)));
                    lineCount++;

                    //Add in submitter/receiver lines
                    result.Append(submitterLines.ToString());
                    lineCount += submitterLineCount;

                    //billing provider start
                    result.AppendLine(string.Format("HL*{0}**20*1", segmentCount));
                    segmentCount++;
                    lineCount++;

                    //billing provider name 
                    result.AppendLine(string.Format("NM1*85*2*{0}*****XX*{1}", billingCompanyName, GetBillingCompanyNPI()));
                    lineCount++;

                    //billing provider address 
                    result.AppendLine(string.Format("N3*{0}", GetBillingCompanyAddress()));
                    lineCount++;

                    //billing provider city,state,zip 
                    result.AppendLine(string.Format("N4*P{0}*{1}*{2}", GetBillingCompanyCity(), GetBillingCompanyState(), GetBillingCompanyPostalCode()));
                    lineCount++;

                    //billing provider tax id
                    result.AppendLine(string.Format("REF*EI*{0}", GetBillingCompanyTaxId()));
                    lineCount++;

                    //Subscriber start line
                    result.AppendLine(string.Format("HL*{0}*{1}*22*{2}", segmentCount, segmentCount - 1, subscriberHierarchialChildCode));
                    segmentCount++;
                    lineCount++;

                    //Subscriber info line
                    result.AppendLine(string.Format("SBR*{0}*{1}*{2}******{3}", GetInsurancePolicySeqCode(claim.InsurancePolicySequence), 
                        GetInsurancePolicyRelationshipeCode(claim.InsurancePolicyRelationshipId), GetInsuranceTypeCode(claim.InsurancePolicySequence), subscriberClaimFilingIndCode));
                    lineCount++;

                    //subscriber name and id
                    result.AppendLine(string.Format("NM1*IL*1*{0}*{1}****MI*{2}", claim.InsurancePolicySubscriberLastName, claim.InsurancePolicySubscriberFirstName, claim.InsurancePolicySubscriberIdNumber));
                    lineCount++;

                    //subscriber  address 
                    result.AppendLine(string.Format("N3*{0}", claim.InsurancePolicySubscriberAddressLine1));
                    lineCount++;

                    //subscriber city,state,zip 
                    result.AppendLine(string.Format("N4*{0}*{1}*{2}", claim.InsurancePolicySubscriberAddressCity, claim.InsurancePolicySubscriberAddressState, claim.InsurancePolicySubscriberAddressPostalCode));
                    lineCount++;

                    if(isAZDDD)
                    {
                        //client  demographics 
                        result.AppendLine(string.Format("DMG*D8*{0}*{1}", FormatDate(claim.ClientDOB), claim.GetGender(claim.ClientGender)));
                        lineCount++;
                    }

                    //Payer
                    result.AppendLine(string.Format("NM1*PR*2*{0}*****PI*{1}", GetInsuranceCompanyName(claim.InsuranceCompanyName), GetInsuranceCompanyPayerId(claim.InsuranceCompanyPayerId)));
                    lineCount++;

                    //Patient Hierarchy Level -- non DDD only
                    if(!isAZDDD)
                    {
                        //Patient start line
                        result.AppendLine(string.Format("HL*{0}*{1}*23*0", segmentCount, segmentCount - 1));
                        segmentCount++;
                        lineCount++;

                        //Patient name
                        result.AppendLine(string.Format("NM1*QC*1*{0}*{1}{2}", claim.ClientLastName, claim.ClientFirstName, claim.ClientMiddleName));
                        lineCount++;

                        //patient  address 
                        result.AppendLine(string.Format("N3*{0}", claim.ClientAddressLine1));
                        lineCount++;

                        result.AppendLine(string.Format("N4*{0}*{1}*{2}", claim.ClientCity, claim.ClientState, claim.ClientPostalCode));
                        lineCount++;

                        //client  demographics 
                        result.AppendLine(string.Format("DMG*D8*{0}*{1}", this.FormatDate(claim.ClientDOB), claim.GetGender(claim.ClientGender)));
                        lineCount++;
                    }

                    //Claim header
                    var placeOfServiceCode = GetPlaceOfServiceCode(claim.LocationTypeId, claim.IsTeletherapy);
                    result.AppendLine(string.Format("CLM*{0}*{1}***{2}:B:1*Y*A*Y*Y", claim.ClaimId, FormatAmount(claim.AmountDue), placeOfServiceCode));
                    lineCount++;

                    if (!isAZDDD && claim.PatientAmountPaid.HasValue)
                    {
                        //Patient Payment Amount
                        result.AppendLine(string.Format("AMT*F5*{0}", FormatAmount(claim.PatientAmountPaid)));
                        lineCount++;
                    }

                    if(isAZDDD && !string.IsNullOrEmpty(claim.ClientAHCCSId))
                    {
                        //Client AHCCCS ID
                        result.AppendLine(string.Format("REF*EA*{0}", claim.ClientAHCCSId));
                        lineCount++;
                    }

                    //Diagnosis Code
                    result.AppendLine(string.Format("HI*{0}", GetDiagnosisCodes(claim.ClientDiagnosisCodes)));
                    lineCount++;

                    if(isAZDDD)
                    {
                        //Referring Provider (Physician)
                        result.AppendLine(string.Format("NM1*DN*1*{0}*{1}***XX*{2}", claim.OrderingPhysicianLastName, claim.OrderingPhysicianFirstName, claim.OrderingPhysicianNPI));
                        lineCount++;
                    }

                    //Rendering Provider (Company Name/NPI)
                    result.AppendLine(string.Format("NM1*82*1*{0}**{1}", billingCompanyName, billingCompanyNPI));
                    lineCount++;                    

                    if (isAZDDD)
                    {
                        //Rendering Provider (Company) AHCCCS ID
                        result.AppendLine(string.Format("REF*G2*{0}", billingCompanyAHCCSId));
                        lineCount++;
                    }

                    switch(placeOfServiceCode)
                    {
                        case (int)PlaceOfService.Home:
                            if(claim.LocationTypeId == (int)LocationEnum.ClientHome)
                            {
                                //Client Info
                                result.AppendLine(string.Format("NM1*77*2*{0}", claim.ClientFullName));
                                lineCount++;

                                //Client Address
                                result.AppendLine(string.Format("N3*{0}", claim.ClientAddressLine1));
                                lineCount++;

                                //Client city,state,zip 
                                result.AppendLine(string.Format("N4*{0}*{1}*{2}", claim.ClientCity, claim.ClientState, claim.ClientPostalCode));
                                lineCount++;
                            } else  //Provider Home
                            {
                                //Provider Info
                                result.AppendLine(string.Format("NM1*77*2*{0}", claim.ProviderFullName));
                                lineCount++;

                                //Provider Address
                                result.AppendLine(string.Format("N3*{0}", claim.ProviderAdressLine1));
                                lineCount++;

                                //Provider city,state,zip 
                                result.AppendLine(string.Format("N4*{0}*{1}*{2}", claim.ProviderCity, claim.ProviderState, claim.ProviderPostalCode));
                                lineCount++;
                            }
                            break;
                        default:
                            //Clinic Info
                            result.AppendLine(string.Format("NM1*77*2*{0}", billingCompanyName));
                            lineCount++;

                            //Clinic Address
                            result.AppendLine(string.Format("N3*{0}", GetBillingCompanyAddress()));
                            lineCount++;

                            //Clinic city,state,zip 
                            result.AppendLine(string.Format("N4*{0}*{1}*{2}", GetBillingCompanyCity(), GetBillingCompanyState(), GetBillingCompanyPostalCode()));
                            lineCount++;

                            break;
                    }

                    //Claim COB
                    if(!isAZDDD && claim.COBPayments != null && claim.COBPayments.Count > 0)
                    {
                        var amountDue = claim.AmountDue;
                        foreach(var pymt in claim.COBPayments.OrderBy(x => x.GetInsurancePriorityAbbreviation()))
                        {
                            result.AppendLine(string.Format("SBR*{0}*{1}*{2}******C1", pymt.GetInsurancePriorityAbbreviation(), GetInsurancePolicyRelationshipeCode(pymt.RelationshipCode), pymt.PolicyNumber));
                            lineCount++;
                            if (pymt.IsDenial)
                            {
                                result.AppendLine(string.Format("CAS*OA*{0}*0", pymt.DenialReasonCode));
                                lineCount++;
                            }
                            
                            amountDue -= pymt.PaymentAmount;
                            result.AppendLine(string.Format("AMT*D*{0}", FormatAmount(pymt.PaymentAmount)));
                            lineCount++;

                            if (pymt.AllowedAmount.HasValue)
                            {
                                result.AppendLine(string.Format("AMT*B6*{0}", FormatAmount(pymt.AllowedAmount)));
                                lineCount++;
                            }

                            result.AppendLine(string.Format("AMT*EAF*{0}", FormatAmount(amountDue)));
                            lineCount++;

                            result.AppendLine("OI***Y*P**Y");
                            lineCount++;

                            //subscriber name
                            result.AppendLine(string.Format("NM1*IL*1*{0}*{1}****MI*{2}", pymt.InsuranceSubscriberLastName, pymt.InsuranceSubscriberFirstName, pymt.InsuranceSubscriberInsuredIdNumber));
                            lineCount++;

                            //subscriber  address 
                            result.AppendLine(string.Format("N3*{0}", pymt.GetInsuranceSubscriberAddressLine1()));
                            lineCount++;

                            //subscriber city,state,zip 
                            result.AppendLine(string.Format("N4*{0}*{1}*{2}", pymt.GetInsuranceSubscriberAddressCity(), pymt.GetInsuranceSubscriberAddressState(), pymt.GetInsuranceSubscriberAddressPostalCode()));
                            lineCount++;

                            //Payer
                            result.AppendLine(string.Format("NM1*PR*2*{0}*****PR*{1}", pymt.InsuranceCompanyName, pymt.InsuranceCompanyIdCode));
                            lineCount++;

                            result.AppendLine(string.Format("DTP*573*{0}", FormatDate(pymt.PaymentDate)));
                            lineCount++;
                        }
                    }

                    //Service line header
                    result.AppendLine(string.Format("LX*{0}", svcLine));
                    svcLine++;
                    lineCount++;

                    //Service Details
                    result.AppendLine(string.Format("SV1*HC:{0}{1}*{2}*UN*{3}*{4}**1", claim.CPTCode, claim.GetModifiers(), FormatAmount(claim.AmountDue), FormatUnits(claim.Units), placeOfServiceCode));
                    lineCount++;

                    //service date
                    result.AppendLine(string.Format("DTP*472*D8*{0}", FormatDateRange(claim.ClaimDateStart, claim.ClaimDateEnd)));
                    lineCount++;

                    if(isAZDDD)
                    {
                        //Line Item Control Nbr
                        result.AppendLine(string.Format("REF*6R*{0}{1}{2}", claim.ClaimId, claim.CPTCode, svcLine));
                        lineCount++;

                        //Ordering Physician
                        result.AppendLine(string.Format("NM1*DK*1*{0}*{1}***XX*{2}", claim.OrderingPhysicianLastName, claim.OrderingPhysicianFirstName, claim.OrderingPhysicianNPI));
                        lineCount++;
                    }

                    //Line Adjudication Info (2430)
                    if (claim.COBPayments != null && claim.COBPayments.Count > 0)
                    {
                        foreach (var pymt in claim.COBPayments.OrderBy(x => x.GetInsurancePriorityAbbreviation()))
                        {
                            //Service Line Adjudication
                            result.AppendLine(string.Format("SVD*{0}*{1}*HC:{2}{3}**{4}", pymt.InsuranceCompanyIdCode, FormatAmount(pymt.PaymentAmount), claim.CPTCode, claim.GetModifiers(), FormatUnits(claim.Units)));
                            lineCount++;

                            //Line Adjustment
                            if((isAZDDD && pymt.PaymentAmount != claim.AmountDue) || pymt.PaymentAmount == 0)
                            {
                                result.AppendLine(string.Format("CAS*PR*1*{0}", FormatAmount(claim.AmountDue)));
                                lineCount++;
                            } else if(pymt.IsDenial)
                            {
                                result.AppendLine(string.Format("CAS*OA*{0}*{1}", pymt.DenialReasonCode, FormatAmount(claim.AmountDue)));
                                lineCount++;
                            } else if(pymt.PaymentAmount != claim.AmountDue)  //paid less than full amt
                            {
                                result.AppendLine(string.Format("CAS*OA*3*{0}", FormatAmount(claim.AmountDue - pymt.PaymentAmount)));
                                lineCount++;
                            }

                            //Payment Date
                            result.AppendLine(string.Format("DTP*573*D8*{0}", FormatDate(pymt.PaymentDate)));
                            lineCount++;
                        }
                    }

                    //End transaction set
                    result.AppendLine(string.Format("SE*{0}*{1}", lineCount, transCtrlNbr));
                    finalResult.Append(result.ToString());
                }

                finalResult.AppendLine(string.Format("GE*10*{0}", groupTracking));
                finalResult.AppendLine(string.Format("IEA*1*{0}", interchangeControlNumber.PadLeft(9, '0')));
            } catch (Exception ex)
            {

            }

            return isAZDDD ? finalResult.ToString().Replace(Environment.NewLine, $"~{Environment.NewLine}") : finalResult.ToString();
        }
        #endregion

        #region Private Methods
        private string GetInsurancePolicyRelationshipeCode(int? insuranceRelationshipId = null)
        {
            if (isAZDDD)
            {
                return "18";
            }
            else if (insuranceRelationshipId.HasValue)
            {
                switch (insuranceRelationshipId.Value)
                {
                    case (int)InsuranceRelationshipEnum.Self:
                        return "18";
                    case (int)InsuranceRelationshipEnum.Spouse:
                        return "01";
                    case (int)InsuranceRelationshipEnum.Child:
                        return "19";
                    default:
                        return "G8";
                }
            } else
            {
                return "G8";
            }
        }

        private string GetInsurancePolicySeqCode(int? insurancePolicySequence)
        {
            if(isAZDDD)
            {
                return "P";
            } else if(insurancePolicySequence.HasValue)
            {
                switch(insurancePolicySequence.Value)
                {
                    case 1:
                        return "P";
                    case 2:
                        return "S";
                    case 3:
                        return "T";
                    default:
                        return "P";
                }
            } else
            {
                return "P";
            }
        }

        private string GetInsuranceTypeCode(int? insurancePolicySequence)
        {
            if (isAZDDD) return "";
            else return GetInsurancePolicySeqCode(insurancePolicySequence);
        }
        #endregion
    }

    public class EDIClaim
    {
        public int ClaimId { get; set; }
        public DateTime StatusUpdatedAt { get; set; }  //some sort of last updated date

        #region Public Properties

        #region Insurance Properties
        public int InsurancePolicyId { get; set; }
        public string InsuranceCompanyName   //leave blank for DDD claims
        {
            get { return ValueToUpperTrimmed(_InsuranceCompanyName, 60); }
            set { _InsuranceCompanyName = value; }
        }
        public string InsuranceCompanyPayerId { get; set; }
        public int? InsurancePolicyRelationshipId { get; set; }
        public int? InsurancePolicySequence { get; set; }
        public string InsurancePolicySubscriberFirstName
        {
            get { return ValueToUpperTrimmed(_InsurancePolicySubscriberFirstName, 35); }
            set { _InsurancePolicySubscriberFirstName = value; }
        }
        public string InsurancePolicySubscriberLastName 
        {
            get { return ValueToUpperTrimmed(_InsurancePolicySubscriberLastName); }
            set { _InsurancePolicySubscriberLastName = value; }
        }
        public string InsurancePolicySubscriberIdNumber { get; set; }      // policy ID or AHCCCS ID
        public string InsurancePolicySubscriberAddressLine1
        {
            get { return ValueToUpperTrimmed(_InsurancePolicySubscriberAddressLine1); }
            set { _InsurancePolicySubscriberAddressLine1 = value; }
        }
        public string InsurancePolicySubscriberAddressCity
        {
            get { return ValueToUpperTrimmed(_InsurancePolicySubscriberAddressCity, 30); }
            set { _InsurancePolicySubscriberAddressLine1 = value; }
        }
        public string InsurancePolicySubscriberAddressState
        {
            get { return ValueToUpperTrimmed(_InsurancePolicySubscriberAddressState); }
            set { _InsurancePolicySubscriberAddressLine1 = value; }
        }
        public string InsurancePolicySubscriberAddressPostalCode //zip code
        {
            get { return ValueToUpperTrimmed(_InsurancePolicySubscriberAddressPostalCode, 5); }
            set { _InsurancePolicySubscriberAddressPostalCode = value; } 
        }
        public bool? ExcludeRenderingProviderInd { get; set; }
        #endregion

        #region Client Information
        public string ClientFirstName 
        {
            get { return ValueToUpperTrimmed(_ClientFirstName, 35); }
            set { _ClientFirstName = value; } 
        }
        public string ClientMiddleName
        {
            get { return ValueToUpperTrimmed(_ClientMiddleName, 25); }
            set { _ClientMiddleName = value; }
        }
        public string ClientLastName 
        {
            get { return ValueToUpperTrimmed(_ClientLastName); }
            set { _ClientLastName = value; }
        }
        public string ClientFullName
        {
            get { return this.ClientFirstName + " " + this.ClientLastName; }
        }
        public string ClientAddressLine1
        {
            get { return ValueToUpperTrimmed(_ClientAddressLine1); }
            set { _ClientAddressLine1 = value; }
        }
        public string ClientCity
        {
            get { return ValueToUpperTrimmed(_ClientCity, 30); }
            set { _ClientCity = value; }
        }
        public string ClientState
        {
            get { return ValueToUpperTrimmed(_ClientState); }
            set { _ClientState = value; }
        }
        public string ClientPostalCode
        {
            get { return ValueToUpperTrimmed(_ClientPostalCode, 5); }
            set { _ClientPostalCode = value; }
        }
        public string[] ClientDiagnosisCodes { get; set; }
        public DateTime? ClientDOB { get; set; }
        public GenderTypeEnum ClientGender { get; set; }
        public string ClientGovtProgramId { get; set; }
        public string ClientAHCCSId { get; set; }
        #endregion

        #region Physician Info
        public string OrderingPhysicianFirstName
        {
            get { return ValueToUpperTrimmed(_PhysicianFirstName); }
            set { _PhysicianFirstName = value; }
        }
        public string OrderingPhysicianLastName
        {
            get { return ValueToUpperTrimmed(_PhysicianLastName); }
            set { _PhysicianLastName = value; }
        }
        public string OrderingPhysicianNPI { get; set; }
        #endregion

        #region Provider Info
        public string ProviderFirstName 
        { 
            get { return ValueToUpperTrimmed(_ProviderFirstName, 35); }
            set { _ProviderFirstName = value; }
        }
        public string ProviderLastName
        {
            get { return ValueToUpperTrimmed(_ProviderLastName); }
            set { _ProviderLastName = value; }
        }
        public string ProviderFullName
        {
            get { return this.ProviderFirstName + " " + this.ProviderLastName; }
        }
        public string ProviderNPI { get; set; }
        public string ProviderAHCCCSId { get; set; }
        public string ProviderAdressLine1
        {
            get { return ValueToUpperTrimmed(_ProviderAddressLine1); }
            set { _ProviderAddressLine1 = value; }
        }
        public string ProviderCity
        {
            get { return ValueToUpperTrimmed(_ProviderCity, 30); }
            set { _ProviderCity = value; }
        }
        public string ProviderState
        {
            get { return ValueToUpperTrimmed(_ProviderState); }
            set { _ProviderState = value; }
        }
        public string ProviderPostalCode
        {
            get { return ValueToUpperTrimmed(_ProviderPostalCode, 5); }
            set { _ProviderPostalCode = value; }
        }
        #endregion

        #region Claim Details
        public decimal AmountDue { get; set; }
        public decimal? PatientAmountPaid { get; set; }
        public int LocationTypeId { get; set; }
        public bool? IsTeletherapy { get; set; }
        public List<EDIClaimPayment> COBPayments { get; set; }
        public string CPTCode { get; set; }
        public string[] ModifierCodes { get; set; }
        public decimal Units { get; set; }
        public DateTime ClaimDateStart { get; set; }
        public DateTime? ClaimDateEnd { get; set; }
        #endregion
        
        #endregion

        #region Private Properties

        #region Insurance Info
        private string _InsuranceCompanyName { get; set; }
        private string _InsurancePolicySubscriberFirstName { get; set; }
        private string _InsurancePolicySubscriberLastName { get; set; }
        private string _InsurancePolicySubscriberAddressLine1 { get; set; }
        private string _InsurancePolicySubscriberAddressCity { get; set; }
        private string _InsurancePolicySubscriberAddressState { get; set; }
        private string _InsurancePolicySubscriberAddressPostalCode { get; set; }
        #endregion

        #region Client Info
        private string _ClientFirstName { get; set; }
        private string _ClientMiddleName { get; set; }
        private string _ClientLastName { get; set; }
        private string _ClientAddressLine1 { get; set; }
        private string _ClientCity { get; set; }
        private string _ClientState { get; set; }
        private string _ClientPostalCode { get; set; }
        #endregion

        #region Physician Info 
        private string _PhysicianFirstName { get; set; }
        private string _PhysicianLastName { get; set; }
        #endregion

        #region Provider Info
        private string _ProviderFirstName { get; set; }
        private string _ProviderLastName { get; set; }
        private string _ProviderAddressLine1 { get; set; }
        private string _ProviderCity { get; set; }
        private string _ProviderState { get; set; }
        private string _ProviderPostalCode { get; set; }
        #endregion

        #endregion

        #region Public Methods
        public string GetGender(GenderTypeEnum gender)
        {
            switch(gender)
            {
                case GenderTypeEnum.Female:
                    return "F";
                case GenderTypeEnum.Male:
                    return "M";
                default:
                    return "U";
            }
        }

        public string GetModifiers()
        {
            StringBuilder sb = new StringBuilder();
            string spacer = ":";

            foreach(var mod in ModifierCodes)
            {
                if(!string.IsNullOrEmpty(mod))
                {
                    sb.AppendFormat("{1}{0}", ValueToUpperTrimmed(mod), spacer);
                }
            }

            return sb.ToString();
        }
        #endregion

        #region Private Methods
        private string ValueToUpperTrimmed(string baseValue, int? maxLength = null)
        {
            if (!string.IsNullOrEmpty(baseValue))
            {
                if(maxLength.HasValue && maxLength.Value <= baseValue.Length)
                {
                    baseValue = baseValue.Substring(0, maxLength.Value);
                }

                return baseValue.Trim().ToUpper();
            }
            else return "";
        }
        #endregion
    }

    public class EDIClaimPayment
    {
        public int? RelationshipCode { get; set; }
        public string PolicyNumber 
        {
            get { return ValueToUpperTrimmed(_PolicyNumber); }
            set { _PolicyNumber = value; } 
        }
        public string InsuranceSubscriberFirstName
        {
            get { return ValueToUpperTrimmed(_InsuranceSubscriberFirstName, 35); }
            set { _InsuranceSubscriberFirstName = value; }
        }
        public string InsuranceSubscriberLastName
        {
            get { return ValueToUpperTrimmed(_InsuranceSubscriberLastName); }
            set { _InsuranceSubscriberLastName = value; }
        }
        public string InsuranceSubscriberInsuredIdNumber
        {
            get { return ValueToUpperTrimmed(_InsuranceSubscriberInsuredIdNumber); }
            set { _InsuranceSubscriberInsuredIdNumber = value; }
        }
        public AddressDTO InsuranceSubscriberAddress { get; set; }
        public int InsurancePriorityCode { get; set; }
        public string InsuranceCompanyName 
        {
            get { return ValueToUpperTrimmed(_InsuranceCompanyName, 60); }
            set { _InsuranceCompanyName = value; }
        }
        public string InsuranceCompanyIdCode
        {
            get { return ValueToUpperTrimmed(_InsuranceCompanyIdCode, 60); }
            set { _InsuranceCompanyIdCode = value; }
        }
        public bool IsDenial { get; set; }
        public string DenialReasonCode { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal? AllowedAmount { get; set; }

        #region Private Properties
        private string _PolicyNumber { get; set; }
        private string _InsuranceSubscriberFirstName { get; set; }
        private string _InsuranceSubscriberLastName { get; set; }
        private string _InsuranceSubscriberInsuredIdNumber { get; set; }
        private string _InsuranceCompanyName { get; set; }
        private string _InsuranceCompanyIdCode { get; set; }
        #endregion

        #region Public Methods
        public string GetInsurancePriorityAbbreviation()
        {
            switch(InsurancePriorityCode)
            {
                case 3: return "T";
                case 2: return "S";
                default: return "P";
            }
        }

        public string GetInsuranceSubscriberAddressLine1()
        {
            return ValueToUpperTrimmed(InsuranceSubscriberAddress.Line1);
        }
        public string GetInsuranceSubscriberAddressCity()
        {
            return ValueToUpperTrimmed(InsuranceSubscriberAddress.City, 30);
        }
        public string GetInsuranceSubscriberAddressState()
        {
            return ValueToUpperTrimmed(InsuranceSubscriberAddress.State);
        }
        public string GetInsuranceSubscriberAddressPostalCode()
        {
            return ValueToUpperTrimmed(InsuranceSubscriberAddress.PostalCode, 5);
        }
        #endregion

        #region Private Methods
        private string ValueToUpperTrimmed(string baseValue, int? maxLength = null)
        {
            if (!string.IsNullOrEmpty(baseValue))
            {
                if (maxLength.HasValue && maxLength.Value <= baseValue.Length)
                {
                    baseValue = baseValue.Substring(0, maxLength.Value);
                }

                return baseValue.Trim().ToUpper();
            }
            else return "";
        }
        #endregion
    }
}