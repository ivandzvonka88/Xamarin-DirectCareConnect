
using DCC.Helpers;
using DCC.Models.Providers;
using Microsoft.Reporting.WebForms;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.Util;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DCC.Models
{
    public class ClaimListExport
    {
        private List<ClaimWrapper> _claims = new List<ClaimWrapper>();
        private readonly int _companyId;

        #region Ctor
        public ClaimListExport(List<ClaimDTO> claims, int companyId, out Dictionary<int, string> errors, bool forGovt = false, bool seperate = false, bool enableTelehealth = false)
        {
            errors = new Dictionary<int, string>();
            this._companyId = companyId;
            foreach (var c in claims)
            {
                if (!forGovt)
                {
                    _claims.Add(new ClaimWrapper(c));
                }
                else
                {
                    c.Appointments.RemoveAll(a => a.GovtUnits.GetValueOrDefault(0) == 0);
                    //if the client has a DDD client ID, check if the length is valid
                    if (!string.IsNullOrEmpty(c.ClientGovtProgramId))
                    {
                        if (c.ClientGovtProgramId.Length != 10)
                        {
                            errors.Add(c.ClaimId, string.Format("Claim {0} has an invalid Client DDD ID. Cannot generate insurance submissions for this client.", c.ClaimId));
                            continue;
                        }
                    }
                    //if client has physician NPI, check if the length is valid
                    if (!string.IsNullOrEmpty(c.OrderingPhysicianNPI))
                    {
                        if (c.OrderingPhysicianNPI.Length != 10)
                        {
                            errors.Add(c.ClaimId, string.Format("Claim {0} has an invalid client Physician NPI. Cannot generate insurance submissions for this client.", c.ClaimId));
                            continue;
                        }
                    }
                    //if staff has providerNPI, check if the length is valid
                    if (!string.IsNullOrEmpty(c.ProviderNPI))
                    {
                        if (c.ProviderNPI.Length != 10)
                        {
                            errors.Add(c.ClaimId, string.Format("Claim {0} has an invalid Provider NPI. Cannot generate insurance submissions for this client.",c.ClaimId));
                            continue;
                        }
                    }
                    //if staff has ProviderStateMedicaid, check if the length is valid
                    if (!string.IsNullOrEmpty(c.ProviderStateMedicaid))
                    {
                        if (c.ProviderStateMedicaid.Length != 6)
                        {
                            errors.Add(c.ClaimId, string.Format("Claim {0} has an invalid Staff Medicaid ID. Cannot generate insurance submissions for this client.", c.ClaimId));
                            continue;
                        }
                    }
                    
                    if (c.Appointments.Count == 0)
                    {
                        errors.Add(c.ClaimId, string.Format("Claim {0} has no GovtUnits. Cannot generate insurance submissions for this claim.", c.ClaimId));
                        continue;
                    }
                    if (seperate)
                    {
                        var seperated = from apt in c.Appointments
                                        group apt by apt.GovtCategory into g
                                        select g;
                        foreach (var g in seperated)
                        {
                            var sc = new ClaimDTO()
                            {
                                Appointments = new List<AppointmentDTO>(),
                                ApproverUserId = c.ApproverUserId,
                                ClaimDate = c.ClaimDate,
                                StatusId = c.StatusId,
                                ClaimId = c.ClaimId,
                                ClientId = c.ClientId,
                                Comments = c.Comments,
                                LocationTypeId = c.LocationTypeId,
                                Payments = c.Payments,
                                ClientGovtProgramId = c.ClientGovtProgramId,
                                ProviderNPI = c.ProviderNPI,
                                ProviderStateMedicaid = c.ProviderStateMedicaid,
                                //PendingWith = c.PendingWith,
                                //Policy = c.Policy,

                                SubStatus = c.SubStatus
                                //UpdatedAt = c.UpdatedAt,
                                //UseProvider = c.UseProvider,
                                //IsTelehealthClient = c.IsTelehealthClient,
                                //TelehealthUpdatedAt = c.TelehealthUpdatedAt
                            };
                            sc.Appointments.AddRange(g);
                            ClaimWrapper cWrapper = new ClaimWrapper(sc);
                            var svc=cWrapper.GovtSvcCode;
                            _claims.Add(cWrapper);

                        }
                    }
                    else
                    {
                        _claims.Add(new ClaimWrapper(c));
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        public StringBuilder HCFA(List<BillingInsuranceCompanyDTO> billingInsuranceCompanies, List<ClientDTO> clients, CompanyInfoDTO company, string userId, Dictionary<int, StaffDTO> users, Dictionary<int, List<CredentialDTO>> credentials, List<string> errors, int clearingHouse, out string groupTracking, List<TeleHealthDTO> telehealthChanges = null, bool enableTelehealth = false)
        {
            var nw = DateTime.UtcNow;
            groupTracking = nw.ToString("yyMMddmm");
            StringBuilder result = new StringBuilder();
            try
            {
                int segmentCount = 1;
                int svcLine = 1;

                //Generate standar submitter lines so do not have to repeat
                StringBuilder submitterLines = new StringBuilder();
                if (company.Name.Length > 60) company.Name = company.Name.Substring(0, 60);
                company.Name = company.Name.ToUpper();

                int submitterLineCount = 0;
                //submitter line
                submitterLines.AppendLine(string.Format("NM1*41*2*{0}*****46*{1}", company.Name, company.TaxId.Replace("-", "").Trim()));
                submitterLineCount++;
                //submitter contact line
                submitterLines.AppendLine(string.Format("PER*IC**TE*{0}", company.Phone));
                submitterLineCount++;
                //receiver line

                submitterLines.AppendLine("NM1*40*2*OFFICE ALLY*****46*330897513");

                submitterLineCount++;




                result.AppendLine(string.Format("ISA*00*          *00*          *30*{0}*30*330897513330897513      *{1}*{2}*^*00501*{3}*0*P*:", company.TaxId.Replace("-", "").PadRight(15), nw.ToString("yyMMdd"), nw.ToString("HHmm"), userId.PadLeft(9, '0')));
                result.AppendLine(string.Format("GS*HC*{0}*OA*{1}*{2}*{3}*X*005010X222A1", company.TaxId.Replace("-", "").PadRight(15), nw.ToString("yyMMdd"), nw.ToString("HHmm"), groupTracking));

                foreach (var claim in _claims)
                {
                    PolicyDTO policy = claim.Claim.Policy;
                    var client = clients.Find(c => c.ClientId == claim.Claim.ClientId);
                    var disciplineCode = claim.Claim.Appointments.Select(x => x.DisciplineCode).FirstOrDefault();
                    string polId = claim.ClaimId < 1000 ? claim.ClaimId.ToString().PadLeft(4, '0') : claim.ClaimId.ToString();
                    int lineCount = 0;
                    //Start transaction set
                    result.AppendLine(string.Format("ST*837*{0}*005010X222A1", polId));
                    lineCount++;
                    //Transaction header
                    result.AppendLine(string.Format("BHT*0019*00*CF-{0}-{1}-{4}*{2}*{3}*CH", policy.InsurancePolicyId, claim.ClaimId, claim.Claim.StatusUpdatedAt.ToString("yyyyMMdd"), claim.Claim.StatusUpdatedAt.ToString("HHmm"), nw.ToString("yyMMdd")));
                    lineCount++;

                    //Add in submitter/receiver lines
                    result.Append(submitterLines.ToString());
                    lineCount += submitterLineCount;

                    //billing provider start
                    result.AppendLine(string.Format("HL*{0}**20*1", segmentCount));
                    segmentCount++;
                    lineCount++;

                    //billing provider name 
                    result.AppendLine(string.Format("NM1*85*2*{0}*****XX*{1}", company.Name, company.NPI.Replace("-", "").Trim()));
                    lineCount++;

                    //billing provider address 
                    result.AppendLine(string.Format("N3*{0}", company.Address.Line1));
                    lineCount++;

                    //billing provider city,state,zip 
                    result.AppendLine(string.Format("N4*{0}*{1}*{2}", company.Address.City.Length > 30 ? company.Address.City.Substring(0, 30).ToUpper() : company.Address.City.ToUpper(), company.Address.State.ToUpper(), company.Address.PostalCode.Substring(0, 5)));
                    lineCount++;

                    //billing provider tax id
                    result.AppendLine(string.Format("REF*EI*{0}", company.TaxId.Replace("-", "")));
                    lineCount++;

                    //Subscriber start line
                    result.AppendLine(string.Format("HL*{0}*{1}*22*1", segmentCount, segmentCount - 1));
                    segmentCount++;
                    lineCount++;

                    //Subscriber info line
                    string relationshipCode = "21";
                    relationshipCode = GetRelationshipCode(policy);
                    int prvPayCount = 0;
                    if (claim.Claim.Payments != null && claim.Claim.Payments.Any())
                    {
                        var prvPolicies = (from p in claim.Claim.Payments
                                           where p.PaymentTypeId.GetValueOrDefault((int)PaymentTypeEnum.Private) == (int)PaymentTypeEnum.Insurance
                                           select p.InsurancePolicyId.GetValueOrDefault(0)).Distinct();
                        prvPayCount = prvPolicies.Count();
                    }
                    var policyOrder = PolicyOrderCode(prvPayCount + 1);
                    result.AppendLine(string.Format("SBR*{2}*{0}*{1}******CI", relationshipCode, policy.PolicyNumber ?? "", policyOrder));
                    lineCount++;

                    //subscriber name
                    result.AppendLine(string.Format("NM1*IL*1*{0}*{1}****MI*{2}", policy.LastName.ToUpper(), policy.FirstName.Length > 35 ? policy.FirstName.Substring(0, 35).ToUpper() : policy.FirstName.ToUpper(), policy.InsuredIdNo));
                    lineCount++;

                    //subscriber  address 
                    result.AppendLine(string.Format("N3*{0}", policy.Address.Line1.ToUpper()));
                    lineCount++;

                    //subscriber city,state,zip 
                    result.AppendLine(string.Format("N4*{0}*{1}*{2}", policy?.Address?.City?.Length > 30 ? policy?.Address?.City?.Substring(0, 30).ToUpper() : policy?.Address?.City?.ToUpper(), policy?.Address?.State?.ToUpper(), policy?.Address?.PostalCode?.Length > 5 ? policy?.Address?.PostalCode?.Substring(0, 5) : policy?.Address?.PostalCode));
                    lineCount++;

                    //Payer
                    var ic = billingInsuranceCompanies.Find(i => i.InsuranceCompanyId == policy.InsuranceCompanyId);
                    if (ic != null)
                    {
                        if (ic.Name?.Length > 60) ic.Name = ic.Name.Substring(0, 60);
                        result.AppendLine(string.Format("NM1*PR*2*{0}*****PR*{1}", ic.Name.ToUpper(), ic.Code));
                        lineCount++;
                    }

                    //Patient start line
                    result.AppendLine(string.Format("HL*{0}*{1}*23*0", segmentCount, segmentCount - 1));
                    segmentCount++;
                    lineCount++;

                    //Patient name
                    result.AppendLine(string.Format("NM1*QC*1*{0}*{1}{2}", client.LastName.ToUpper(), client.FirstName.Length > 35 ? client.FirstName.Substring(0, 35).ToUpper() : client.FirstName.ToUpper(), string.IsNullOrWhiteSpace(client.MiddleName) ? "" : client.MiddleName.Length > 25 ? client.MiddleName.Substring(0, 25).ToUpper() : client.MiddleName.ToUpper()));
                    lineCount++;


                    //patient  address 
                    result.AppendLine(string.Format("N3*{0}", client.Address.Line1.ToUpper()));
                    lineCount++;

                    //client city,state,zip 

                    #region Get Client Address from policy if not present in Client
                    if (string.IsNullOrWhiteSpace(client.Address.City))
                    {
                        client.Address.City = policy.Address.City;
                    }

                    if (string.IsNullOrWhiteSpace(client.Address.PostalCode))
                    {
                        client.Address.PostalCode = policy.Address.PostalCode;
                    }

                    if (string.IsNullOrWhiteSpace(client.Address.State))
                    {
                        client.Address.State = policy.Address.State;
                    }
                    #endregion

                    result.AppendLine(string.Format("N4*{0}*{1}*{2}", client.Address.City.Length > 30 ? client.Address.City.Substring(0, 30).ToUpper() : client.Address.City.ToUpper(), client.Address.State.ToUpper(), client.Address.PostalCode.Length > 5 ? client.Address.PostalCode.Substring(0, 5) : client.Address.PostalCode));
                    lineCount++;

                    //client  demographics 
                    result.AppendLine(string.Format("DMG*D8*{0}*{1}", client.DoB.Value.ToString("yyyyMMdd"), client.GenderId.GetValueOrDefault((int)GenderTypeEnum.Other) == (int)GenderTypeEnum.Male ? "M" : client.GenderId.GetValueOrDefault((int)GenderTypeEnum.Other) == (int)GenderTypeEnum.Female ? "F" : "U"));
                    lineCount++;

                    var diagnosis = (from d in client.DiagnosisCodes
                                     select string.Format("ABF:{0}", d.Code)).ToArray();

                    if (!diagnosis.Any())
                    {
                        errors.Add(string.Format("Client {0} has no diagnosis on their profile. Cannot generate insurance submissions for this client.", client.ClientId));
                        result.Clear();
                        return result;
                    }
                    diagnosis[0] = diagnosis[0].Replace("ABF:", "ABK:");
                    //Add in the claims
                    decimal amountDue = 0;

                    //Break costs/units down by cpt code
                    AccountData account = new AccountData();
                    var cpts = new List<CPTBreakOutDTO>();
                    decimal cptClaimTotal = 0;
                    foreach (var apt in claim.Claim.Appointments)
                    {
                        apt.AdjustCPTsToUnits();
                        var cptTotal = apt.CPTRates.Sum(c => c.Amount.GetValueOrDefault(0));
                        if (cptTotal == 0) continue; //This appointment does not contribute so move to next
                        cptClaimTotal += cptTotal;
                        foreach (var cpt in apt.CPTRates)
                        {
                            if (cpt.Amount.GetValueOrDefault(0) == 0) continue;
                            var bo = cpts.Find(b => b.Code == cpt.CPTCode);
                            decimal amountShare = Math.Ceiling(100 * apt.Amount.GetValueOrDefault(0) * cpt.Amount.GetValueOrDefault(0) / cptTotal) / 100;
                            if (bo == null)
                            {
                                cpts.Add(new CPTBreakOutDTO(cpt.CPTCode, cpt.Amount.GetValueOrDefault(0), cpt.Modifier1, cpt.Modifier2, cpt.Modifier3) { Amount = amountShare });
                            }
                            else
                            {
                                bo.Units += cpt.Amount.GetValueOrDefault(0);
                                bo.Amount += amountShare;
                            }
                        }
                    }


                    if (!cpts.Any())
                    {
                        errors.Add(string.Format("Claim {0} has no CPT's assigned from its current insurance. Cannot generate insurance submissions for this claim.", claim.ClaimId));
                        result.Clear();
                        return result;
                    }

                    //Claim header
                    amountDue = cpts.Sum(c => c.Amount);
                    result.AppendLine(string.Format("CLM*{0}*{1}***{2}:B:1*Y*A*Y*Y", claim.ClaimId, amountDue, claim.Claim.LocationTypeId == (int)LocationEnum.Clinic ? "11" : "12"));
                    lineCount++;
                    //Private payment notes
                    if (claim.Claim.Payments != null && claim.Claim.Payments.Exists(py => !py.VoidedAt.HasValue && py.Amount.GetValueOrDefault(0) > 0 && py.PaymentTypeId == (int)PaymentTypeEnum.Private))
                    {
                        var ppay = claim.Claim.Payments.FindAll(py => !py.VoidedAt.HasValue && py.Amount.GetValueOrDefault(0) > 0 && py.PaymentTypeId == (int)PaymentTypeEnum.Private).Sum(py => py.Amount.GetValueOrDefault(0));
                        result.AppendLine(string.Format("AMT*F5*{0}", ppay.ToString("f2")));
                        lineCount++;
                        amountDue -= ppay;
                    }
                    //Diagnosis
                    result.AppendLine(string.Format("HI*{0}", string.Join("*", diagnosis)));
                    lineCount++;

                    var usr = users[claim.Claim.ApproverUserId];
                    string npi = "";
                    if (!string.IsNullOrWhiteSpace(usr?.NPI))
                    {
                        npi = usr.NPI;
                    }

                    //Provider name
                    if (!ic.ExcludeRenderer.GetValueOrDefault(false))
                    {
                        result.AppendLine(string.Format("NM1*82*1*{0}*{1}{2}", usr.LastName.ToUpper(), usr.FirstName.Length > 35 ? usr.FirstName.Substring(0, 35).ToUpper() : usr.FirstName.ToUpper(), npi == null ? "" : string.Format("****XX*{0}", npi)));
                        lineCount++;
                    }
                    //location
                    switch (claim.Claim.LocationTypeId)
                    {
                        case (int)LocationEnum.ClientHome:
                            result.AppendLine(string.Format("NM1*77*2*{0}", client.LastName.ToUpper(), client.FirstName.Length > 35 ? client.FirstName.Substring(0, 35).ToUpper() : client.FirstName.ToUpper()));
                            lineCount++;
                            result.AppendLine(string.Format("N3*{0}", client.Address.Line1.ToUpper()));
                            lineCount++;

                            //client city,state,zip 
                            result.AppendLine(string.Format("N4*{0}*{1}*{2}", client.Address.City.Length > 30 ? client.Address.City.Substring(0, 30).ToUpper() : client.Address.City.ToUpper(), client.Address.State.ToUpper(), client.Address.PostalCode.Substring(0, 5)));
                            lineCount++;

                            break;
                        case (int)LocationEnum.ProviderHome:
                            usr = users[claim.Claim.StaffUserID.GetValueOrDefault()];
                            if (!string.IsNullOrWhiteSpace(usr?.NPI))
                            {
                                npi = usr.NPI;
                            }
                            result.AppendLine(string.Format("NM1*77*2*{0}*{2}", usr.LastName.ToUpper(), usr.FirstName.Length > 35 ? usr.FirstName.Substring(0, 35).ToUpper() : usr.FirstName.ToUpper(), npi == null ? "" : string.Format("*****XX*{0}", npi)));
                            lineCount++;
                            result.AppendLine(string.Format("N3*{0}", usr.Address.Line1.ToUpper()));
                            lineCount++;

                            //client city,state,zip 
                            result.AppendLine(string.Format("N4*{0}*{1}*{2}", usr.Address.City.Length > 30 ? usr.Address.City.Substring(0, 30).ToUpper() : usr.Address.City.ToUpper(), usr.Address.State.ToUpper(), usr.Address.PostalCode.Substring(0, 5)));
                            lineCount++;
                            break;
                    }


                    #region Claim COB
                    List<ClaimPaymentDTO> ipays = null;
                    if (claim.Claim.Payments != null && claim.Claim.Payments.Count > 0)
                    {
                        ipays = claim.Claim.Payments.FindAll(cp => !cp.VoidedAt.HasValue && cp.PaymentTypeId == (int)PaymentTypeEnum.Insurance);
                        ipays.Sort((a, b) => a.PayDate.GetValueOrDefault().CompareTo(b.PayDate.GetValueOrDefault()));
                        int order = 1;
                        foreach (var py in claim.Claim.Payments)
                        {
                            var payPolicy = client.Policies.Find(ip => ip.InsurancePolicyId == py.InsurancePolicyId.GetValueOrDefault(0));//py.SourceId.GetValueOrDefault(0));
                            if (payPolicy == null || py.VoidedAt.HasValue) continue;
                            string oName = PolicyOrderCode(order);
                            order++;
                            relationshipCode = GetRelationshipCode(payPolicy);
                            result.AppendLine(string.Format("SBR*{2}*{0}*{1}******CI", relationshipCode, payPolicy.PolicyNumber ?? "", oName));
                            lineCount++;
                            if (py.IsDenial)
                            {
                                result.AppendLine(string.Format("CAS*OA*{0}*0", py.DenialReasonId));
                                lineCount++;
                            }
                            amountDue -= py.Amount.GetValueOrDefault(0);
                            result.AppendLine(string.Format("AMT*D*{0}", py.Amount.GetValueOrDefault(0).ToString("0.00")));
                            lineCount++;
                            result.AppendLine(string.Format("AMT*EAF*{0}", amountDue.ToString("0.00")));
                            lineCount++;
                            result.AppendLine("OI***Y*P**Y");
                            lineCount++;

                            //subscriber name
                            result.AppendLine(string.Format("NM1*IL*1*{0}*{1}****MI*{2}", payPolicy.LastName.ToUpper(), payPolicy.FirstName.Length > 35 ? payPolicy.FirstName.Substring(0, 35).ToUpper() : payPolicy.FirstName.ToUpper(), payPolicy.InsuredIdNo));
                            lineCount++;

                            //subscriber  address 
                            result.AppendLine(string.Format("N3*{0}", payPolicy.Address.Line1.ToUpper()));
                            lineCount++;

                            //subscriber city,state,zip 
                            result.AppendLine(string.Format("N4*{0}*{1}*{2}", payPolicy.Address.City.Length > 30 ? payPolicy.Address.City.Substring(0, 30).ToUpper() : payPolicy.Address.City.ToUpper(), payPolicy.Address.State.ToUpper(), payPolicy.Address.PostalCode.Length > 5 ? payPolicy.Address.PostalCode.Substring(0, 5) : payPolicy.Address.PostalCode));
                            lineCount++;

                            //Payer
                            var pic = billingInsuranceCompanies.Find(i => i.InsuranceCompanyId == payPolicy.InsuranceCompanyId);
                            if (pic.Name.Length > 60) pic.Name = pic.Name.Substring(0, 60);
                            result.AppendLine(string.Format("NM1*PR*2*{0}*****PR*{1}", pic.Name.ToUpper(), pic.Code));
                            lineCount++;

                            result.AppendLine(string.Format("DTP*573*{0}", py.PayDate.GetValueOrDefault().ToString("yyyyMMdd")));
                            lineCount++;
                        }
                    }
                    #endregion

                    foreach (var c in cpts)
                    {
                        #region Telehealth

                        //replacement for OT
                        string telehealthCode = "97530";
                        string telehealthMods = ":GO:GT";
                        var replacementRequried = false;

                        //check for Telehealth
                        if (enableTelehealth && client.IsTelehealth.GetValueOrDefault() && !claim.IsNonTelehealth.GetValueOrDefault() && (claim.Claim.ClaimDate >= client.TelehealthUpdatedAt))
                        {
                            //check if it has OT, PT and Speech Therapy Service
                            var isOTA = disciplineCode == "OTA";
                            var isSTA = disciplineCode == "STA";
                            var isPTA = disciplineCode == "PTA";

                            //only replace if it is OTA or STA
                            replacementRequried = isSTA || isOTA;

                            //Replacement for STA
                            if (isSTA)
                            {
                                telehealthCode = "92507";
                                telehealthMods = ":GN:GT";
                            }

                            if (isPTA)
                            {
                                telehealthCode = "97110";
                                telehealthMods = ":GP:GT";
                            }

                            if (replacementRequried)
                            {
                                var parts = telehealthMods.Split(':');
                                telehealthChanges.Add(new TeleHealthDTO
                                {
                                    ServiceCPTRateID = c.ServiceCPTRateId,
                                    CPTCode = telehealthCode,
                                    Mod1 = parts[1],
                                    Mod2 = parts[2],
                                    Mod3 = string.Empty,
                                    ClaimId = claim.ClaimId
                                });
                            }

                        }
                        #endregion




                        //Svc line header
                        result.AppendLine(string.Format("LX*{0}", svcLine));
                        svcLine++;
                        lineCount++;

                        //service details
                        var locCode = claim.Claim.LocationTypeId == (int)LocationEnum.Clinic ? "11" : "12";
                        if (!claim.IsNonTelehealth.GetValueOrDefault() && ic != null && (ic.Name.ToUpper().Contains("UMR") || ic.Name.ToUpper().Contains("UNITED HEALTHCARE")))
                        {
                            locCode = "02";
                        }
                        result.AppendLine(string.Format("SV1*HC:{0}{4}*{1}*UN*{2}*{3}**1", (replacementRequried ? telehealthCode : c.Code), c.Amount.ToString("0.00"), c.Units.ToString("0.00"), locCode, (replacementRequried ? telehealthMods : c.Mods)));
                        lineCount++;


                        //service date
                        result.AppendLine(string.Format("DTP*472*D8*{0}", claim.Claim.ClaimDate.ToString("yyyyMMdd")));
                        lineCount++;

                        //Line Adjududication Info (2430)
                        if (ipays != null && ipays.Count > 0)
                        {
                            foreach (var py in ipays)
                            {
                                var payPolicy = client.Policies.Find(ip => ip.InsurancePolicyId == py.InsurancePolicyId.GetValueOrDefault(0));
                                if (payPolicy == null || py.VoidedAt.HasValue) continue;
                                var pic = billingInsuranceCompanies.Find(i => i.InsuranceCompanyId == payPolicy.InsuranceCompanyId);
                                var payAmount = py.Amount.GetValueOrDefault(0) * c.Units / cptClaimTotal;
                                result.AppendLine(string.Format("SVD*{2}*{1}*HC:{0}**1", c.Code, payAmount.ToString("0.00"), pic.Code));
                                lineCount++;
                                if (py.IsDenial)
                                {
                                    result.AppendLine(string.Format("CAS*OA*{0}*{1}", py.DenialReasonId, c.Amount - payAmount));
                                    lineCount++;

                                }
                                else if (payAmount == 0)
                                {
                                    result.AppendLine(string.Format("CAS*PR*1*{0}", c.Amount));
                                    lineCount++;
                                }
                                else if (payAmount != c.Amount)
                                {
                                    result.AppendLine(string.Format("CAS*OA*3*{0}", c.Amount - payAmount));
                                    lineCount++;
                                }
                                result.AppendLine(string.Format("DTP*573*D8*{0}", py.PayDate.GetValueOrDefault().ToString("yyyyMMdd")));
                                lineCount++;
                            }
                            lineCount++;
                        }
                    }

                    //End transaction set
                    result.AppendLine(string.Format("SE*{0}*{1}", lineCount, polId));

                }
                result.AppendLine(string.Format("GE*10*{0}", groupTracking));
                result.AppendLine(string.Format("IEA*1*{0}", userId.PadLeft(9, '0')));

            }
            catch (Exception ex)
            {
                throw (ex);
                //var error = new ErrorLogDTO();
                //error.CompanyId = this._companyId;
                //error.MethodInScope = MethodBase.GetCurrentMethod().Name;
                //error.ClassInScope = this.GetType().Name;
                //error.ExceptionFull = ex;
                //ErrorLogHelper.StoreException(error);
            }

            return result;
        }

        public LocalReport AZMedicaidClaimSubmission(string npi, string taxId, string systemId)
        {
            //Cap payment entries
            if (_claims.Any())
            {
                _claims.ForEach(c =>
                {
                    c.CapPayments();
                    
                });
            }

            LocalReport localReport = new LocalReport();

            //Specify Report Path.
            localReport.ReportEmbeddedResource = "DCC.CompanyProcessor.Export.AZMedicaidClaimSubmission.rdlc";
            localReport.ReportPath = System.Web.HttpContext.Current.Server.MapPath("~/ModelsLegacy/AZMedicaidClaimSubmission.rdlc");
            //Create Report Data Sources
            ReportDataSource dsClaims = new ReportDataSource();
            dsClaims.Name = "Claims";
            dsClaims.Value = _claims;

            ReportDataSource dsCompany = new ReportDataSource();
            dsCompany.Name = "Company";
            List<CompanyExportInfoDTO> company = new List<CompanyExportInfoDTO>();
            company.Add(new CompanyExportInfoDTO()
            {
                NPI = npi,
                SystemId = systemId,
                TaxId = taxId
            });
            dsCompany.Value = company;

            ReportDataSource dsSummary = new ReportDataSource();
            dsSummary.Name = "Summary";

            List<ExportSummaryDTO> summary = new List<ExportSummaryDTO>();
            summary.Add(new ExportSummaryDTO()
            {
                RecordCount = _claims.Count,
                UnitCount = _claims.Sum(c => c.AbsentUnits + c.DeliveredUnits),
                TotalDue = _claims.Sum(c => c.AmountDue)
            });
            dsSummary.Value = summary;


            //Bind DataSources into Report
            localReport.DataSources.Add(dsClaims);
            localReport.DataSources.Add(dsCompany);
            localReport.DataSources.Add(dsSummary);

            //Set Parameters
            List<ReportParameter> lst = new List<ReportParameter>();

            localReport.SetParameters(lst);
            return localReport;
        }

        public List<byte[]> GetDDDFiles(AccountData account)
        {
            List<byte[]> byteFiles = new List<byte[]>();
            byte[] coverSheetBytes, dddBytes;
            string templateFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");

            string fileName = $"{templateFolder}\\DDDBillingFileNew.xls";
            DateTime dt = DateTime.UtcNow.AddMonths(-1);
            string billingYear = "";
            string billingMonth = "";

            // state fiscal year
            if (dt.Month > 6)
                billingYear = dt.AddYears(1).ToString("yy");
            else
                billingYear = dt.ToString("yy");
            if (dt.Month > 9)
                billingMonth = dt.Month.ToString();
            else
                billingMonth = $"0{dt.Month}";

            string billingMonthAbr = string.Format("{0:MMM}", dt).ToUpper();

            ByteArrayOutputStream bosExcel = new ByteArrayOutputStream();

            try
            {
                HSSFWorkbook billingFile;
                using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    billingFile = new HSSFWorkbook(file);
                }

                // header sheet
                ISheet header = billingFile.GetSheet("HEADER");
                IRow headerRow = header.GetRow(1);
                headerRow.GetCell(0).SetCellValue(account.TaxId);
                headerRow.GetCell(1).SetCellValue(billingMonthAbr);
                headerRow.GetCell(2).SetCellValue(billingYear);
                headerRow.GetCell(3).SetCellValue("P2");
                headerRow.GetCell(4).SetCellValue(account.NPI);
                headerRow.GetCell(5).SetCellValue(account.ProvAhcccsId);


                //Details sheet
                ISheet details = billingFile.GetSheet("DETAILS");
                int detailRowIndex = 1;
                decimal totalBilled = 0;
                decimal totalUnits = 0;
                int totalRecords = 0;


                foreach (var item in _claims)
                {
                    item.CapPayments();
                    
                    IRow detailsRow = details.GetRow(detailRowIndex);

                    totalBilled += item.AmountDue;
                    totalRecords += 1;
                    totalUnits += item.DeliveredUnits + item.AbsentUnits;

                    //cell formula removed because its already calculated
                    detailsRow.GetCell(19).SetCellFormula(string.Empty);  //TotalAmountDue

                    detailsRow.CreateCell(0).SetCellValue("AA");//ProvSvcLocation 
                    detailsRow.GetCell(2).SetCellValue(item.ClientGovtProgId); //clientId
                    detailsRow.GetCell(3).SetCellValue(item.ServiceDate.Date); // start date
                    detailsRow.GetCell(4).SetCellValue(item.ServiceDate.Date); // end date
                    detailsRow.CreateCell(5).SetCellValue(item.GovtSvcCode); // 3 letter service code
                    detailsRow.CreateCell(7).SetCellValue(Convert.ToDouble(item.DeliveredUnits)); // delivered units
                    detailsRow.CreateCell(8).SetCellValue(Convert.ToDouble(item.AbsentUnits)); // absent units
                    detailsRow.CreateCell(9).SetCellValue(Convert.ToDouble(item.Rate)); // rate
                    detailsRow.CreateCell(10).SetCellValue(item.TPLCode1); // TPLCode1
                    detailsRow.CreateCell(11).SetCellValue(Convert.ToDouble(item.TPLAmount1.GetValueOrDefault(0))); // TPLAmount1
                    detailsRow.CreateCell(12).SetCellValue(item.TPLReCode1); // TPLReCode1
                    detailsRow.CreateCell(13).SetCellValue(item.TPLCode2); // TPLCode2
                    detailsRow.CreateCell(14).SetCellValue(Convert.ToDouble(item.TPLAmount2.GetValueOrDefault(0))); // TPLAmount2
                    detailsRow.CreateCell(15).SetCellValue(item.TPLReCode2); // TPLReCode2
                    detailsRow.CreateCell(16).SetCellValue(item.TPLCode3); // TPLCode3
                    detailsRow.CreateCell(17).SetCellValue(Convert.ToDouble(item.TPLAmount3.GetValueOrDefault(0))); // TPLAmount3
                    detailsRow.CreateCell(18).SetCellValue(item.TPLReCode3); // TPLReCode3
                    detailsRow.CreateCell(19).SetCellValue(Convert.ToDouble(item.AmountDue)); // AmountDue
                    detailsRow.CreateCell(20).SetCellValue(Convert.ToString(item.ClaimId)); // ClaimId
                    detailsRow.CreateCell(21).SetCellValue(item.StaffStateId); // StaffStateId
                    detailsRow.CreateCell(22).SetCellValue(item.StaffNPI); // StaffNPI
                    detailsRow.CreateCell(23).SetCellValue(item.PlaceOfService); // PlaceOfService
                    detailsRow.CreateCell(26).SetCellValue(item.ProcMod3); // ProcMod3
                    detailsRow.CreateCell(27).SetCellValue(item.TPLCode4); // TPLCode4
                    detailsRow.CreateCell(28).SetCellValue(Convert.ToDouble(item.TPLAmount4)); // TPLAmount4
                    detailsRow.CreateCell(29).SetCellValue(item.TPLReCode4); // TPLReCode4
                    detailsRow.CreateCell(30).SetCellValue(item.TPLCode5); // TPLCode5
                    detailsRow.CreateCell(31).SetCellValue(Convert.ToDouble(item.TPLAmount5)); // TPLAmount5
                    detailsRow.CreateCell(32).SetCellValue(item.TPLReCode5); // TPLReCode5
                    detailsRow.CreateCell(33).SetCellValue(item.TPLCode6); // TPLCode6
                    detailsRow.CreateCell(34).SetCellValue(Convert.ToDouble(item.TPLAmount6)); // TPLAmount6
                    detailsRow.CreateCell(35).SetCellValue(item.TPLReCode6); // TPLReCode6
                    detailsRow.CreateCell(36).SetCellValue(item.TPLCode7); // TPLCode7
                    detailsRow.CreateCell(37).SetCellValue(Convert.ToDouble(item.TPLAmount7)); // TPLAmount7
                    detailsRow.CreateCell(38).SetCellValue(item.TPLReCode7); // TPLReCode7
                    detailsRow.CreateCell(39).SetCellValue(item.TPLCode8); // TPLCode8
                    detailsRow.CreateCell(40).SetCellValue(Convert.ToDouble(item.TPLAmount8)); // TPLAmount8
                    detailsRow.CreateCell(41).SetCellValue(item.TPLReCode8); // TPLReCode8
                    detailsRow.CreateCell(42).SetCellValue(item.TPLCode9); // TPLCode9
                    detailsRow.CreateCell(43).SetCellValue(Convert.ToDouble(item.TPLAmount9)); // TPLAmount9
                    detailsRow.CreateCell(44).SetCellValue(item.TPLReCode9); // TPLReCode9
                    detailsRow.CreateCell(24).SetCellValue(item.ProcMod1); // Teletherapy Mod
                    detailsRow.CreateCell(65).SetCellValue(item.OrderingPhysicianNPI); // Ordering Physician NPI

                    if(item.DiagnosisCodes != null && item.DiagnosisCodes.Any())
                    {
                        int colIdx = 48;   //ClientDiagnosisCode1
                        int maxIdx = 59;   //ClientDiagnosisCode12

                        foreach(var diagnosis in item.DiagnosisCodes.Where(x => x.InsurancePolicyId == item.Claim.InsurancePolicyId))
                        {
                            if (colIdx > maxIdx) break;
                            var sanitizedDiagnosisCode = !string.IsNullOrEmpty(diagnosis.Code) ? diagnosis.Code.Replace(".", "").Replace("-", "").Trim() : "";
                            detailsRow.CreateCell(colIdx).SetCellValue(sanitizedDiagnosisCode);   // ClientDiagnosisCodeX
                            colIdx++;
                        }
                    }

                    detailRowIndex++;
                }

                //Footer sheet
                ISheet footer = billingFile.GetSheet("FOOTER");
                IRow footerRow = footer.GetRow(1);

                footerRow.GetCell(0).SetCellValue(totalRecords);
                footerRow.GetCell(1).SetCellValue(Convert.ToDouble(totalUnits));
                footerRow.GetCell(2).SetCellValue(Convert.ToDouble(totalBilled));

                //pass your excel bytes here
                billingFile.Write(bosExcel);
                dddBytes = bosExcel.ToArray();
                byteFiles.Add(dddBytes);


                //create cover sheet
                string coverSheetTemplate = $"{templateFolder}\\DDDCoverDoc.pdf";
                DateTime stdt = Convert.ToDateTime(billingMonth + "/1/" + (2000 + Convert.ToInt32(billingYear)));
                PdfDocument pdfDoc = PdfReader.Open(coverSheetTemplate, PdfDocumentOpenMode.Import);
                PdfDocument pdfNewDoc = new PdfDocument();
                PdfPage pp = pdfNewDoc.AddPage(pdfDoc.Pages[0]);
                XGraphics gfx = XGraphics.FromPdfPage(pp);
                XTextFormatter tf = new XTextFormatter(gfx);
                float y = 97;
                float yOffset = 24.5F;
                int x1 = 37;
                int x2 = 308;
                int x3 = 440;
                XFont A12B = new XFont("Arial", 11, XFontStyle.Regular);


                XRect rect = new XRect(x1, y, 200, 13);
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(account.Company.Name, A12B, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(x2, y, 200, 13);
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(account.Company.CompanyCode, A12B, XBrushes.Black, rect, XStringFormats.TopLeft);

                y += yOffset;
                rect = new XRect(x1, y, 200, 13);
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(account.Company.SkilledBillingContactName, A12B, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(x2, y, 200, 13);
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(account.TaxId, A12B, XBrushes.Black, rect, XStringFormats.TopLeft);

                y += yOffset;
                rect = new XRect(x1, y, 200, 13);
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(account.Company.SkilledBillingPhone, A12B, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(x2, y, 200, 13);
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(account.Company.SkilledBillingEmail, A12B, XBrushes.Black, rect, XStringFormats.TopLeft);

                y += yOffset;
                rect = new XRect(x1, y, 200, 13);
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(account.Company.SkilledBillingAddress, A12B, XBrushes.Black, rect, XStringFormats.TopLeft);

                y += yOffset;
                rect = new XRect(x1, y, 200, 13);
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(account.Company.SkilledBillingCity, A12B, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(x2, y, 200, 13);
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(account.Company.SkilledBillingState, A12B, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(x3, y, 200, 13);
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(account.Company.SkilledBillingZipCode, A12B, XBrushes.Black, rect, XStringFormats.TopLeft);

                y += yOffset;
                rect = new XRect(x1, y, 200, 13);
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString((string.Format("{0:MMM}", stdt)).ToUpper() + " " + stdt.ToString("yy"), A12B, XBrushes.Black, rect, XStringFormats.TopLeft);
                rect = new XRect(x2, y, 200, 13);
                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(String.Format("{0:C}", Math.Round(totalBilled, 2), CultureInfo.CurrentCulture), A12B, XBrushes.Black, rect, XStringFormats.TopLeft);

                //pass your pdf bytes here
                var ms = new MemoryStream();
                pdfNewDoc.Save(ms);
                coverSheetBytes = new byte[ms.Length];
                ms.Read(coverSheetBytes, 0, coverSheetBytes.Length);
                byteFiles.Add(coverSheetBytes);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return byteFiles;
        }
        #endregion

        #region Private Methods
        private static string GetRelationshipCode(PolicyDTO policy)
        {
            string relationshipCode;
            switch (policy.InsuranceRelationshipId)
            {
                case (int)InsuranceRelationshipEnum.Self:
                    relationshipCode = "18";
                    break;
                case (int)InsuranceRelationshipEnum.Spouse:
                    relationshipCode = "01";
                    break;
                case (int)InsuranceRelationshipEnum.Child:
                    relationshipCode = "19";
                    break;
                default:
                    relationshipCode = "21";
                    break;
            }
            return relationshipCode;
        }

        private static string PolicyOrderCode(int order)
        {
            string oName = order.ToString();
            switch (order)
            {
                case 1:
                    oName = "P";
                    break;
                case 2:
                    oName = "S";
                    break;
                case 3:
                    oName = "T";
                    break;
            }
            return oName;
        }
        #endregion
    }
}


