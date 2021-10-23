using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.AcroForms;

using DCC.Helpers;
using DCC.Models;
using DCC.Models.Clients;
using DCC.Geo;
using DCC.ModelsLegacy;

using System.Globalization;
using DCC.SQLHelpers.Helpers;


namespace DCC.Controllers
{

    public class ClientsController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;
        public ClientsController()
        {
            sqlHelper = new SQLHelper();
        }
        BillingInsuranceCompanyController billingInsuranceCompanies = new BillingInsuranceCompanyController();


        [AJAXAuthorize]
        public async Task<ActionResult> Index()
        {
            ClientInit r = new ClientInit();

            // For Admin get current clients & clients that have been deactivated in the last 12 months
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsGetClientList", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            try
            {
                r.clientList = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new Client()
                {
                    id = (int)spR["clsvid"],
                    name = (string)spR["nm"],
                    deleted = (bool)spR["deleted"]
                }).ToList();

             
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            ds.Dispose();

            setViewModelBase((ViewModelBase)r);

            var genderType = from GenderTypeEnum e in Enum.GetValues(typeof(GenderTypeEnum))
                             select new
                             {
                                 ID = (int)e,
                                 Name = e.ToString()
                             };

            ViewBag.EnumList = new SelectList(genderType, "ID", "Name");

            return View(r);
        }
       
        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> Client(int id)
        {
            ClientPageData r = new ClientPageData();
            r.documentationStart = DateTimeLocal().AddDays(-14).ToString("yyyy-MM-dd");
            r.documentationEnd = DateTimeLocal().ToString("yyyy-MM-dd");
            r.documentationSelNotes = true;
            r.documentationSelReports = true;
            r.userLevel = UserClaim.userLevel;
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsGetClientNew", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@clsvId", id);
                        cmd.Parameters.AddWithValue("@TimeZone", UserClaim.timeZone);
                        cmd.Parameters.AddWithValue("@docStartDate", r.documentationStart);
                        cmd.Parameters.AddWithValue("@docEndDate", r.documentationEnd);
                        cmd.Parameters.AddWithValue("@docSelectNotes", r.documentationSelNotes);
                        cmd.Parameters.AddWithValue("@docSelectReports", r.documentationSelReports);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            if (r.er.code == 0)
            {
                try
                {
                    setClientProfile(ref r, ref ds, false);
                    r.services = setClientServices(ref ds, 1);
                    setClientServiceDetails(ref ds, 2, 3, 4, 15, ref r);
                    r.geoLocations = setGeoLocations(ref ds, 5);
                    r.charts = setClientCharts(ref ds, 6);
                    r.commentHistory = setCommentHistory(ref ds, 7);
                    r.serviceObjectives = setObjectives(ref ds, 8);
                    r.careAreas = setCareAreas(ref ds, 9);
                    r.documentation = setClientDocumentation(ref ds, 10);
                    r.providerServices = setProviderServices(ref ds, 11);
                    ///  r.careAreaOptions = setCareAreaOptions(ref ds, 11);
                    ///   r.objectiveOptions = setObjectiveOptions(ref ds, 12);
                    r.policies = setPolicy(ref ds, 12);
                    //r.serviceList = setServiceList(ref ds, 14);
                    r.clientClaims = setClaim(ref ds, 13);
                    DataView dv = new DataView(ds.Tables[1]);
                    dv.RowFilter = "hasCareAreas<>0";
                    if (dv.Count != 0)
                        r.hasCareAreas = true;
                    dv.Dispose();
                }
                catch (Exception ex)
                {
                    r.er.code = 1;
                    r.er.msg = ex.Message;
                }
            }
            ds.Dispose();
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Page", r);
        }





        private void getPolicyObject(int policyId, ref InsurancePolicyDTO policy, int InsurancePolicyId)
        {

            DataSet ds = new DataSet();
            var dt = new DataTable();
            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd2 = new SqlCommand("sp_ClientsGetInsurancePolicyNew", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd2.Parameters.AddWithValue("@ID", policyId);
                sqlHelper.ExecuteSqlDataAdapter(cmd2, ds);
               
            }
            DataSet dsPrg = getTherapyTierList().Result;
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                policy.insurancePolicyID = row["InsurancePolicyid"] == DBNull.Value ? 0 : Convert.ToInt32(row["InsurancePolicyid"]);
                policy.phone = Convert.ToString(row["Phone"]);
                policy.firstName = Convert.ToString(row["FirstName"]);
                policy.lastName = Convert.ToString(row["LastName"]);
                policy.addressLine1 = Convert.ToString(row["AddressLine"]);
                policy.addressLine2 = Convert.ToString(row["AddressLine2"]);
                policy.genderId = row["GenderId"] == DBNull.Value ? 0 : Convert.ToInt32(row["GenderId"]);
                policy.city = Convert.ToString(row["City"]);
                policy.state = Convert.ToString(row["State"]).ToUpper();
                if (InsurancePolicyId != 0)
                {
                    policy.companyId = row["InsuranceCompanyId"] == DBNull.Value ? 0 : Convert.ToInt32(row["InsuranceCompanyId"]);
                    policy.insurancePolicyID = row["InsurancePolicyId"] == DBNull.Value ? 0 : Convert.ToInt32(row["InsurancePolicyId"]);
                    policy.insuredIdNo = Convert.ToString(row["InsuredIdNo"]);
                    policy.startDate = row["StartDate"] == DBNull.Value ? "" : ((DateTime)row["StartDate"]).ToString("yyyy-MM-dd");
                    policy.endDate = ((DateTime)row["EndDate"]).ToString("yyyy-MM-dd") == DateTime.MaxValue.ToString("yyyy-MM-dd") ? "" : ((DateTime)row["EndDate"]).ToString("yyyy-MM-dd");
                    policy.mcid = Convert.ToString(row["MCID"]);
                    policy.policyGroupNumber = Convert.ToString(row["PolicyNumber"]);
                    policy.hasWaivers = Convert.ToString(row["HasWaivers"]);
                }

                policy.postalCode = Convert.ToString(row["PostalCode"]);

   //             policy.patientIdNo = Convert.ToString(row["PatientIdNo"]);
                policy.insRelId = row["InsuranceRelationshipId"] == DBNull.Value ? 0 : Convert.ToInt32(row["InsuranceRelationshipId"]);
                policy.InsurancePriorityID = row["InsurancePriorityId"] == DBNull.Value ? 0 : Convert.ToInt32(row["InsurancePriorityId"]);

                policy.dob = row["InsuredDoB"] == DBNull.Value ? "" : ((DateTime)row["InsuredDoB"]).ToString("yyyy-MM-dd");
             
                policy.AssistID = Convert.ToString(row["AssistID"]);


                policy.Inactive = (bool)row["Inactive"];

                policy.Expired = ((DateTime)row["EndDate"])> DateTimeLocal().AddDays(-1) ? false : true;

            }

            policy.PolicyWaivers = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new PolicyWaiverDTO()
            {
                ServiceId = Convert.ToInt32(spR["ClientServiceId"] == DBNull.Value ? 3 : spR["ClientServiceId"]),
                StartDate = spR["StartDate"] == DBNull.Value ? "" : ((DateTime)spR["StartDate"]).ToString("MM/dd/yyyy", CultureInfo.GetCultureInfo("en-us")),
                EndDate = ((DateTime)spR["EndDate"]).ToString("yyyy-MM-dd") == DateTime.MaxValue.ToString("yyyy-MM-dd") ? "" : ((DateTime)spR["EndDate"]).ToString("yyyy-MM-dd"),
            Units = Convert.ToString(spR["Units"]),
                ServiceName = Convert.ToString(spR["Name"]),
                PolicyWaiverId = spR["PolicyWaiverId"] == DBNull.Value ? 0 : Convert.ToInt32(spR["PolicyWaiverId"]),
            }).ToList();

            if (InsurancePolicyId != 0)
            {
                policy.PreAuths = ds.Tables[5].Rows.Cast<DataRow>().Select(spR => new PreAuthDTO()
                {
                    ServiceId = Convert.ToInt32(spR["ClientServiceId"] == DBNull.Value ? 3 : spR["ClientServiceId"]),
                    StartDate = ((DateTime)spR["StartDate"]).ToString("yyyy-MM-dd") == DateTime.MinValue.ToString("yyyy-MM-dd") ? "" : ((DateTime)spR["StartDate"]).ToString("MM/dd/yyyy", CultureInfo.GetCultureInfo("en-us")),
                    EndDate = ((DateTime)spR["EndDate"]).ToString("yyyy-MM-dd") == DateTime.MaxValue.ToString("yyyy-MM-dd") ? "" : ((DateTime)spR["EndDate"]).ToString("MM/dd/yyyy", CultureInfo.GetCultureInfo("en-us")),
                    Units = (bool)spR["isApplicable"] ? Convert.ToString(spR["Units"]) : "",
                    ServiceName = Convert.ToString(spR["Name"]),
                    PreAuthorizationId = Convert.ToInt32(spR["PreAuthorizationId"]),
                    NotApplicable = !(bool)spR["isApplicable"]
                }).ToList();
            }
            else
                policy.PreAuths = new List<PreAuthDTO>();
            policy.ClientServiceCPTRates = ds.Tables[3].Rows.Cast<DataRow>().Select(spR => new ClientServiceCPTRate()
            {
                ClientServiceId = (int)spR["clientServiceId"],
                CPTCode = (string)spR["CptCode"],
                Amount = (decimal)spR["Amount"],
                Mod1 = (string)spR["Mod1"],
                Mod2 = (string)spR["Mod2"],
                Mod3 = (string)spR["Mod3"],
                //changed (string)spR["Units"] to Convert.ToString(spR["Units"]) as the former causes "Unable to cast object of type 'System.DBNull' to type 'System.String'." when value is Null
                Units = Convert.ToString(spR["Units"]),
            }).ToList();



            List<DiagnosisCode> diagnosisCodes = ds.Tables[2].Rows.Cast<DataRow>().Select(x => new DiagnosisCode()
            {
                Code = (string)x["Code"],
                ClientServiceID = (int)x["clientServiceId"]
            }).ToList();

            List<SelectListItem> diagnosisCodesFull = dt.Rows.Cast<DataRow>().Select(x => new SelectListItem()
            {
                Value = (string)x["id"],
                Text = (string)x["Name"],
            }).ToList();

           
        //    policy.DiagnosisCodes = diagnosisCodesFull;
          


            foreach (var service in policy.ClientServiceCPTRates)
            {
                var data = new SelectListItem();

                diagnosisCodesFull.ForEach(x =>
                {
                    service.DiagCodes.Add(
                        new SelectListItem()
                        {
                            Text = x.Text,
                            Value = x.Value,
                            Selected = diagnosisCodes.Any(d => d.ClientServiceID == service.ClientServiceId && d.Code.Trim() == x.Value.Trim())
                        });

                });

                policy.DiagnosisCodes.ForEach(x =>
                {
                    service.DiagCodes.Add(
                        new SelectListItem()
                        {
                            Text = x.Text,
                            Value = x.Value,
                            Selected = diagnosisCodes.Any(d => d.ClientServiceID == service.ClientServiceId && d.Code.Trim() == x.Value.Trim())
                        });

                });


           
            }

      

            policy.DDDClientAuths = new List<DDDClientAuth>();

            foreach (DataRow row in ds.Tables[4].Rows)
            {
                DDDClientAuth auth = new DDDClientAuth()
                {
                    ID = (int)row["ClientServiceId"],
                    StartDate = row["stdt"] == DBNull.Value ? "" : ((DateTime)row["stdt"]).ToString("MM/dd/yyyy", CultureInfo.GetCultureInfo("en-us")),
                    EndDate = row["eddt"] == DBNull.Value ? "" : ((DateTime)row["eddt"]).ToString("MM/dd/yyyy", CultureInfo.GetCultureInfo("en-us")),
                    HasWaiver = row["HasWaivers"].ToString(),
                    ServiceName = row["name"].ToString(),
                    HCPC = row["HCPC"].ToString(),
                    DDDUnits = row["Units"] == DBNull.Value ? 0 : (decimal)row["Units"],
                    DiagnosisCode = row["dddDiagnosis"].ToString()
                };

                policy.DDDClientAuths.Add(auth);
            }
           
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]

        public async Task<ActionResult> ManagePolicy(InsurancePolicyDTO policy)
        {

          
            ClientPageData r = new ClientPageData();
            r.userLevel = UserClaim.userLevel;

            DataTable Waivers = new DataTable();
            Waivers.Columns.Add("PolicyWaiverId");
            Waivers.Columns.Add("ClientServiceId");
            Waivers.Columns.Add("StartDate");
            Waivers.Columns.Add("EndDate");
            Waivers.Columns.Add("Units");

            DataTable PreAuths = new DataTable();
            PreAuths.Columns.Add("PreAuthorizationId");
            PreAuths.Columns.Add("ClientServiceId");
            PreAuths.Columns.Add("StartDate");
            PreAuths.Columns.Add("EndDate");
            PreAuths.Columns.Add("Units");
            PreAuths.Columns.Add("IsApplicable");

            DataTable CPTRates = new DataTable();
            CPTRates.Columns.Add("ClientServiceId");
            CPTRates.Columns.Add("CPTCode");
            CPTRates.Columns.Add("Mod1");
            CPTRates.Columns.Add("Mod2");
            CPTRates.Columns.Add("Mod3");
            CPTRates.Columns.Add("Units");

            DataTable DiagCodes = new DataTable();
            DiagCodes.Columns.Add("ClientServiceId");
            DiagCodes.Columns.Add("Code");

            DataTable PolicyList = new DataTable();
            PolicyList.Columns.Add("InsId");
            PolicyList.Columns.Add("InsPriorityId");

            if (policy.endDate == null)
            {
                policy.endDate = DateTime.MaxValue.ToString("yyyy-MM-dd");
            }


            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsManagePolicyNew", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@insurancePolicyID", policy.insurancePolicyID);
                        cmd.Parameters.AddWithValue("@clientId", policy.clientId);
                        cmd.Parameters.AddWithValue("@firstName", policy.firstName);
                        cmd.Parameters.AddWithValue("@lastName", policy.lastName);
                        cmd.Parameters.AddWithValue("@city", policy.city);
                        cmd.Parameters.AddWithValue("@state", policy.state);
                        cmd.Parameters.AddWithValue("@postalCode", policy.postalCode);
                        cmd.Parameters.AddWithValue("@addressLine", policy.addressLine1);
                        cmd.Parameters.AddWithValue("@addressLine2", policy.addressLine2);
                        cmd.Parameters.AddWithValue("@policyNumber", policy.policyGroupNumber == null ? "" : policy.policyGroupNumber);
                        cmd.Parameters.AddWithValue("@dob", policy.dob);
                        cmd.Parameters.AddWithValue("@insuredIdNumber",  policy.insuredIdNo);
                        cmd.Parameters.AddWithValue("@startDate", policy.startDate);
                        cmd.Parameters.AddWithValue("@endDate", policy.endDate);
                        cmd.Parameters.AddWithValue("@insuranceCompanyId", policy.companyId);
                        cmd.Parameters.AddWithValue("@phone", policy.phone);
                        cmd.Parameters.AddWithValue("@mcid", policy.mcid);
                        cmd.Parameters.AddWithValue("@lastChecked", DateTime.UtcNow.Date);
                        cmd.Parameters.AddWithValue("@genderId", policy.genderId);
                   //     cmd.Parameters.AddWithValue("@patientIdNo", policy.patientIdNo);
                        cmd.Parameters.AddWithValue("@InsurancePriorityId", policy.InsurancePriorityID);
                        cmd.Parameters.AddWithValue("@insuranceRelId", policy.insRelId);


                        if (policy.ClientServiceCPTRates != null)
                        {
                            foreach (var item in policy.ClientServiceCPTRates)
                            {
                                DataRow nRow = CPTRates.NewRow();
                                nRow["ClientServiceId"] = item.ClientServiceId;
                                nRow["CPTCode"] = item.CPTCode;
                                nRow["Mod1"] = item.Mod1 == null ? "" : item.Mod1;
                                nRow["Mod2"] = item.Mod2 == null ? "" : item.Mod2; ;
                                nRow["Mod3"] = item.Mod3 == null ? "" : item.Mod3; ;
                                nRow["Units"] = item.Units;
                                CPTRates.Rows.Add(nRow);
                                foreach (var diagCode in item.DiagnosisCodes)
                                {
                                    if (!string.IsNullOrWhiteSpace(diagCode))
                                    {
                                        nRow = DiagCodes.NewRow();
                                        nRow["ClientServiceId"] = item.ClientServiceId;
                                        nRow["Code"] = diagCode;
                                        DiagCodes.Rows.Add(nRow);
                                    }
                                }
                            }
                        }
                        cmd.Parameters.AddWithValue("@CPTRates", CPTRates);
                        cmd.Parameters.AddWithValue("@DiagCodes", DiagCodes);

                        if (policy.PolicyWaivers != null)
                        {
                            foreach (var item in policy.PolicyWaivers)
                            {
                                DataRow nRow = Waivers.NewRow();
                                nRow["PolicyWaiverId"] = item.PolicyWaiverId;
                                nRow["ClientServiceId"] = item.ServiceId;
                                nRow["StartDate"] = item.StartDate;
                                nRow["EndDate"] = item.EndDate == null ? DateTime.MaxValue.ToString("yyyy-MM-dd") : item.EndDate;
                                nRow["Units"] = item.Units;
                                Waivers.Rows.Add(nRow);
                            }
                        }
                        cmd.Parameters.AddWithValue("@Waivers", Waivers);

                        if (policy.PreAuths != null)
                        {
                            foreach (var item in policy.PreAuths)
                            {
                                if (item.EndDate == null)
                                    item.EndDate = DateTime.MaxValue.ToString("yyyy-MM-dd");
                                DataRow nRow = PreAuths.NewRow();
                                nRow["PreAuthorizationId"] = item.PreAuthorizationId;
                                nRow["ClientServiceId"] = item.ServiceId;
                                nRow["StartDate"] = item.StartDate == null || item.NotApplicable ? DateTime.MinValue.ToString("yyyy-MM-dd") : item.StartDate;
                                nRow["EndDate"] = item.EndDate == null || item.NotApplicable ? DateTime.MaxValue.ToString("yyyy-MM-dd") : item.EndDate;
                                nRow["Units"] = item.NotApplicable ? "0" : item.Units;
                                nRow["IsApplicable"] = !item.NotApplicable;
                                PreAuths.Rows.Add(nRow);
                            }
                        }
                        cmd.Parameters.AddWithValue("@PreAuths", PreAuths);
                       
                        if (policy.PolicyList != null)
                        {
                            foreach (var item in policy.PolicyList)
                            {
                                DataRow nRow = PolicyList.NewRow();
                                nRow["InsId"] = item.InsId;
                                nRow["InsPriorityId"] = item.InsPriorityId;
                                PolicyList.Rows.Add(nRow);
                            }
                        }
                        cmd.Parameters.AddWithValue("@PolicyList", PolicyList);

                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }

                });
                r.policies = setPolicy(ref ds, 0);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            CPTRates.Dispose();
            DiagCodes.Dispose();
            Waivers.Dispose();
            PreAuths.Dispose();
            PolicyList.Dispose();
            ds.Dispose();
   //         if (r.er.code == 0)
  //              r.policies = GetInsurancePoliciesByClientID(policy.clientId);

            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Insurance", r);
        }

     
        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]

        public async Task<ActionResult> OpenAddPolicyView(int clientID, int insurancePolicyId)
        {
            InsurancePolicyDTO policy = new InsurancePolicyDTO();
            Er er = new Er();

            List<ClientServices> clientServiceList = new List<ClientServices>();

            DataSet ds = new DataSet();
            DataSet clientInfo = new DataSet();
            SQLHelper sqlHelper = new SQLHelper();
            policy.clientId = clientID;
            await Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ClientsGetClientInsuranceOptions", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@clientId", clientID);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);


                   
                        var row = ds.Tables[7].Rows[0];
                        policy.firstName = Convert.ToString(row["fn"]);
                        policy.lastName = Convert.ToString(row["ln"]);
                        policy.addressLine1 = Convert.ToString(row["responsiblePersonAddress"]);
                        policy.addressLine2 = Convert.ToString(row["responsiblePersonAddress2"]);
                        policy.city = Convert.ToString(row["responsiblePersonCity"]);
                        policy.state = Convert.ToString(row["responsiblePersonState"]);
                        policy.postalCode = Convert.ToString(row["responsiblePersonZip"] == DBNull.Value ? null : row["responsiblePersonZip"]);
                        policy.phone = Convert.ToString(row["responsiblePersonTelephone"] == DBNull.Value ? null : row["responsiblePersonTelephone"]);
                        policy.dob = row["dob"] == DBNull.Value ? null : Convert.ToDateTime(row["dob"]).ToString("yyyy-MM-dd");
                        policy.Gender = Convert.ToString(row["Sex"]);
                    switch (policy.Gender)
                    {
                        case "Female"://GenderTypeEnum.Female:
                            policy.genderId = 0;
                            break;
                        case "Male":
                            policy.genderId = 1;
                            break;
                        default:
                            policy.genderId = 2;
                            break;
                    }





                    policy.AssistID = Convert.ToString(row["clID"]);
                    policy.hasWaivers = row["clID"] != DBNull.Value && Convert.ToString(row["clID"]).Length > 0 ? "Yes" : "No"; // hasWaivers seems to mean has DDD insurance
                   
                }
            });

            policy.InsuranceRelationShips = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new Option()
            {
                value = Convert.ToString(spR["InsuranceRelationshipId"]),
                name = (string)spR["Name"]
            }).ToList();


            policy.serviceList = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new Option()
            {
                value = Convert.ToString(spR["serviceId"]),
                name = (string)spR["Name"]
            }).ToList();

            List<SelectListItem> unitsList = new List<SelectListItem>();

            policy.clientServices = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new ClientServices()
            {
                Id = (int)spR["serviceid"],
                Name = (string)spR["Name"],
                Units = unitsList,
            }).ToList();

            foreach (DataRow row in ds.Tables[2].Rows)
            {
                string unit = (string) row["name"];
                //exclude 0.75
                if(unit == "0.75") continue;
                
                SelectListItem units = new SelectListItem();
                units.Text = unit == "0.25" ? "4" : unit == "0.5" ? "2" : "1";
                units.Value = (string)row["name"];
                unitsList.Add(units);
            }

            policy.clientServices = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new ClientServices()
            {
                Id = (int)spR["serviceid"],
                Name = (string)spR["Name"],
                CompanyServiceId = Convert.ToString(spR["CompanyServiceId"]),
                CPTCode = Convert.ToString(spR["CPTCode"]),
                Mod1 = Convert.ToString(spR["Mod1"]),
                Unit = Convert.ToString(spR["Unit"]),
                Units = unitsList,

            }).ToList();

            var CPTOpts = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = "Choose",
                    Value = ""
                }
            };

            CPTOpts.AddRange(ds.Tables[3].Rows.Cast<DataRow>().Select(spR => new SelectListItem()
            {
                Text = (string)spR["Code"] + "/" + spR["Name"].ToString(),
                Value = (string)spR["Code"]

            }).ToList());

            ViewBag.CPTCodes = CPTOpts;


            policy.DiagnosisCodes = ds.Tables[4].Rows.Cast<DataRow>().Select(spR => new SelectListItem()
            {
                Text = spR["Name"].ToString(),
                Value = (string)spR["id"]

            }).ToList();


            policy.companyInsuranceList = ds.Tables[5].Rows.Cast<DataRow>().Select(spR => new Option
            {
                name = spR["Name"].ToString(),
                value = spR["InsuranceCompanyId"].ToString(),
                InsCode = spR["InsCode"].ToString(),
                MCID = spR["MCID"].ToString()
            }).ToList();


            var genderType = from GenderTypeEnum e in Enum.GetValues(typeof(GenderTypeEnum))
                             select new
                             {
                                 ID = (int)e,
                                 Name = e.ToString(),

                             };
            ViewBag.GenderList = new SelectList(genderType, "ID", "Name");

            //InsurancePriorities 
            var insPriority = from InsuranceTierEnum e in Enum.GetValues(typeof(InsuranceTierEnum))
                              select new
                              {
                                  ID = (int)e,
                                  Name = e.ToString()
                              };
            ViewBag.InsurancePriorityList = new SelectList(insPriority, "ID", "Name");


            policy.DDDClientAuths = new List<DDDClientAuth>();

            foreach (DataRow row in ds.Tables[6].Rows)
            {
                DDDClientAuth auth = new DDDClientAuth()
                {
                    ID = (int)row["ClientServiceId"],
                    StartDate = row["stdt"] == DBNull.Value ? "" : ((DateTime)row["stdt"]).ToString("MM/dd/yyyy", CultureInfo.GetCultureInfo("en-us")),
                    EndDate = row["eddt"] == DBNull.Value ? "" : ((DateTime)row["eddt"]).ToString("MM/dd/yyyy", CultureInfo.GetCultureInfo("en-us")),
                    HasWaiver = row["HasWaivers"].ToString(),
                    ServiceName = row["name"].ToString(),
                    HCPC = row["HCPC"].ToString(),
                    DDDUnits = row["Units"] == DBNull.Value ? 0 : (decimal)row["Units"],
                    DiagnosisCode = row["dddDiagnosis"].ToString()
                };

                policy.DDDClientAuths.Add(auth);
            }

            int insurancePolicyToGetId = insurancePolicyId;
            if (insurancePolicyId == 0)
            {
                var commandText = string.Format("Select top 1 InsurancePolicyId from InsurancePolicy where clientId = {0} and  InsurancePriorityId in ({1})", clientID, "1,2,3");
                var primaryPolicyId = Convert.ToInt32(SqlExecuteScalar(commandText));
                if (primaryPolicyId > 0)
                    insurancePolicyToGetId = primaryPolicyId;

            }
            if (insurancePolicyToGetId != 0)
            {

                try
                {
                    getPolicyObject(insurancePolicyToGetId, ref policy, insurancePolicyId);
                }
                catch (Exception ex)
                {
                    er.code = 1;
                    er.msg = ex.Message;
                }
            }

            policy.clientId = clientID;
            policy.insurancePolicyID = insurancePolicyId;
            if (policy.insurancePolicyID == 0)
            {
                policy.InsurancePriorityID = 1;
                policy.isAddNew = true;
            }
            else
                policy.isAddNew = false;

            ds.Dispose();
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }

            return PartialView("ModalManagePolicy", policy);
        }

        private Task<DataSet> getTherapyTierList()
        {

            return Task.Run(() =>
           {
               using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
               {
                   DataSet ds = new DataSet();
                   SqlCommand cmd = new SqlCommand("sp_GetTherapyTier", cn)
                   {
                       CommandType = CommandType.StoredProcedure
                   };
                   sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                   return ds;
               }
           });
        }

        /*
        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]

        public async Task<ActionResult> OpenWaiverView(int clientId)
        {
            ClientInit r = new ClientInit();

            // For Admin get current clients & clients that have been deactivated in the last 12 months
            DataSet ds = new DataSet();
            DataSet dsDccMain = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsGetClientList", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@clientId", clientId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            try
            {
                r.serviceList = ds.Tables[4].Rows.Cast<DataRow>().Select(spR => new Option()
                {
                    value = Convert.ToString(spR["serviceId"]),
                    name = (string)spR["Name"]
                }).ToList();

            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            ds.Dispose();
            return PartialView("Client_Waiver", r);
        }
        */
        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]

        public async Task<ActionResult> ManageWaiver(PolicyWaiver policyWaiver)
        {
            ClientInit r = new ClientInit();
            // ClientPageData r = new ClientPageData();
            r.userLevel = UserClaim.userLevel;
            DataTable dt = new DataTable();
            DataSet ds = new DataSet();
            DataSet ds2 = new DataSet();

            List<PolicyWaiver> waivers = new List<PolicyWaiver>();

            try
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ClientsManagePolicyWaiver", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    try
                    {
                        cmd.Parameters.AddWithValue("@PolicyWaiverId", policyWaiver.PolicyWaiverId);
                        cmd.Parameters.AddWithValue("@InsurancePolicyId", policyWaiver.InsurancePolicyID);
                        cmd.Parameters.AddWithValue("@ClientServiceId", policyWaiver.ServiceID);
                        cmd.Parameters.AddWithValue("@StartDate", policyWaiver.FromDate);
                        cmd.Parameters.AddWithValue("@EndDate", policyWaiver.ToDate);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);

                    SqlCommand cmd2 = new SqlCommand("sp_ClientsServiceList", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    try
                    {
                        cmd2.Parameters.AddWithValue("@clientId", policyWaiver.ClientId);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    sqlHelper.ExecuteSqlDataAdapter(cmd2, ds2);
                }
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            if (r.er.code == 0)
            {
                r.policyWaivers = setWaiver(ref ds, 0);
                r.serviceList = setServiceList(ref ds2, 0);
            }

            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else

                return PartialView("Client_Waiver", r);

        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> WaiverList(int insurancePolicyID)
        {
            ClientPageData r = new ClientPageData();
            r.userLevel = UserClaim.userLevel;
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd2 = new SqlCommand("sp_ClientsGetWaiver", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd2.Parameters.AddWithValue("@InsurancePolicyId", insurancePolicyID);
                    sqlHelper.ExecuteSqlDataAdapter(cmd2, dt);
                }
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            List<PolicyWaiver> waiverList = new List<PolicyWaiver>();

            foreach (DataRow row in dt.Rows)
            {
                PolicyWaiver policyWaiver = new PolicyWaiver();

                policyWaiver.PolicyWaiverId = row["PolicyWaiverId"] == DBNull.Value ? 0 : Convert.ToInt32(row["PolicyWaiverId"]);
                policyWaiver.ServiceID = row["ClientServiceId"] == DBNull.Value ? 0 : Convert.ToInt32(row["ClientServiceId"]);
                policyWaiver.ServiceName = Convert.ToString(row["ServiceName"]);
                policyWaiver.FromDate = row["StartDate"] == DBNull.Value ? "" : ((DateTime)row["StartDate"]).ToShortDateString();
                policyWaiver.ToDate = row["EndDate"] == DBNull.Value ? "" : ((DateTime)row["EndDate"]).ToShortDateString();

                waiverList.Add(policyWaiver);
            }

            return Json(new { waiver = waiverList },
                   JsonRequestBehavior.AllowGet);

        }


        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> DeleteWaiver(PolicyWaiver policyWaiver)
        {
            ClientInit a = new ClientInit();
            DataSet ds = new DataSet();
            DataSet ds2 = new DataSet();

            List<PolicyWaiver> waivers = new List<PolicyWaiver>();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsDeletePolicyWaiver", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@PolicyWaiverId", policyWaiver.PolicyWaiverId);
                        cmd.Parameters.AddWithValue("@InsurancePolicyId", policyWaiver.InsurancePolicyID);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);

                        SqlCommand cmd2 = new SqlCommand("sp_ClientsServiceList", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd2.Parameters.AddWithValue("@clientId", policyWaiver.ClientId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd2, ds2);
                    }
                });

                //r.policies = setPolicy(ref ds, 0);
                a.policyWaivers = setWaiver(ref ds, 0);
                a.serviceList = ds2.Tables[0].Rows.Cast<DataRow>().Select(spR => new Option()
                {
                    value = Convert.ToString(spR["serviceId"]),
                    name = (string)spR["Name"]
                }).ToList();
                //a.serviceList = setServiceList(ref ds, 1);
            }
            catch (Exception ex)
            {
                a.er.code = 1;
                a.er.msg = ex.Message;

            }
           
            if (a.er.code != 0)
            {
                Response.Write(a.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else

                return PartialView("Client_Waiver", a);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> DeletePolicy(InsurancePolicyDTO policy)
        {
            ClientPageData r = new ClientPageData();
            r.userLevel = UserClaim.userLevel;
            DataSet ds = new DataSet();

            DataTable PolicyList = new DataTable();
            PolicyList.Columns.Add("InsId");
            PolicyList.Columns.Add("InsPriorityId");

            if (policy.PolicyList != null)
            {
                foreach(var item in policy.PolicyList)
                {
                    DataRow nRow = PolicyList.NewRow();
                    nRow["InsId"] = item.InsId;
                    nRow["InsPriorityId"] = item.InsPriorityId;
                    PolicyList.Rows.Add(nRow);
                }
            }

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsDeletePolicyNew", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        try
                        {
                            cmd.Parameters.AddWithValue("@InsurancePolicyID", policy.insurancePolicyID);
                            cmd.Parameters.AddWithValue("@clientId", policy.clientId);
                            cmd.Parameters.AddWithValue("@policyList", PolicyList);

                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.policies = setPolicy(ref ds, 0);
            }  
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            PolicyList.Dispose();
            ds.Dispose();
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Insurance", r);
        }


        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenActivateDeactivateClientModal(int id)
        {
            ClientProfile r = new ClientProfile();
            Er er = new Er();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientGetProfile", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@clsvId", id);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                DataRow dr = ds.Tables[0].Rows[0];
                r.clsvId = (int)dr["clsvId"];
                r.fn = (string)dr["fn"];
                r.ln = (string)dr["ln"];
                r.name = (string)dr["fn"] + " " + (string)dr["ln"];
                r.deleted = (bool)dr["deleted"];
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else if (r.deleted)
                return PartialView("ModalActivateClient", r);
            else
                return PartialView("ModalDeactivateClient", r);
        }


        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> ToggleClient(ClientProfile s)
        {
            ClientPageData r = new ClientPageData();
            ClientPageWindows w = new ClientPageWindows();
            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsToggleClientActiveNew", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clsvId", s.clsvId);
                        cmd.Parameters.AddWithValue("@deleted", s.deleted);
                        cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);

                    }
                });
                setClientProfile(ref r, ref ds, false);
                r.services = setClientServices(ref ds, 1);
                setClientServiceDetails(ref ds, 2, 3, 4,5, ref r);
                w.clientHeader = RenderRazorViewToString("Client_Header", r);
                w.clientProfile = RenderRazorViewToString("Client_Profile", r);
                w.clientServices = RenderRazorViewToString("Client_Services", r);

            }
            catch (Exception ex)
            {
                w.er.code = 1;
                w.er.msg = ex.Message;

            }
            ds.Dispose();
            if (w.er.code != 0)
            {
                Response.Write(w.er.msg);
                Response.StatusCode = 400;
                return null;
            }

            return Json(w);
        }

        [HttpGet]
        [AJAXAuthorize]
        public ActionResult GetNewClientModal()
        {
            return PartialView("ModalAddClient");
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> NewClient(NewClientReq c)
        {
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsAddClient", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@firstName", c.clientFirstName);
                        cmd.Parameters.AddWithValue("@lastName", c.clientLastName);
                        cmd.Parameters.AddWithValue("@assistNumber", c.assistNumber == null ? "" : c.assistNumber);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                c.clientId = Convert.ToInt32(ds.Tables[0].Rows[0].ItemArray[0]);
            }
            catch (Exception ex)
            {
                c.er.code = 1;
                c.er.msg = ex.Message;

            }
            ds.Dispose();



            return Json(c);
        }

        [HttpGet]
        [AJAXAuthorize]
        public ActionResult GetRecycleModal(Documentation d)
        {
            return PartialView("ModalRecycleDocument", d);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> RecycleDocumemt(Documentation d)
        {
            Er er = new Er();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_RecycleDocument", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@noteType", d.noteType);
                        cmd.Parameters.AddWithValue("@noteId", d.noteId);
                        cn.Open();
                        cmd.ExecuteNonQuery();
                        cn.Close();
                    }
                });
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }
            if (er.code == 0)
                er.msg = d.noteType + d.noteId;

            return Json(er);
        }



        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> GetClientProfile(int clsvId)
        {
            ClientPageData r = new ClientPageData();
            Er er = new Er();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientGetProfileForEdit", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@clsvId", clsvId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.userLevel = UserClaim.userLevel;
                setClientProfile(ref r, ref ds, true);
            }

            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalEditClientProfile", r.clientProfile);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> UpdateClientProfile(ClientProfile c)
        {

            ClientPageData r = new ClientPageData();
            r.userLevel = UserClaim.userLevel;

            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsUpdateClient", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clId", c.clId == null ? "" : c.clId);
                        cmd.Parameters.AddWithValue("@medicaidId", c.medicaidId == null ? "" : c.medicaidId);
                        cmd.Parameters.AddWithValue("@fn", c.fn);
                        cmd.Parameters.AddWithValue("@ln", c.ln);
                        cmd.Parameters.AddWithValue("@clsvId", c.clsvId);
                        cmd.Parameters.AddWithValue("@clwNm", c.clwNm);
                        cmd.Parameters.AddWithValue("@clwPh", c.clwPh);
                        cmd.Parameters.AddWithValue("@clwEm", c.clwEm);
                        cmd.Parameters.AddWithValue("@sex", c.sex);
                        cmd.Parameters.AddWithValue("@dob", c.dob);

                        cmd.Parameters.AddWithValue("@physicianTitle", c.physicianTitle == null ? "" : c.physicianTitle);
                        cmd.Parameters.AddWithValue("@physicianFirstName", c.physicianFirstName == null ? "" : c.physicianFirstName);
                        cmd.Parameters.AddWithValue("@physicianMI", c.physicianMI == null ? "" : c.physicianMI);
                        cmd.Parameters.AddWithValue("@physicianLastName", c.physicianLastName == null ? "" : c.physicianLastName);
                        cmd.Parameters.AddWithValue("@physicianSuffix", c.physicianSuffix == null ? "" : c.physicianSuffix);

                        cmd.Parameters.AddWithValue("@physicianAgency", c.physicianAgency == null ? "" : c.physicianAgency);
                        cmd.Parameters.AddWithValue("@physicianNPI", c.physicianNPI == null ? "" : c.physicianNPI);
                        cmd.Parameters.AddWithValue("@physicianPhone", c.physicianPhone == null ? "" : c.physicianPhone);
                        cmd.Parameters.AddWithValue("@physicianFax", c.physicianFax == null ? "" : c.physicianFax);
                        cmd.Parameters.AddWithValue("@physicianEmail", c.physicianEmail == null ? "" : c.physicianEmail);
                        cmd.Parameters.AddWithValue("@physicianAddress", c.physicianAddress == null ? "" : c.physicianAddress);
                        cmd.Parameters.AddWithValue("@physicianCity", c.physicianCity == null ? "" : c.physicianCity);
                        cmd.Parameters.AddWithValue("@physicianState", c.physicianState == null ? "" : c.physicianState);
                        cmd.Parameters.AddWithValue("@physicianZip", c.physicianZip == null ? "" : c.physicianZip);

                        cmd.Parameters.AddWithValue("@responsiblePersonLn", c.responsiblePersonLn == null ? "" : c.responsiblePersonLn);
                        cmd.Parameters.AddWithValue("@responsiblePersonFn", c.responsiblePersonFn == null ? "" : c.responsiblePersonFn);
                        cmd.Parameters.AddWithValue("@responsiblePersonPhone", c.responsiblePersonPhone == null ? "" : c.responsiblePersonPhone);
                        cmd.Parameters.AddWithValue("@responsiblePersonEmail", c.responsiblePersonEmail == null ? "" : c.responsiblePersonEmail);
                        cmd.Parameters.AddWithValue("@responsiblePersonAddress", c.responsiblePersonAddress == null ? "" : c.responsiblePersonAddress);
                        cmd.Parameters.AddWithValue("@responsiblePersonAddress2", c.responsiblePersonAddress2 == null ? "" : c.responsiblePersonAddress2);
                        cmd.Parameters.AddWithValue("@responsiblePersonCity", c.responsiblePersonCity == null ? "" : c.responsiblePersonCity);
                        cmd.Parameters.AddWithValue("@responsiblePersonState", c.responsiblePersonState == null ? "" : c.responsiblePersonState);
                        cmd.Parameters.AddWithValue("@responsiblePersonZip", c.responsiblePersonZip == null ? "" : c.responsiblePersonZip);
                        cmd.Parameters.AddWithValue("@responsiblePersonRelationshipId", c.responsiblePersonRelationshipId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                setClientProfile(ref r, ref ds, false);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }

            ds.Dispose();
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Profile", r);
        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> GetNewServices(int clsvId)
        {
            Er er = new Er();
            NewServices r = new NewServices();

            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientGetNewServices", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@clsvId", clsvId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.serviceOptions = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {
                    name = (string)spR["name"],
                    value = spR["serviceId"].ToString()
                }).ToList();


            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalAddNewService", r);


        }


        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> AddNewService(ClientService n)
        {
            ClientPageData r = new ClientPageData();


            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientAddNewServiceNew", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clsvId", n.clsvId);
                        cmd.Parameters.AddWithValue("@serviceId", n.serviceId);
                        cmd.Parameters.AddWithValue("@dddPay", n.dddPay);
                        cmd.Parameters.AddWithValue("@ppPay", n.ppPay);
                        cmd.Parameters.AddWithValue("@pInsPay", n.pInsPay);
                        cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.services = setClientServices(ref ds, 0);
                setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);

            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Services", r);


        }


        [AJAXAuthorize]
        public ActionResult GetNewAuth(int clsvidId)
        {
            return PartialView("ModalAddAuthorization", clsvidId);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> AddNewAuth(Auth n)
        {
            ClientPageData r = new ClientPageData();


            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientAddNewAuthNew", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clsvId", n.clsvId);
                        cmd.Parameters.AddWithValue("@clsvidId", n.clsvidId);
                        cmd.Parameters.AddWithValue("@stDt", n.stdt);
                        cmd.Parameters.AddWithValue("@edDt", n.eddt);
                        cmd.Parameters.AddWithValue("@au", n.au);
                        cmd.Parameters.AddWithValue("@tempAddedUnits", n.tempAddedUnits);
                        cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);

                      //  cmd.Parameters.AddWithValue("@ru", n.ru);
                      //  cmd.Parameters.AddWithValue("@uu", n.uu);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                if (ds.Tables.Count != 0)
                {
                    r.services = setClientServices(ref ds, 0);
                    setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);
                }
                else
                {
                    r.er.code = 1;
                    r.er.msg = "Authorizations dates would overlap with an existing auth";

                }

            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Services", r);


        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> GetEditAuthModal(int auId)
        {
            Auth a = new Auth();
            Er er = new Er();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientGetAuth", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@auId", auId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                DataRow dr = ds.Tables[0].Rows[0];
                a.auId = auId;
                a.stdt = ((DateTime)dr["stdt"]).ToString("yyyy-MM-dd");
                a.eddt = ((DateTime)dr["eddt"]).ToString("yyyy-MM-dd");
                a.au = (decimal)dr["au"];
                a.tempAddedUnits = (decimal)dr["tempAddedUnits"];
                a.weeklyHourOverride = (decimal)dr["weeklyHourOverride"];
                a.service = (string)dr["service"];
            }

            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalEditAuthorization", a);
        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> GetDelAuthModal(int auId)
        {
            Auth a = new Auth();
            Er er = new Er();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientGetAuth", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@auId", auId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                DataRow dr = ds.Tables[0].Rows[0];

                a.auId = auId;
                a.stdt = ((DateTime)dr["stdt"]).ToShortDateString();
                a.eddt = ((DateTime)dr["eddt"]).ToShortDateString();
                a.au = (decimal)dr["au"];
                a.tempAddedUnits = (decimal)dr["tempAddedUnits"];
                a.service = (string)dr["service"];
            }

            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalDeleteAuthorization", a);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> DeleteAuth(int clsvId, int auId)
        {
            ClientPageData r = new ClientPageData();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientDeleteAuthNew", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clsvId", clsvId);
                        cmd.Parameters.AddWithValue("@auId", auId);
                        cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.services = setClientServices(ref ds, 0);
                setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);

            }

            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Services", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> UpdateAuth(Auth n)
        {
            ClientPageData r = new ClientPageData();


            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientUpdateAuthNew", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clsvId", n.clsvId);
                        cmd.Parameters.AddWithValue("@auId", n.auId);
                        cmd.Parameters.AddWithValue("@stDt", n.stdt);
                        cmd.Parameters.AddWithValue("@edDt", n.eddt);
                        cmd.Parameters.AddWithValue("@au", n.au);
                        cmd.Parameters.AddWithValue("@tempAddedUnits", n.tempAddedUnits);
                        cmd.Parameters.AddWithValue("@weeklyHourOverride", n.weeklyHourOverride);
                        cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                if (ds.Tables.Count != 0)
                {
                    r.services = setClientServices(ref ds, 0);
                    setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);
                }
                else
                {
                    r.er.code = 1;
                    r.er.msg = "Authorizations dates would overlap with an existing auth";

                }

            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Services", r);

        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> GetEditISPDatesModal(int clsvidId)
        {
            ClientService c = new ClientService();
            Er er = new Er();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientGetServiceISPDates", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@clsvidId", clsvidId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                DataRow dr = ds.Tables[0].Rows[0];
                c.clsvidId = clsvidId;
                c.ISPStart = dr["ISPStart"] == DBNull.Value ? "" : ((DateTime)dr["ISPStart"]).ToString("yyyy-MM-dd");
                c.ISPEnd = dr["ISPEnd"] == DBNull.Value ? "" : ((DateTime)dr["ISPEnd"]).ToString("yyyy-MM-dd");
                c.svcLong = (string)dr["service"];
            }

            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }
            ds.Dispose();
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalEditISPDates", c);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> UpdateISPDates(ClientService n)
        {
            ClientPageData r = new ClientPageData();


            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientUpdateISPDatesNew", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clsvId", n.clsvId);
                        cmd.Parameters.AddWithValue("@clsvidId", n.clsvidId);
                        cmd.Parameters.AddWithValue("@ISPStart", n.ISPStart);
                        cmd.Parameters.AddWithValue("@ISPEnd", n.ISPEnd);
                        cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.services = setClientServices(ref ds, 0);
                setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Services", r);

        }


        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> GetEditPOCDatesModal(int clsvidId)
        {
            ClientService c = new ClientService();
            Er er = new Er();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientGetServicePOCDates", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@clsvidId", clsvidId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                DataRow dr = ds.Tables[0].Rows[0];
                c.clsvidId = clsvidId;
                c.POCStart = dr["POCStart"] == DBNull.Value ? "" : ((DateTime)dr["POCStart"]).ToString("yyyy-MM-dd");
                c.POCEnd = dr["POCEnd"] == DBNull.Value ? "" : ((DateTime)dr["POCEnd"]).ToString("yyyy-MM-dd");
                c.svcLong = (string)dr["service"];
            }

            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }
            ds.Dispose();
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalEditPOCDates", c);
        }


        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> UpdatePOCDates(ClientService n)
        {
            ClientPageData r = new ClientPageData();


            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientUpdatePOCDatesNew", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clsvId", n.clsvId);
                        cmd.Parameters.AddWithValue("@clsvidId", n.clsvidId);
                        cmd.Parameters.AddWithValue("@POCStart", n.POCStart);
                        cmd.Parameters.AddWithValue("@POCEnd", n.POCEnd);
                        cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.services = setClientServices(ref ds, 0);
                setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Services", r);

        }


        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenActivateServiceModal(int clsvidId)
        {
            Er er = new Er();
            ClientService r = new ClientService();

            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientGetService", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@clsvidId", clsvidId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                DataRow dr = ds.Tables[0].Rows[0];
                r.clsvidId = clsvidId;
                r.svcLong = (string)dr["service"];
                r.deleted = (bool)dr["deleted"];
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }

            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalActivateDeactivateService", r);


        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> ToggleService(ClientService t)
        {
            ClientPageData r = new ClientPageData();
            r.userLevel = UserClaim.userLevel;
            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsToggleServiceActiveNew", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clsvId", t.clsvId);
                        cmd.Parameters.AddWithValue("@clsvidId", t.clsvidId);
                        cmd.Parameters.AddWithValue("@deleted", t.deleted);
                        cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.services = setClientServices(ref ds, 0);
                setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);

            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }

            ds.Dispose();
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Services", r);
        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> GetService(int clsvidId)
        {
            Er er = new Er();
            ClientService r = new ClientService();

            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientGetService", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@clsvidId", clsvidId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                DataRow dr = ds.Tables[0].Rows[0];
                r.clsvidId = clsvidId;
                r.svcLong = (string)dr["service"];
                r.dddPay = (bool)dr["dddPay"];
                r.ppPay = (bool)dr["ppPay"];
                r.pInsPay = (bool)dr["pInsPay"];
                r.contingencyPlanId = dr["contingencyPlanId"].ToString();

                r.contingencyPlans = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {
                    value = Convert.ToString(spR["contingencyPlanId"]),
                    name = (string)spR["Description"]
                }).ToList();


            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }

            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalUpdateService", r);


        }



        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> UpdateService(ClientService n)
        {
            ClientPageData r = new ClientPageData();


            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientUpdateServiceNew", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clsvId", n.clsvId);
                        cmd.Parameters.AddWithValue("@clsvidId", n.clsvidId);
                        cmd.Parameters.AddWithValue("@dddPay", n.dddPay);
                        cmd.Parameters.AddWithValue("@ppPay", n.ppPay);
                        cmd.Parameters.AddWithValue("@pInsPay", n.pInsPay);
                        cmd.Parameters.AddWithValue("@contingencyPlanId", n.contingencyPlanId);
                        cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.services = setClientServices(ref ds, 0);
                setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);

            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Services", r);


        }





        [AJAXAuthorize]
        public async Task<ActionResult> OpenEditProgressReportDateModal(int clientServiceId)
        {
            Er er = new Er();
            ClientService r = new ClientService();

            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientGetService", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@clsvidId", clientServiceId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                DataRow dr = ds.Tables[0].Rows[0];
                r.clsvidId = clientServiceId;
                r.nextReportDueDate = dr["nextReportDueDate"] == DBNull.Value ? "" : ((DateTime)dr["nextReportDueDate"]).ToString("yyyy-MM-dd");
                r.reportingPeriodId = (int)dr["reportingPeriodId"];
              
                if (r.reportingPeriodId == 1)
                {
                    // monthly
                    DateTime Date = DateTimeLocal().AddMonths(-1);
                    r.reportPeriodEndDateOption = new List<SelectOption>();
                    int lastDayOfMonth = DateTime.DaysInMonth(Date.Year, Date.Month);                
                    var date = new DateTime(Date.Year, Date.Month, lastDayOfMonth).ToShortDateString();
                     r.reportPeriodEndDateOption.Add(new SelectOption() { name = date, value = date});
                    for (var i = 0; i < 3; i++)
                    {
                        Date = Date.AddMonths(1);
                        lastDayOfMonth = DateTime.DaysInMonth(Date.Year, Date.Month);
                        date = new DateTime(Date.Year, Date.Month, lastDayOfMonth).ToShortDateString();
                        r.reportPeriodEndDateOption.Add(new SelectOption() { name = date, value = date });
                    }
                }
                else if (r.reportingPeriodId == 2)
                {
                    // quarterly
                    DateTime Date = DateTimeLocal().AddMonths(-4);
                    r.reportPeriodEndDateOption = new List<SelectOption>();
                    while (Date.Month != 3 && Date.Month != 6 & Date.Month != 9 && Date.Month != 12)
                    {
                        Date = Date.AddMonths(1);
                    }
                    int lastDayOfMonth = DateTime.DaysInMonth(Date.Year, Date.Month);
                    var date = new DateTime(Date.Year, Date.Month, lastDayOfMonth).ToShortDateString();
                    r.reportPeriodEndDateOption.Add(new SelectOption() { name = date, value = date });
                    for (var i = 0; i < 2; i++)
                    {
                        Date = Date.AddMonths(3);
                        lastDayOfMonth = DateTime.DaysInMonth(Date.Year, Date.Month);
                        date = new DateTime(Date.Year, Date.Month, lastDayOfMonth).ToShortDateString();
                        r.reportPeriodEndDateOption.Add(new SelectOption() { name = date, value = date });
                    }

                }
                else if (r.reportingPeriodId == 3)
                {
                    // semi-annual
                    DateTime Date = DateTimeLocal().AddMonths(-3);
                    r.reportPeriodEndDateOption = new List<SelectOption>();
                    while (Date.Month != 6 && Date.Month != 12)
                    {
                        Date = Date.AddMonths(1);
                    }
                    int lastDayOfMonth = DateTime.DaysInMonth(Date.Year, Date.Month);
                    var date = new DateTime(Date.Year, Date.Month, lastDayOfMonth).ToShortDateString();
                    r.reportPeriodEndDateOption.Add(new SelectOption() { name = date, value = date });
                    for (var i = 0; i < 1; i++)
                    {
                        Date = Date.AddMonths(6);
                        lastDayOfMonth = DateTime.DaysInMonth(Date.Year, Date.Month);
                        date = new DateTime(Date.Year, Date.Month, lastDayOfMonth).ToShortDateString();
                        r.reportPeriodEndDateOption.Add(new SelectOption() { name = date, value = date });
                    }

                }
                else if (r.reportingPeriodId == 4)
                {
                    // annual
                    DateTime Date = DateTimeLocal().AddMonths(-3);
                    r.reportPeriodEndDateOption = new List<SelectOption>();
                    while (Date.Month != 12)
                    {
                        Date = Date.AddMonths(1);
                    }
                    int lastDayOfMonth = DateTime.DaysInMonth(Date.Year, Date.Month);
                    var date = new DateTime(Date.Year, Date.Month, lastDayOfMonth).ToShortDateString();
                    r.reportPeriodEndDateOption.Add(new SelectOption() { name = date, value = date });
                    for (var i = 0; i < 1; i++)
                    {
                        Date = Date.AddMonths(12);
                        lastDayOfMonth = DateTime.DaysInMonth(Date.Year, Date.Month);
                        date = new DateTime(Date.Year, Date.Month, lastDayOfMonth).ToShortDateString();
                        r.reportPeriodEndDateOption.Add(new SelectOption() { name = date, value = date });
                    }


                }





            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }

            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalSetNextProgressReportDate", r);

        }


        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> UpdateNextProgressReportDate(ClientService n)
        {
            ClientPageData r = new ClientPageData();

            if (n.nextReportDueDate != null)
            {
                if (Convert.ToDateTime(n.nextReportDueDate) < DateTime.Now.AddMonths(-4))
                {
                    r.er.code = 1;
                    r.er.msg = "Cannot backdate end date more than 4 months!";
                }
            }

            if (r.er.code == 0)
            {

                DataSet ds = new DataSet();
                try
                {
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_ClientUpdateServiceReportDateNew", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                            cmd.Parameters.AddWithValue("@clsvId", n.clsvId);
                            cmd.Parameters.AddWithValue("@clsvidId", n.clsvidId);
                            cmd.Parameters.AddWithValue("@nextReportDueDate", n.nextReportDueDate != null ? (object)n.nextReportDueDate : DBNull.Value);
                            cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    });
                    r.services = setClientServices(ref ds, 0);
                    setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);

                }
                catch (Exception ex)
                {
                    r.er.code = 1;
                    r.er.msg = ex.Message;
                }
            }
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Services", r);


        }


        [AJAXAuthorize]
        public async Task<ActionResult> OpenEditNextVistDateModal(int clientServiceId)
        {
            Er er = new Er();
            ClientService r = new ClientService();

            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientGetService", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@clsvidId", clientServiceId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                DataRow dr = ds.Tables[0].Rows[0];
                r.clsvidId = clientServiceId;
                r.nextATCMonitoringVisit = dr["nextATCMonitoringVisit"] == DBNull.Value ? "" : ((DateTime)dr["nextATCMonitoringVisit"]).ToString("yyyy-MM-dd");



            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }

            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalSetNextVisitDate", r);

        }


        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> UpdateNextVisitDate(ClientService n)
        {
            ClientPageData r = new ClientPageData();
            if (n.nextATCMonitoringVisit != null)
            {
                if (Convert.ToDateTime(n.nextATCMonitoringVisit) < DateTime.Now.AddMonths(-1))
                {
                    r.er.code = 1;
                    r.er.msg = "Cannot backdate end date more than one month!";
                }
            }
            if (r.er.code == 0)
            {
                DataSet ds = new DataSet();
                try
                {
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_ClientUpdateServiceVisitDateNew", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                            cmd.Parameters.AddWithValue("@clsvId", n.clsvId);
                            cmd.Parameters.AddWithValue("@clsvidId", n.clsvidId);
                            cmd.Parameters.AddWithValue("@nextATCMonitoringVisit", n.nextATCMonitoringVisit != null ? (object)n.nextATCMonitoringVisit : DBNull.Value);
                            cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    });
                    r.services = setClientServices(ref ds, 0);
                    setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);

                }
                catch (Exception ex)
                {
                    r.er.code = 1;
                    r.er.msg = ex.Message;
                }


            }




            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Services", r);


        }

        [HttpGet]
        [AJAXAuthorize]
        public ActionResult OpenAddSpecialRateModal(SpecialRate r)
        {
            return PartialView("ModalAddSpecialRate", r);
        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenEditSpecialRateModal(int spRtId)
        {
            SpecialRate r = new SpecialRate();
            Er er = new Er();
            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsGetSpecialRate", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@spRtId", spRtId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                DataRow dr = ds.Tables[0].Rows[0];
                r.spRtId = (int)dr["spRtId"];
                r.clsvidId = (int)dr["clsvidId"];
                r.rate = (decimal)dr["rate"];
                r.ratio = (decimal)dr["rate"];
                r.svcName = (string)dr["svc"];
                r.fn = (string)dr["fn"];
                r.ln = (string)dr["ln"];
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }

            ds.Dispose();


            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalEditSpecialRate", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SetSpecialRate(SpecialRate sr)
        {
            ClientPageData r = new ClientPageData();
            r.userLevel = UserClaim.userLevel;
            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsAddSpecialRateNew", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clsvId", sr.clsvId);
                        cmd.Parameters.AddWithValue("@clsvidId", sr.clsvidId);
                        cmd.Parameters.AddWithValue("@spRtId", sr.spRtId);
                        cmd.Parameters.AddWithValue("@ratio", sr.ratio);
                        cmd.Parameters.AddWithValue("@rate", sr.rate);
                        cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.services = setClientServices(ref ds, 0);
                setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            ds.Dispose();
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Services", r);
        }

        [HttpGet]
        [AJAXAuthorize]
        public ActionResult OpenDeleteSpecialRateModal(SpecialRate r)
        {
            return PartialView("ModalDeleteSpecialRate", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> DeleteSpecialRate(SpecialRate sr)
        {
            ClientPageData r = new ClientPageData();
            r.userLevel = UserClaim.userLevel;
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsDeleteSpecialRateNew", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@spRtId", sr.spRtId);
                        cmd.Parameters.AddWithValue("@clsvId", sr.clsvId);
                        cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            if (r.er.code == 0)
            {
                r.services = setClientServices(ref ds, 0);
                setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);
            }
            ds.Dispose();
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Services", r);
        }


        [AJAXAuthorize]
        public ActionResult GetNewCommentModal(ClientProfile r)
        {
            return PartialView("ModalAddComment", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> AddComment(ClientComment c)
        {
            ClientPageData r = new ClientPageData();
            r.userLevel = UserClaim.userLevel;
            DataSet ds = new DataSet();

            try
            {

                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsAddComment", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@clsvId", c.clsvId);
                        cmd.Parameters.AddWithValue("@comment", c.comment);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.commentHistory = setCommentHistory(ref ds, 0);

            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }


            ds.Dispose();
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Comments", r);
        }

        [HttpGet]
        public JsonResult IsPolicyResequenceRequired(int tierId, int clientId)
        {
            return Json(PolicyResequenceRequired(clientId, tierId), JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenEditGeoLocationModal(int clientLocationId)
        {
            Er er = new Er();
            GeoLocation r = new GeoLocation();

            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientGetGeoLocById ", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@clientLocationid", clientLocationId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                DataRow dr = ds.Tables[0].Rows[0];

                r.locationTypeId = (short)dr["billinglocationTypeId"];
                r.ad1 = (string)dr["ad1"];
                r.ad2 = dr["ad2"] == DBNull.Value ? "" : (string)dr["ad2"];
                r.cty = (string)dr["cty"];
                r.st = (string)dr["st"];
                r.zip = (string)dr["zip"];
                r.landline = dr["landline"] == DBNull.Value ? "" : (string)dr["landline"];
    }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }

            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalEditGeoLocation", r);


        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenAddGeoLocationModal()
        {
            Er er = new Er();
            BillingLocations r = new BillingLocations();

            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsGetBillingLocationInfo ", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                r.billingLocationTypes = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {
                    value = Convert.ToString(spR["billingLocationTypeId"]),
                    name = (string)spR["billingLocationType"]
                }).ToList();


                r.billingLocations = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {
                    value = spR["billingLocationTypeId"] + "-" + spR["locationId"] + "-" + spR["clLocId"],
                    name = (string)spR["name"]
                }).ToList();


            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }

            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalAddGeoLocation", r);


        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> UpdateGeoLocation(GeoLocation g)
        {
            ClientPageData r = new ClientPageData();

            if (g.st == "AZ" && (g.locationTypeId == 1 || g.locationTypeId == 2))
            {
                DataTable MatchingZips = new DataTable();
                try
                {

                    await Task.Run(() =>
                    {
                        if (g.ad2 == null)
                            g.ad2 = "";
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_CheckZipCodeForAZ", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@zip", g.zip.Substring(0, 5));

                            sqlHelper.ExecuteSqlDataAdapter(cmd, MatchingZips);
                        }
                    });
                }
                catch (Exception ex)
                {
                    r.er.code = 1;
                    r.er.msg = ex.Message;
                }
                if (MatchingZips.Rows.Count == 0)
                {
                    r.er.code = 1;
                    r.er.msg = "The first 5 digits of the AZ zip code could not be found. If the zip code is correct please put in a request to add it to the database";

                }
                MatchingZips.Dispose();
            }

            if (r.er.code == 0)
            {
                r.userLevel = UserClaim.userLevel;
                GeoWebResult geoWebResult = new GeoWebResult();

                if (g.locationTypeId == 1 || g.locationTypeId == 2)
                {
                    GeoLocator geoLocator = new GeoLocator(g.ad1, g.ad2, g.cty, g.st, g.zip);
                    geoWebResult = await geoLocator.GetGeoLocation();
                }

                if (geoWebResult.er.code == 0)
                {
                    g.lat = geoWebResult.lat;
                    g.lon = geoWebResult.lon;
                    g.locationType = geoWebResult.locationType;
                    g.radius = geoWebResult.radius;
                    DataSet ds = new DataSet();
                    try
                    {

                        await Task.Run(() =>
                        {
                            if (g.ad2 == null)
                                g.ad2 = "";
                            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                            {
                                SqlCommand cmd = new SqlCommand("sp_ClientsAddGeoLoc", cn)
                                {
                                    CommandType = CommandType.StoredProcedure
                                };
                                cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                                cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                                cmd.Parameters.AddWithValue("@billingLocationTypeId", g.locationTypeId);
                                cmd.Parameters.AddWithValue("@locationId", g.locationId);

                                cmd.Parameters.AddWithValue("@clsvId", g.clsvId);
                                cmd.Parameters.AddWithValue("@clLocId", g.clLocId);
                                cmd.Parameters.AddWithValue("@name", (object)g.name);
                                cmd.Parameters.AddWithValue("@ad1", (object)g.ad1);
                                cmd.Parameters.AddWithValue("@ad2", (object)g.ad2);
                                cmd.Parameters.AddWithValue("@cty", (object)g.cty);
                                cmd.Parameters.AddWithValue("@st", (object)g.st);
                                cmd.Parameters.AddWithValue("@zip", g.zip);
                                cmd.Parameters.AddWithValue("@lat", g.lat);
                                cmd.Parameters.AddWithValue("@lon", g.lon);
                                cmd.Parameters.AddWithValue("@locationType", g.locationType);
                                cmd.Parameters.AddWithValue("@radius", g.radius);
                                cmd.Parameters.AddWithValue("@landline", g.landline);
                                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                            }
                        });
                        r.geoLocations = setGeoLocations(ref ds, 0);
                    }
                    catch (Exception ex)
                    {
                        r.er.code = 1;
                        r.er.msg = ex.Message;
                    }

                    ds.Dispose();

                }
                else
                {
                    r.er.code = 1;
                    r.er.msg = "Geo Location Not Found";
                }
            }

            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Locations", r);

        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenRemoveGeoLocation(GeoLocation r)
        {
            Er er = new Er();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetGeoLocation", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@clLocId", r.clLocId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                DataRow dr = ds.Tables[0].Rows[0];
                r.name = dr["name"] == DBNull.Value ? "" : (string)dr["name"];
                r.ad1 = (string)dr["ad1"];
                r.cty = (string)dr["cty"];
                r.st = (string)dr["st"];
                r.zip = (string)dr["zip"];
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalDeleteGeoLocation", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> RemoveGeoLocation(GeoLocation g)
        {
            ClientPageData r = new ClientPageData();
            r.userLevel = UserClaim.userLevel;
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsDeleteGeoLoc", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clsvId", g.clsvId);
                        cmd.Parameters.AddWithValue("@clLocId", g.clLocId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.geoLocations = setGeoLocations(ref ds, 0);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }

            ds.Dispose();

            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Locations", r);
        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenChartUploadModal(int clsvId)
        {

            Er er = new Er();
            UploadChartModal r = new UploadChartModal();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetChartModalInfo", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@clsvId", clsvId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });



              
                r.serviceOptions = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {
                    value = Convert.ToString(spR["serviceId"]),
                    name = (string)spR["svcLong"]

                }).ToList();
                r.chartDocTypes = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {
                    value = Convert.ToString(spR["docTypeId"]),
                    name = (string)spR["docType"]

                }).ToList();

            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalUploadChart", r);

        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> UploadChart(IEnumerable<HttpPostedFileBase> files, int clsvId, int docTypeId, int serviceId, string startDate, string endDate)
        {
            ClientPageData r = new ClientPageData();
            ClientPageWindows w = new ClientPageWindows();

            r.userLevel = UserClaim.userLevel;
            if (files != null)
            {
                foreach (var file in files)
                {
                    // Verify that the user selected a file
                    if (file != null && file.ContentLength > 0)
                    {
                        // extract only the fielname
                        var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                        var fileExtension = Path.GetExtension(file.FileName);
                        var contentType = file.ContentType;


                        if (w.er.code == 0)
                        {
                            DataSet ds = new DataSet();

                            try
                            {
                                await Task.Run(() =>
                                {
                                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                                    {
                                        SqlCommand cmd = new SqlCommand("sp_ClientsAddChartNew", cn)
                                        {
                                            CommandType = CommandType.StoredProcedure
                                        };
                                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                                        cmd.Parameters.AddWithValue("@clsvId", clsvId);
                                        cmd.Parameters.AddWithValue("@fileName", fileName);
                                        cmd.Parameters.AddWithValue("@fileExtension", fileExtension);
                                        cmd.Parameters.AddWithValue("@contentType", contentType);
                                        cmd.Parameters.AddWithValue("@docTypeId", docTypeId);
                                        cmd.Parameters.AddWithValue("@serviceId", serviceId);
                                        cmd.Parameters.AddWithValue("@startDate", startDate == "" ? DBNull.Value : (object)startDate);
                                        cmd.Parameters.AddWithValue("@endDate", endDate == "" ? DBNull.Value : (object)endDate);
                                        cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                w.er.code = 1;
                                w.er.msg = ex.Message;
                            }

                            if (w.er.code == 0)
                            {
                                if (ds.Tables[0].Rows.Count != 0)
                                {
                                    /* client charts */
                                    DataRow dr = ds.Tables[0].Rows[0];
                                    int chartId = (int)dr["chartId"];

                                    r.charts = setClientCharts(ref ds, 0);

                                    r.services = setClientServices(ref ds, 1);
                                    setClientServiceDetails(ref ds, 2, 3, 4, 5, ref r);
                                    w.clientServices = RenderRazorViewToString("Client_Services", r);
                                    w.clientCharts = RenderRazorViewToString("Client_Charts", r);
                                    /*
                                    r.services = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new ClientService()
                                    {
                                        serviceId = (int)spR["serviceId"],
                                        svcLong = (string)spR["svcLong"]
                                    }).ToList();
                                    */
                                    try
                                    {
                                        byte[] data = new byte[file.InputStream.Length];
                                        file.InputStream.Read(data, 0, data.Length);
                                        FileData f = new FileData("charts", UserClaim.blobStorage);
                                        f.StoreFile(data, chartId + fileExtension);
                                    }
                                    catch (Exception ex)
                                    {
                                        w.er.code = 1;
                                        w.er.msg = ex.Message;
                                    }

                                    if (w.er.code != 0)
                                    {

                                        // delete file

                                        await Task.Run(() =>
                                        {
                                            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                                            {
                                                SqlCommand cmd = new SqlCommand("sp_ClientsDeleteChartNew", cn)
                                                {
                                                    CommandType = CommandType.StoredProcedure
                                                };
                                                cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                                                cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                                                cmd.Parameters.AddWithValue("@clsvId", clsvId);
                                                cmd.Parameters.AddWithValue("@chartId", chartId);
                                                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                                            }
                                        });

                                    }
                                }
                            }
                        }
                    }
                }
            }
            return Json(w);


        }

        [HttpGet]
        [AJAXAuthorize]
        public ActionResult openDeleteChartItemModal(ChartDelete r)
        {
            return PartialView("ModalDeleteChart", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> DeleteChartItem(ChartDelete s)
        {
            ClientPageData r = new ClientPageData();
            r.userLevel = UserClaim.userLevel;
            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsDeleteChartNew", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@clsvId", s.clsvId);
                        cmd.Parameters.AddWithValue("@chartId", s.chartId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        return ds;
                    }
                });
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            if (r.er.code == 0)
            {
                if (ds.Tables[0].Rows.Count != 0)
                {
                    try
                    {
                        FileData f = new FileData("charts", UserClaim.blobStorage);
                        f.DeleteFile(s.chartId + ds.Tables[0].Rows[0].ItemArray[0].ToString());
                        r.charts = setClientCharts(ref ds, 1);
                    }
                    catch (Exception ex)
                    {
                        r.er.code = 1;
                        r.er.msg = ex.Message;

                    }
                }
            }

            ds.Dispose();
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Charts", r);
        }

        [AJAXAuthorize]
        public async Task<ActionResult> GetChart(string id)
        {
            Er er = new Er();
            string commandStr = "SELECT fileName,extension FROM ClientCharts WHERE chartId=" + id + ";";
            DataSet ds = null;
            try
            {
                ds = await SqlGetData(commandStr);
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }
            if (er.code == 0)
            {
                if (ds.Tables[0].Rows.Count != 0)
                {
                    string fileName = ds.Tables[0].Rows[0].ItemArray[0].ToString() + ds.Tables[0].Rows[0].ItemArray[1].ToString();
                    string filePath = id + ds.Tables[0].Rows[0].ItemArray[1].ToString();
                    FileData f = new FileData("charts", UserClaim.blobStorage);

                    byte[] data = f.GetFile(filePath);
                    Response.AddHeader("Content-Disposition", "attachment;filename=\"" +fileName + "\"");

                    return new FileContentResult(data, ds.Tables[0].Rows[0].ItemArray[1].ToString());
                }
            }

            Response.Write(er.msg);
            Response.StatusCode = 400;
            return null;


        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> GetProviderOptions(int serviceId)
        {
            ProviderOptions r = new ProviderOptions();
            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                 {
                     using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                     {
                         SqlCommand cmd = new SqlCommand("sp_ClientsGetProviderOptions", cn)
                         {
                             CommandType = CommandType.StoredProcedure
                         };
                         cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                         cmd.Parameters.AddWithValue("@serviceId", serviceId);
                         sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                     }
                 });
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            if (r.er.code == 0)
            {
                DataRow dr = ds.Tables[0].Rows[0];
                r.requiresATCRelationship = (bool)dr["requiresATCRelationship"];
                r.providers = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new Option()
                {
                    value = Convert.ToString(spR["prId"]),
                    name = spR["ln"] + " " + spR["fn"]

                }).ToList();

            }

            ds.Dispose();

            return Json(r, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenDeleteClientStaffRelationshipModal(int relId)
        {
            Er er = new Er();
            ProviderService r = new ProviderService();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetClientStaffRelationship", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@relId", relId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                DataRow dr = ds.Tables[0].Rows[0];
                r.providerName = dr["pfn"] + " " + dr["pLn"];
                r.service = (string)dr["svc"];
                r.relId = relId;

            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }
            ds.Dispose();
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalDeleteClientStaffRelationship", r);

        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> DeleteClientStaffRelationship(RelId c)
        {
            ClientPageData r = new ClientPageData();
            r.userLevel = UserClaim.userLevel;
            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsDeleteClientStaffRelationship", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clsvId", c.clsvId);
                        cmd.Parameters.AddWithValue("@relId", c.relId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.providerServices = setProviderServices(ref ds, 0);
                r.services = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new ClientService()
                {
                    clsvidId = (int)spR["clsvidid"],
                    svc = (string)spR["svc"],
                    svcLong = (string)spR["svcLong"],
                }).ToList();
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }

            ds.Dispose();
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Providers", r);

        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenAddClientStaffRelationshipModal(int clsvId)
        {
            Er er = new Er();
            ClientRelationshipModal r = new ClientRelationshipModal();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetClientStaffRelationshipData", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@clsvId", clsvId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.atcRelationships = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {
                    value = Convert.ToString(spR["atcRelId"]),
                    name = (string)spR["atcRelationship"]
                }).ToList();
                r.services = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {
                    value = spR["serviceId"] + "-" + spR["clsvidId"],
                    name = (string)spR["name"]
                }).ToList();



            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }
            ds.Dispose();
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalAddClientStaffRelationship", r);

        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SetClientStaffRelationship(ClientStaffRelationship c)
        {
            ClientPageData r = new ClientPageData();
            r.userLevel = UserClaim.userLevel;
            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsSetClientStaffRelationship", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clsvId", c.clsvId);
                        cmd.Parameters.AddWithValue("@clsvidId", c.clsvidId);
                        cmd.Parameters.AddWithValue("@prId", c.prId);
                        cmd.Parameters.AddWithValue("@atcRelId", c.atcRelId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.providerServices = setProviderServices(ref ds, 0);
                r.services = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new ClientService()
                {
                    clsvidId = (int)spR["clsvidid"],
                    svc = (string)spR["svc"],
                    svcLong = (string)spR["svcLong"],
                }).ToList();
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }

            ds.Dispose();
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Providers", r);

        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenAddCareAreaModal(int clsvId)
        {
            ServicesRequiringCareAreas r = new ServicesRequiringCareAreas();
            Er er = new Er();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsGetCareAreaServices", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@clsvId", clsvId); ;
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                if (ds.Tables[0].Rows.Count != 0)
                {
                    r.services = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                    {
                        value = spR["serviceId"] + "-" + spR["clsvidId"],
                        name = (string)spR["svcLong"]

                    }).ToList();
                }
                else
                {
                    er.code = 1;
                    er.msg = "This client has no services requiring care areas";
                }



            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }

            ds.Dispose();
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalAddCareArea", r);
        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> GetCareAreas(int id)
        {
           
            Er er = new Er();
            List<SelectOption> serviceTaskList = null;
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsGetCareForService", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@serviceId", id); ;
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                if (ds.Tables[0].Rows.Count != 0)
                {
                    serviceTaskList = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                    {
                        value = Convert.ToString(spR["careAreaId"]),
                        name = (string)spR["careArea"]

                    }).ToList();
                }
                else
                {
                    er.code = 1;
                    er.msg = "This service has no predined tasks";
                }
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }

            ds.Dispose();
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return Json(serviceTaskList, JsonRequestBehavior.AllowGet);
        }



        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> AddCareArea(CareArea c)
        {
            ClientPageData r = new ClientPageData();

            r.userLevel = UserClaim.userLevel;
            r.hasCareAreas = true;
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsAddCareArea", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@clsvId", c.clsvId);
                        cmd.Parameters.AddWithValue("@clsvidId", c.clsvidId);
                        cmd.Parameters.AddWithValue("@serviceId", c.serviceId);
                        cmd.Parameters.AddWithValue("@careArea", c.careArea);
                        cmd.Parameters.AddWithValue("@careAreaId", c.careAreaId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.careAreas = setCareAreas(ref ds, 0);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            ds.Dispose();
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_CareAreas", r);
        }

        [HttpGet]
        [Authorize]
        public ActionResult OpenDeleteCareAreaModal(CareArea r)
        {

            return PartialView("ModalDeleteCareArea", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> DeleteCareArea(CareArea c)
        {
            ClientPageData r = new ClientPageData();
            r.userLevel = UserClaim.userLevel;
            r.hasCareAreas = true;
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsDeleteCareArea", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clsvId", c.clsvId);
                        cmd.Parameters.AddWithValue("@careId", c.careId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.careAreas = setCareAreas(ref ds, 0);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            ds.Dispose();
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_CareAreas", r);
        }


        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenEditObjectivesModal(int clientId, int serviceId, int isTherapy)
        {
            ObjectivesModal r = new ObjectivesModal();

            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetServiceObjectives", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@clsvId", clientId);
                        cmd.Parameters.AddWithValue("@serviceId", serviceId);
                        cmd.Parameters.AddWithValue("@isTherapy", isTherapy);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                DataRow dr = ds.Tables[0].Rows[0];
                r.serviceObjective = new ServiceObjective();
                r.serviceObjective.serviceId = (int)dr["serviceId"];
                r.serviceObjective.clsvidId = (int)dr["clsvidId"];
                r.serviceObjective.svcName = (string)dr["svcName"];
                r.serviceObjective.isTherapy = (bool)dr["isTherapy"];

                DataView dv = new DataView(ds.Tables[0]);


                dv.RowFilter = "serviceId=" + r.serviceObjective.serviceId + " AND objectiveId IS NOT NULL";
                r.serviceObjective.longTermObjectives = dv.ToTable(true, "objectiveId", "goalAreaId", "goalArea", "longTermVision", "longTermGoal", "objectiveStatus", "completedDt").Rows.Cast<DataRow>().Select(spR => new LongTermObjective()
                {
                    objectiveId = (int)spR["objectiveId"],
                    goalAreaId = (int)spR["goalAreaId"],
                    goalAreaName = (string)spR["goalArea"],
                    longTermVision = (string)spR["longTermVision"],
                    longTermGoal = (string)spR["longTermGoal"],
                    objectiveStatus = (string)spR["objectiveStatus"],
                    completedDt = spR["completedDt"] == DBNull.Value ? "" : ((DateTime)spR["completedDt"]).ToString("yyyy-MM-dd")

                }).ToList();
                foreach (LongTermObjective longTermObjective in r.serviceObjective.longTermObjectives)
                {
                    dv.RowFilter = "serviceId=" + r.serviceObjective.serviceId + " AND objectiveId=" + longTermObjective.objectiveId + " AND objectiveId IS NOT NULL" + " AND goalId IS NOT NULL";
                    longTermObjective.shortTermGoals = dv.ToTable(true, "goalId", "shortTermGoal", "teachingMethod", "goalStatus", "frequency", "frequencyId", "goalCompletedDt").Rows.Cast<DataRow>().Select(spR => new ShortTermGoal()
                    {
                        goalId = (int)spR["goalId"],
                        shortTermGoal = (string)spR["shortTermGoal"],
                        teachingMethod = (string)spR["teachingMethod"],
                        goalStatus = (string)spR["goalStatus"],
                        frequency = (string)spR["frequency"],
                        frequencyId = spR["frequencyId"].ToString(),
                        completedDt = spR["goalCompletedDt"] == DBNull.Value ? "" : ((DateTime)spR["goalCompletedDt"]).ToString("yyyy-MM-dd")
                    }).ToList();
                }

                r.goalAreas = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {
                    value = spR["goalAreaId"].ToString(),
                    name = (string)spR["name"],
                }).ToList();

                r.frequencies = ds.Tables[2].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {
                    value = spR["frequencyId"].ToString(),
                    name = (string)spR["name"],
                }).ToList();

                r.statuses = ds.Tables[3].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {
                    value = spR["name"].ToString(),
                    name = (string)spR["name"],
                }).ToList();

            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            ds.Dispose();
            if (r.goalAreas.Count == 0)
            {
                r.er.msg = "Can not create Client Objectives if there are no goal areas for the service";
                r.er.code = 1;
            }

            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else


                return PartialView("ModalEditObjectivesGoals", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> AddLongTermVision2(ServiceObjective r)
        {

            LongTermObjective l = r.longTermObjectives[0];
            l.objectiveStatus = "Active";
            DataSet ds = new DataSet();
            try
            {

                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsAddTherapyLongTermObjective", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@serviceId", r.serviceId);
                        cmd.Parameters.AddWithValue("@isTherapy", r.isTherapy);

                        cmd.Parameters.AddWithValue("@clsvId", r.clientId);
                        cmd.Parameters.AddWithValue("@goalAreaId", l.goalAreaId);
                        cmd.Parameters.AddWithValue("@longTermVision", l.longTermVision == null ? "" : l.longTermVision);
                        cmd.Parameters.AddWithValue("@longTermGoal", l.longTermGoal == null ? "" : l.longTermGoal);
                        cmd.Parameters.AddWithValue("@objectiveStatus", l.objectiveStatus);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                DataRow dr = ds.Tables[0].Rows[0];
                l.objectiveId = (int)dr["objectiveId"];


            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }




            return Json(r);
        }


        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> AddActionStep2(ServiceObjective r)
        {

            ShortTermGoal g = r.longTermObjectives[0].shortTermGoals[0];
            g.goalStatus = "Active";
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsAddTherapyShortTermGoal", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@isTherapy", r.isTherapy);
                        cmd.Parameters.AddWithValue("@objectiveId", r.longTermObjectives[0].objectiveId);
                        cmd.Parameters.AddWithValue("@shortTermGoal", g.shortTermGoal == null ? "" : g.shortTermGoal);
                        cmd.Parameters.AddWithValue("@teachingMethod", g.teachingMethod == null ? "" : g.teachingMethod);
                        cmd.Parameters.AddWithValue("@goalStatus", g.goalStatus);
                        cmd.Parameters.AddWithValue("@frequencyId", g.frequencyId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                DataRow dr = ds.Tables[0].Rows[0];
                g.goalId = (int)dr["goalId"];
                g.step = (int)dr["step"];

            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            return Json(r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SaveObjectives2(ServiceObjective so)
        {

            ClientPageData r = new ClientPageData();

            DataTable objectives = new DataTable();
            objectives.Clear();
            objectives.Columns.Add("objectiveId");
            objectives.Columns.Add("goalAreaId");
            objectives.Columns.Add("longTermVision");
            objectives.Columns.Add("longTermGoal");
            objectives.Columns.Add("objectiveStatus");
            objectives.Columns.Add("completedDt");
            objectives.Columns.Add("changes");

            DataTable shortTermGoals = new DataTable();
            shortTermGoals.Clear();
            shortTermGoals.Columns.Add("goalId");
            shortTermGoals.Columns.Add("shortTermGoal");
            shortTermGoals.Columns.Add("teachingMethod");
            shortTermGoals.Columns.Add("goalStatus");
            shortTermGoals.Columns.Add("completedDt");
            shortTermGoals.Columns.Add("frequencyId");
            shortTermGoals.Columns.Add("progress");
            shortTermGoals.Columns.Add("recommendation");

            foreach (LongTermObjective l in so.longTermObjectives)
            {
                DataRow nRow1 = objectives.NewRow();
                nRow1["objectiveId"] = l.objectiveId;
                nRow1["goalAreaId"] = l.goalAreaId;
                nRow1["longTermVision"] = l.longTermVision == null ? "" : l.longTermVision;
                nRow1["longTermGoal"] = l.longTermGoal == null ? "" : l.longTermGoal;
                nRow1["objectiveStatus"] = l.objectiveStatus;
            //    if (l.objectiveStatus == "Completed")
                    nRow1["completedDt"] = l.completedDt;
                objectives.Rows.Add(nRow1);


                if (l.shortTermGoals != null)
                {
                    foreach (ShortTermGoal s in l.shortTermGoals)
                    {
                        DataRow nRow2 = shortTermGoals.NewRow();
                        nRow2["goalId"] = s.goalId;
                        nRow2["shortTermGoal"] = s.shortTermGoal == null ? "" : s.shortTermGoal;
                        nRow2["teachingMethod"] = s.teachingMethod == null ? "" : s.teachingMethod;
                        nRow2["goalStatus"] = s.goalStatus;
                        //           if (s.goalStatus == "Completed")
                        nRow2["completedDt"] = s.completedDt;

                        nRow2["frequencyId"] = s.frequencyId;

                        shortTermGoals.Rows.Add(nRow2);
                    }

                }

            }
            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientsSetObjectives", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clientId", so.clientId);
                        cmd.Parameters.AddWithValue("@isTherapy", so.isTherapy);
                        cmd.Parameters.AddWithValue("@shortTermGoals", shortTermGoals);
                        cmd.Parameters.AddWithValue("@objectives", objectives);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.serviceObjectives = setObjectives(ref ds, 0);


            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            ds.Dispose();

            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Objectives", r);

        }


        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenPlanOfCareModal(int clsvidId)
        {
            Er er = new Er();
            PlanOfCareData r = new PlanOfCareData();

            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetTherapistForService", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@clientServiceId", clsvidId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                if (ds.Tables[0].Rows.Count == 0)
                {
                    er.code = 1;
                    er.msg = "No therapist supervisors have been assigned to this client";
                }
                else
                {
                    r.Options = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                    {
                        value = spR["prId"].ToString(),
                        name = spR["fn"] + " " + spR["ln"]

                    }).ToList();


                    DataRow dr = ds.Tables[1].Rows[0];
                    r.evaluationId = (int)dr["evaluationId"];
                    r.evaluationServiceId = (int)dr["evaluationServiceId"];

                    r.frequencies = ds.Tables[2].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                    {
                        value = spR["frequencyId"].ToString(),
                        name = (string)spR["name"]
                    }).ToList();

                }

            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }
            ds.Dispose();
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("ModalCreateplanOfCare", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> CreatePlanOfCare(PlanOfCareData r)
        {
            Er er = new Er();

            try
            {
                await Task.Run(() =>
                {

                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        cn.Open();
                        SqlCommand cmd = new SqlCommand("sp_TaskCreatePlanOfCareRenewal", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clientId", r.clientId);
                        cmd.Parameters.AddWithValue("@providerId", r.providerId);
                        cmd.Parameters.AddWithValue("@evaluationId", r.evaluationId);
                        cmd.Parameters.AddWithValue("@evaluationServiceId", r.evaluationServiceId);

                        cmd.Parameters.AddWithValue("@StartDate", r.treatmentStart);
                        cmd.Parameters.AddWithValue("EndDate", r.treatmentEnd);
                        cmd.Parameters.AddWithValue("@treatmentFrequencyId", r.frequencyId);
                        cmd.Parameters.AddWithValue("@treatmentDurationId", r.treatmentDurationId);
                        cmd.Parameters.AddWithValue("@numberOfVisits", r.numberOfVisits);

                        cmd.Parameters.AddWithValue("@date", DateTimeLocal());


                        cmd.ExecuteNonQuery();
                        cn.Close();
                    }
                });


            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }

            return Json(er);
        }

        private void setClientProfile(ref ClientPageData r, ref DataSet ds, bool forEdit)
        {

            DataRow dr1 = ds.Tables[0].Rows[0];
            r.clientProfile = new ClientProfile();
            r.clientProfile.clsvId = (int)dr1["clsvID"];
            r.clientProfile.clId = (string)dr1["clID"];
            r.clientProfile.medicaidId = dr1["medicaidId"] == DBNull.Value ? "" : (string)dr1["medicaidId"];

            r.clientProfile.fn = (string)dr1["fn"];
            r.clientProfile.ln = (string)dr1["ln"];
            r.clientProfile.name = (string)dr1["fn"] + " " + (string)dr1["ln"];
            r.clientProfile.deleted = (bool)dr1["deleted"];
            r.clientProfile.clwNm = dr1["clwNm"] == DBNull.Value ? "" : (string)dr1["clwNm"];
            r.clientProfile.clwPh = dr1["clwPh"] == DBNull.Value ? "" : (string)dr1["clwPh"];
            r.clientProfile.clwEm = dr1["clwEm"] == DBNull.Value ? "" : (string)dr1["clwEm"];
            r.clientProfile.sex = dr1["sex"] == DBNull.Value ? "" : (string)dr1["sex"];
            r.clientProfile.dob = dr1["dob"] == DBNull.Value ? "" : ((DateTime)dr1["dob"]).ToShortDateString();
            r.clientProfile.dobISO = dr1["dob"] == DBNull.Value ? "" : ((DateTime)dr1["dob"]).ToString("yyyy-MM-dd");

            r.clientProfile.physician = dr1["physician"].ToString();
            r.clientProfile.physicianTitle = dr1["physicianTitle"].ToString();
            r.clientProfile.physicianFirstName = dr1["physicianFirstName"].ToString();
            r.clientProfile.physicianMI = dr1["physicianMI"].ToString();
            r.clientProfile.physicianLastName = dr1["physicianLastName"].ToString();
            r.clientProfile.physicianSuffix = dr1["physicianSuffix"].ToString();
            r.clientProfile.physicianAgency = dr1["physicianAgency"].ToString();
            r.clientProfile.physicianPhone = dr1["physicianTelephone"].ToString();
            r.clientProfile.physicianAddress = dr1["physicianAddress"].ToString();
            r.clientProfile.physicianCity = dr1["physicianCity"].ToString();
            r.clientProfile.physicianState = dr1["physicianState"].ToString();
            r.clientProfile.physicianZip = dr1["physicianZip"].ToString();

            r.clientProfile.physicianEmail = dr1["physicianEmail"].ToString();
            r.clientProfile.physicianNPI = dr1["physicianNPI"].ToString();
            r.clientProfile.physicianFax = dr1["physicianFax"].ToString();
            r.clientProfile.selfResponsible = (bool)dr1["selfResponsible"];
            r.clientProfile.responsiblePersonFn = dr1["responsiblePersonFn"].ToString();
            r.clientProfile.responsiblePersonLn = dr1["responsiblePersonLn"].ToString();
            r.clientProfile.responsiblePersonAddress = dr1["responsiblePersonAddress"].ToString();
            r.clientProfile.responsiblePersonAddress2 = dr1["responsiblePersonAddress2"].ToString();
            r.clientProfile.responsiblePersonCity = dr1["responsiblePersonCity"].ToString();
            r.clientProfile.responsiblePersonState = dr1["responsiblePersonState"].ToString();
            r.clientProfile.responsiblePersonZip = dr1["responsiblePersonZip"].ToString();
            r.clientProfile.responsiblePersonPhone = dr1["responsiblePersonTelephone"].ToString();
            r.clientProfile.responsiblePersonEmail = dr1["responsiblePersonEmail"].ToString();
            r.clientProfile.responsiblePersonRelationship = dr1["relationship"] == DBNull.Value ? "" : dr1["relationship"].ToString();
            r.clientProfile.responsiblePersonRelationshipId = dr1["relationshipId"] == DBNull.Value ? 0 : (int)dr1["relationshipId"];
            
                
            // Edit has two tables
            if (forEdit)
            {
                r.clientProfile.relationshipOptions = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {
                   value = spR["relationshipId"].ToString(),
                   name = spR["relationship"].ToString()
                }).ToList();
            }


        }


        private List<ClientService> setClientServices(ref DataSet ds, int tableIdx)
        {
            List<ClientService> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new ClientService()
            {
                clsvidId = (int)spR["id"],
                serviceId = (int)spR["serviceId"],
                deleted = (bool)spR["deleted"],
                status = (bool)spR["deleted"] ? "Inactive" : "Active",
                svc = (string)spR["svc"],
                contingencyPlan = (string)spR["contingencyPlan"],
                svcLong = (string)spR["svcLong"],
                allowManualInOut = (bool)spR["allowManualInOut"],

                allowSpecialRates = (bool)spR["allowSpecialRates"],
                isTherapy = (bool)spR["isTherapy"],
                hasCareAreas = (bool)spR["hasCareAreas"],
                hasProgressReport = (bool)spR["hasProgressReport"],
                isEvaluation = (bool)spR["isEvaluation"],
                assignedRateName = spR["rateName"] == DBNull.Value ? "" : spR["rateName"] + " / $" + spR["rate"],
                assignedRateId = spR["assignedRateId"] == DBNull.Value ? 0 : (int)spR["assignedRateId"],
                billingType = spR["billingType"] == DBNull.Value ? 0 : (int)spR["billingType"],
                // mu = (decimal)spR["mu"],

                isHourly = (bool)spR["isHourly"],
                nextReportDueDate = spR["nextReportDueDate"] == DBNull.Value ? "Missing" : ((DateTime)spR["nextReportDueDate"]).ToShortDateString(),
                nextATCMonitoringVisit = spR["nextATCMonitoringVisit"] == DBNull.Value ? "Missing" : ((DateTime)spR["nextATCMonitoringVisit"]).ToShortDateString(),

                ISPStart = spR["ISPStart"] == DBNull.Value ? "Missing" : ((DateTime)spR["ISPStart"]).ToShortDateString(),
                ISPEnd = spR["ISPEnd"] == DBNull.Value ? "Missing" : ((DateTime)spR["ISPEnd"]).ToShortDateString(),
                POCStart = spR["POCStart"] == DBNull.Value ? "Missing" : ((DateTime)spR["POCStart"]).ToShortDateString(),
                POCEnd = spR["POCEnd"] == DBNull.Value ? "Missing" : ((DateTime)spR["POCEnd"]).ToShortDateString(),

                dddPay = (bool)spR["dddPay"],
                ppPay = (bool)spR["ppPay"],
                pInsPay = (bool)spR["pInsPay"]
            }).ToList();
            return r;
        }

        private void setClientServiceDetails(ref DataSet ds, int authTableIdx, int spRateTableIdx, int preAuthIdx, int rateTblIdx, ref ClientPageData r)
        {

            DataView dvAuths = new DataView(ds.Tables[authTableIdx]);
            DataView dvSpecialRates = new DataView(ds.Tables[spRateTableIdx]);
            DataView dvPreAuths = new DataView(ds.Tables[preAuthIdx]);
            DataView dvAssignableRates = new DataView(ds.Tables[rateTblIdx]);

            foreach (ClientService s in r.services)
            {
                dvAuths.RowFilter = "clsvidId=" + s.clsvidId;
                s.auths = dvAuths.ToTable().Rows.Cast<DataRow>().Select(spR => new Auth()
                {
                    auId = (int)spR["auId"],
                    clsvidId = (int)spR["clsvidId"],
                    stdt = ((DateTime)spR["stdt"]).ToShortDateString(),
                    eddt = ((DateTime)spR["eddt"]).ToShortDateString(),
                    au = (decimal)spR["au"],
                    tempAddedUnits = (decimal)spR["tempAddedUnits"],
                    uu = (decimal)spR["uu"],
                    ou = (decimal)spR["o"],
                    ru = ((decimal)spR["au"] + (decimal)spR["tempAddedUnits"]) - (decimal)spR["uu"] - (decimal)spR["o"],
                    wk = (decimal)spR["wk"],
                    weeklyHourOverride = (decimal)spR["weeklyHourOverride"]
                }).ToList();


                dvSpecialRates.RowFilter = "clsvidId=" + s.clsvidId;
                s.specialRates = dvSpecialRates.ToTable().Rows.Cast<DataRow>().Select(spR => new SpecialRate()
                {
                    spRtId = (int)spR["spRtId"],
                    clsvidId = (int)spR["clsvidId"],
                    ratio = (decimal)spR["ratio"],
                    rate = (decimal)spR["rate"]
                }).ToList();



                dvAssignableRates.RowFilter = "serviceId=" + s.serviceId;
                s.assignableRates = dvAssignableRates.ToTable().Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {
                    value = spR["rateId"].ToString(),
                    name = spR["rateName"] + " / $" + spR["rate"]
                }).ToList();

                dvPreAuths.RowFilter = "clientServiceId=" + s.clsvidId;
                s.insurancePreAuths = dvPreAuths.ToTable(true, "insuranceCompanyName", "insuranceCompanyId").Rows.Cast<DataRow>().Select(spR => new InsurancePreAuth()
                {
                    InsuranceCompanyId = (int)spR["insuranceCompanyId"],
                    InsuranceCompany = (string)spR["insuranceCompanyName"],
                }).ToList();

                foreach (var insurance in s.insurancePreAuths)
                {

                    dvPreAuths.RowFilter = "clientServiceId=" + s.clsvidId + " AND InsuranceCompanyId='" + insurance.InsuranceCompanyId + "'";
                    insurance.preAuths = dvPreAuths.ToTable().Rows.Cast<DataRow>().Select(spR => new PreAuth()
                    {
                        isApplicable = spR["paApplicable"] == DBNull.Value ? (bool?)null : (bool)spR["paApplicable"],
                        start = spR["paStart"] == DBNull.Value ? null : ((DateTime)spR["paStart"]).ToShortDateString(),
                        end = spR["paEnd"] == DBNull.Value ? null : ((DateTime)spR["paEnd"]).ToShortDateString(),
                        authUnits = (decimal)spR["authUnits"],
                        usedUnits = (decimal)spR["usedUnits"],
                        remUnits = (decimal)spR["authUnits"] - (decimal)spR["usedUnits"]

                    }).ToList();

                }

            }

            dvAuths.Dispose();
            dvSpecialRates.Dispose();
            dvPreAuths.Dispose();
            dvAssignableRates.Dispose();
        }

        private List<GeoLocation> setGeoLocations(ref DataSet ds, int tableIdx)
        {
            List<GeoLocation> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new GeoLocation()
            {
                clLocId = (int)spR["clLocId"],

                locationTypeId = (short)spR["billinglocationTypeId"],
                type = (string)spR["billinglocationType"],

                locationId = spR["locationId"] == DBNull.Value ? 0 : (int)spR["locationId"],
                name = spR["name"] == DBNull.Value ? "" : (string)spR["name"],

                ad1 = (string)spR["ad1"],
                ad2 = spR["ad2"] == DBNull.Value ? "" : (string)spR["ad2"],
                cty = (string)spR["cty"],
                st = (string)spR["st"],
                zip = (string)spR["zip"],
                lat = (decimal)spR["lat"],
                lon = (decimal)spR["lon"],
                locationType = spR["locationType"].ToString(),
                radius = (short)spR["radius"],
                landline = spR["landline"] == DBNull.Value ? "" : (string)spR["landline"],
                billingTier = spR["TherapyTier"] == DBNull.Value ? "" : (string)spR["TherapyTier"]
            }).ToList();
            return r;
        }

        private List<Chart> setClientCharts(ref DataSet ds, int tableIdx)
        {
            List<Chart> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new Chart()
            {
                chartId = (int)spR["chartId"],
                fileName = (string)spR["fileName"] + spR["extension"],
                service = (string)spR["svcLong"],
                startDate = spR["startDate"] == DBNull.Value ? "" : ((DateTime)spR["startDate"]).ToShortDateString(),
                endDate = spR["endDate"] == DBNull.Value ? "" : ((DateTime)spR["endDate"]).ToShortDateString(),
                docType = (string)spR["docType"]
            }).ToList();
            return r;
        }
        private List<CommentHistory> setCommentHistory(ref DataSet ds, int tableIdx)
        {
            List<CommentHistory> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new CommentHistory()
            {
                commentId = (int)spR["commentId"],
                subject = (string)spR["subject"],
                commentator = (string)spR["commentator"],
                comment = (string)spR["comment"],
                cmtType = (string)spR["cmtType"],
                cmtDt = ((DateTime)spR["cmtDt"]).ToShortDateString()
            }).ToList();
            return r;
        }

        private List<DCC.Models.Providers.ClientDetailInfo> setClaim(ref DataSet ds, int tableIdx)
        {
            List<DCC.Models.Providers.ClientDetailInfo> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new DCC.Models.Providers.ClientDetailInfo()
            {
                claimId = Convert.ToInt32(spR["ClaimID"] == DBNull.Value ? 0 : spR["ClaimID"]),
                billedAmount = Math.Round(Convert.ToDecimal(spR["AmtBilled"] == DBNull.Value ? 0 : spR["AmtBilled"]), 2),
                paidAmount = Math.Round(Convert.ToDecimal(spR["PaidAmt"] == DBNull.Value ? 0 : spR["PaidAmt"]), 2),
                ClaimStatus = Convert.ToString(spR["ClaimStatus"]),
                cptCode = Convert.ToString(spR["CPTCode"]),
                provider = Convert.ToString(spR["Provider"]),
                DateOfService = Convert.ToDateTime(spR["DateOfService"])
            }).ToList().OrderByDescending(c => c.DateOfService).ToList();
            return r;
        }

        private List<ServiceObjective> setObjectives(ref DataSet ds, int tableIdx)
        {

            DataView dv = new DataView(ds.Tables[tableIdx]);

            List<ServiceObjective> r = dv.ToTable(true, "serviceId", "clsvidId", "svcName", "isTherapy").Rows.Cast<DataRow>().Select(spR => new ServiceObjective()
            {
                serviceId = (int)spR["serviceId"],
                clsvidId = (int)spR["clsvidId"],
                svcName = (string)spR["svcName"],
                isTherapy = (bool)spR["isTherapy"]
            }).ToList();


            foreach (ServiceObjective serviceObjective in r)
            {
                dv.RowFilter = "serviceId=" + serviceObjective.serviceId + " AND objectiveId IS NOT NULL";
                serviceObjective.longTermObjectives = dv.ToTable(true, "objectiveId", "goalAreaId", "goalArea", "longTermVision", "longTermGoal", "objectiveStatus", "completedDt").Rows.Cast<DataRow>().Select(spR => new LongTermObjective()
                {
                    objectiveId = (int)spR["objectiveId"],
                    goalAreaId = (int)spR["goalAreaId"],
                    goalAreaName = spR["goalArea"] == DBNull.Value ? "Missing Goal Area" : (string)spR["goalArea"],
                    longTermVision = ConvertToHtml((string)spR["longTermVision"]),
                    longTermGoal = ConvertToHtml((string)spR["longTermGoal"]),
                    objectiveStatus = (string)spR["objectiveStatus"],
                    completedDt = spR["completedDt"] == DBNull.Value ? "" : ((DateTime)spR["completedDt"]).ToShortDateString()

                }).ToList();
                foreach (LongTermObjective longTermObjective in serviceObjective.longTermObjectives)
                {
                    dv.RowFilter = "serviceId=" + serviceObjective.serviceId + " AND objectiveId=" + longTermObjective.objectiveId + " AND objectiveId IS NOT NULL" + " AND goalId IS NOT NULL";
                    longTermObjective.shortTermGoals = dv.ToTable(true, "goalId", "shortTermGoal", "teachingMethod", "goalStatus", "frequency", "frequencyId", "goalCompletedDt").Rows.Cast<DataRow>().Select(spR => new ShortTermGoal()
                    {
                        goalId = (int)spR["goalId"],
                        shortTermGoal = ConvertToHtml((string)spR["shortTermGoal"]),
                        teachingMethod = ConvertToHtml((string)spR["teachingMethod"]),
                        goalStatus = (string)spR["goalStatus"],
                        frequency = (string)spR["frequency"],
                        frequencyId = spR["frequencyId"].ToString(),
                        completedDt = spR["goalCompletedDt"] == DBNull.Value ? "" : ((DateTime)spR["goalCompletedDt"]).ToShortDateString()

                    }).ToList();
                }
            }
            return r;

        }

        private List<CareAreaList> setCareAreas(ref DataSet ds, int tableIdx)
        {
            List<CareAreaList> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new CareAreaList()
            {
                careId = (int)spR["careId"],
                careArea = (string)spR["careArea"],
                deleted = (bool)spR["deleted"],
                lastDate = spR["lastDate"] == DBNull.Value ? "" : ((DateTime)spR["lastDate"]).ToShortDateString()
            }).ToList();
            return r;
        }
        private List<Documentation> setClientDocumentation(ref DataSet ds, int tableIdx)
        {
            List<Documentation> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new Documentation()
            {
                noteId = (int)spR["noteId"],
                noteType = (string)spR["noteType"],
                dt = ((DateTime)spR["dt"]).ToShortDateString(),
                serviceType = (string)spR["ds"],
                svc = (string)spR["svc"],
                staffName = (string)spR["fn"] + " " + (string)spR["ln"],
                clientName = (string)spR["cfn"] + " " + (string)spR["cln"],
                fileName = spR["fileName"] != DBNull.Value ? (string)spR["fileName"] : "",
                attachment = spR["attachmentName"] != DBNull.Value ? spR["attachmentName"].ToString() + (string)spR["fileExtension"] : "",
                hasAttachment = (bool)spR["hasAttachment"]
            }).ToList();

            return r;
        }


        private List<ProviderService> setProviderServices(ref DataSet ds, int tableIdx)
        {
            List<ProviderService> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new ProviderService()
            {
                relId = (int)spR["relId"],
                providerName = spR["fn"] + " " + spR["ln"],

                relationship = (string)spR["atcrelationship"],
                service = (string)spR["svc"],
            }).ToList();
            return r;
        }

        private List<Option> setCareAreaOptions(ref DataSet ds, int tableIdx)
        {
            List<Option> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new Option()
            {
                value = spR["serviceId"] + "-" + spR["clsvidId"],
                name = (string)spR["name"]
            }).ToList();
            return r;
        }

        private List<Option> setObjectiveOptions(ref DataSet ds, int tableIdx)
        {
            List<Option> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new Option()
            {
                value = spR["serviceId"] + "-" + spR["clsvidId"] + "-" + spR["goalAreaId"],
                name = spR["name"] + " - " + spR["goalAreaName"]
            }).ToList();
            return r;
        }




        [HttpGet]
        [AJAXAuthorize]
        public ActionResult GetAttachment(string fileName)
        {
            FileData f = new FileData("attachments", UserClaim.blobStorage);
            byte[] data = f.GetFile(fileName);
            Response.AddHeader("Content-Disposition", "attachment;filename=\"" + fileName + "\"");
            return new FileContentResult(data, MimeMapping.GetMimeMapping(fileName));
        }

        [HttpGet]
        [AJAXAuthorize]
        public ActionResult GetProgressReportDoc(string fileName)
        {

            FileData f = new FileData("progressreports", UserClaim.blobStorage);
            byte[] data = f.GetFile(fileName);
            Response.AddHeader("Content-Disposition", "attachment;filename=\"" + fileName + "\"");
            return new FileContentResult(data, MimeMapping.GetMimeMapping(fileName));
        }

        [HttpGet]
        [AJAXAuthorize]
        public ActionResult GetEvaluationDoc(string fileName)
        {
            FileData f = new FileData("evaluations", UserClaim.blobStorage);
            byte[] data = f.GetFile(fileName);
            Response.AddHeader("Content-Disposition", "attachment;filename=\"" + fileName + "\"");
            return new FileContentResult(data, MimeMapping.GetMimeMapping(fileName));
        }

        [HttpGet]
        [AJAXAuthorize]
        public ActionResult GetSessionNote(string fileName)
        {
            FileData f = new FileData("sessionnotes", UserClaim.blobStorage);
            byte[] data = f.GetFile(fileName);
            Response.AddHeader("Content-Disposition", "attachment;filename=\"" + fileName + "\"");
            return new FileContentResult(data, MimeMapping.GetMimeMapping(fileName));
        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> GetATCMonitoringReportPdf(string docId)
        {
            Er er = new Er();
            string fileName = Server.MapPath("~/Templates/") + "ATC Monitoring form.pdf";



            string filePath = "";
            MemoryStream ms = null;
            try
            {
                DataSet ds = new DataSet();
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskGetATCMonitoringReport", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@ATCMonitorId", docId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                PdfDocument document = PdfReader.Open(fileName, PdfDocumentOpenMode.Modify);

                if (document.AcroForm.Elements.ContainsKey("/NeedAppearances") == false)
                    document.AcroForm.Elements.Add("/NeedAppearances", new PdfBoolean(true));
                else
                    document.AcroForm.Elements["/NeedAppearances"] = new PdfBoolean(true);

                DataRow dr = ds.Tables[0].Rows[0];
                // client Name
                ((PdfTextField)document.AcroForm.Fields["inname"]).Value = new PdfString((string)dr["cnm"]);
                //AHCCCS Id of client
                ((PdfTextField)document.AcroForm.Fields["idno"]).Value = new PdfString((string)dr["clId"]);
                // monitoring date
                ((PdfTextField)document.AcroForm.Fields["date2"]).Value = new PdfString(((DateTime)dr["visitDt"]).ToShortDateString());


                // support coordinator Name ********
                ((PdfTextField)document.AcroForm.Fields["supcoorname"]).Value = new PdfString((string)dr["clwNm"]);
                // Monitor Name *******
                ((PdfTextField)document.AcroForm.Fields["monname"]).Value = new PdfString((string)dr["scnm"]);
                // Monitor Title **********
                ((PdfTextField)document.AcroForm.Fields["title1"]).Value = new PdfString((string)dr["scTitle"]);
                // Consumer or family Name *********
                ((PdfTextField)document.AcroForm.Fields["confamname"]).Value = new PdfString((string)dr["guardian"]);
                // Provider Name
                ((PdfTextField)document.AcroForm.Fields["provname"]).Value = new PdfString((string)dr["pnm"]);
                // Provider Title
                ((PdfTextField)document.AcroForm.Fields["title2"]).Value = new PdfString((string)dr["pTitle"]);


                // service start date
                ((PdfTextField)document.AcroForm.Fields["date1"]).Value = new PdfString(((DateTime)dr["serviceStartDate"]).ToShortDateString());
                // 5days
                if ((bool)dr["days5"])
                    ((PdfCheckBoxField)document.AcroForm.Fields["Check Box4"]).Checked = true;
                // 30 days
                if ((bool)dr["days30"])
                    ((PdfCheckBoxField)document.AcroForm.Fields["Check Box5"]).Checked = true;
                // 60 days
                if ((bool)dr["days60"])
                    ((PdfCheckBoxField)document.AcroForm.Fields["Check Box6"]).Checked = true;
                // 90 days
                if ((bool)dr["days90"])
                    ((PdfCheckBoxField)document.AcroForm.Fields["Check Box7"]).Checked = true;
                //ANC CheckBox
                if ((bool)dr["anc"])
                    ((PdfCheckBoxField)document.AcroForm.Fields["Check Box1"]).Checked = true;
                //AFC Checkbox
                if ((bool)dr["afc"])
                    ((PdfCheckBoxField)document.AcroForm.Fields["Check Box2"]).Checked = true;
                //Housekeeping
                if ((bool)dr["hsk"])
                    ((PdfCheckBoxField)document.AcroForm.Fields["Check Box3"]).Checked = true;




                int i = 1;
                foreach (DataRow questions in ds.Tables[1].Rows)
                {

                    ((PdfTextField)document.AcroForm.Fields["question" + questions["qNum"]]).Value = new PdfString((string)questions["cmt"]);

                    var rb = (PdfRadioButtonField)document.AcroForm.Fields["Radio Button" + i];
                    PdfName value;
                    int index;
                    if ((bool)questions["yes"])
                    {
                        value = new PdfName("/0");
                        index = 0;
                    }
                    else if ((bool)questions["no"])
                    {
                        value = new PdfName("/1");
                        index = 1;
                    }
                    else
                    {
                        value = new PdfName("/2");
                        index = 2;
                    }


                    rb.Value = value;
                    PdfArray kids = (PdfArray)rb.Elements["/Kids"];
                    int j = 0;
                    foreach (var kid in kids)
                    {
                        var kidValues = ((PdfSharp.Pdf.Advanced.PdfReference)kid).Value as PdfDictionary;
                        PdfRectangle rectangle = kidValues.Elements.GetRectangle(PdfSharp.Pdf.Annotations.PdfAnnotation.Keys.Rect);
                        if (j == index)
                            kidValues.Elements.SetValue("/AS", value);
                        else
                            kidValues.Elements.SetValue("/AS", new PdfName("/Off"));
                        j++;
                    }


                    /*
                    if ((bool)questions["yes"])
                        ((PdfRadioButtonField)document.AcroForm.Fields["Radio Button" + i]).Value = new PdfName("/0");
                    else if ((bool)questions["no"])
                        ((PdfRadioButtonField)document.AcroForm.Fields["Radio Button" + i]).Value = new PdfName("/1");
                    else if ((bool)questions["na"])
                        ((PdfRadioButtonField)document.AcroForm.Fields["Radio Button" + i]).Value = new PdfName("/2");
                    */


                    i++;
                }
                if (document.AcroForm.Elements.ContainsKey("/NeedAppearances") == false)
                    document.AcroForm.Elements.Add("/NeedAppearances", new PdfBoolean(true));
                else
                    document.AcroForm.Elements["/NeedAppearances"] = new PdfBoolean(true);

                //monitor sig date
                // ((PdfTextField)document.AcroForm.Fields["date3"]).Value = new PdfString("3/3/2019");
                //family sig date
                //  ((PdfTextField)document.AcroForm.Fields["date4"]).Value = new PdfString("4/4/2019");
                // provider sig date
                //   ((PdfTextField)document.AcroForm.Fields["date5"]).Value = new PdfString("5/5/2019");


                filePath = "MonitorReport_" + dr["cnm"].ToString().Replace(" ", "_") + "_" + ((DateTime)dr["visitDt"]).Year.ToString() + "_" + ((DateTime)dr["visitDt"]).ToString("MM") + "_" + ((DateTime)dr["visitDt"]).ToString("dd") + ".pdf";
                ms = new MemoryStream();
                document.Save((Stream)ms, false);

                ms.Seek(0, SeekOrigin.Begin);

                document.Dispose();

            }
            catch (Exception ex)
            {
                er.msg = ex.Message;
                er.code = 1;

            }
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
            {

                Response.AddHeader("Content-Disposition", "attachment;filename=" + filePath);
                return new FileContentResult(ms.ToArray(), "application/pdf");

            }

        }

 

        private List<InsurancePolicyDTO> setPolicy(ref DataSet ds, int tableIdx)
        {
            try
            {
                List<InsurancePolicyDTO> policies = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new InsurancePolicyDTO()
                {
                    insuredName = spR["FirstName"] == DBNull.Value ? "" : (string)spR["FirstName"],
                    insuredIdNo = spR["InsuredIdNo"] == DBNull.Value ? "" : (string)spR["InsuredIdNo"],
                    startDate = spR["startDate"] == DBNull.Value ? "" : ((DateTime)spR["startDate"]).ToShortDateString(),
                    endDate = ((DateTime)spR["endDate"]).ToString("yyyy-MM-dd") == DateTime.MaxValue.ToString("yyyy-MM-dd") ? "" : ((DateTime)spR["endDate"]).ToShortDateString(),
                    IsDeletable = spR["Deletable"] == DBNull.Value ? "" : (string)spR["Deletable"],
                    hasWaivers = spR["HasWaivers"] == DBNull.Value ? "" : (string)spR["HasWaivers"],
                    insurancePolicyID = (int)spR["InsurancePolicyId"],
                    policyGroupNumber = spR["PolicyNumber"] == DBNull.Value ? "" : (string)spR["PolicyNumber"],
                    InsurancePriorityID = spR["InsurancePriorityId"] == DBNull.Value ? 0 : Convert.ToInt32((byte)spR["InsurancePriorityId"]),
                    companyId = (int)spR["InsuranceCompanyId"],
                    //IsDelete=(string)spR["IsDelete"],

                     Inactive = (bool)spR["Inactive"],

                    Expired = ((DateTime)spR["EndDate"]) > DateTimeLocal().AddDays(-1) ? false : true

            }).ToList();
                var insuranceIds = policies.Select(x => x.companyId).ToList();

                InsuranceCompanyController InsuranceCompanyCnt = new InsuranceCompanyController();
                var insuranceCompaniesFull = InsuranceCompanyCnt.GetInsuranceCompanies();

                insuranceCompaniesFull.RemoveAll(x => !insuranceIds.Contains(x.InsuranceCompanyId));

                insuranceCompaniesFull.ForEach(x =>
                {
                    policies.Where(c => c.companyId == x.InsuranceCompanyId).ToList()
                            .ForEach(s =>
                            {
                                s.companyName = x.Name;
                            });
                });
                return policies;
            }
            catch (Exception ex)
            {
               throw;
            }

        }
        private List<PolicyWaiver> setWaiver(ref DataSet ds, int tableIdx)
        {
            List<PolicyWaiver> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new PolicyWaiver()
            {
                FromDate = spR["StartDate"] == DBNull.Value ? "" : ((DateTime)spR["StartDate"]).ToShortDateString(),
                ToDate = spR["EndDate"] == DBNull.Value ? "" : ((DateTime)spR["EndDate"]).ToShortDateString(),
                ServiceID = (int)spR["ClientServiceId"],
                ServiceName = (string)spR["ServiceName"],
                PolicyWaiverId = (int)spR["PolicyWaiverId"],
                InsurancePolicyID = (int)spR["InsurancePolicyId"],

            }).ToList();
            return r;
        }


        private List<Option> setServiceList(ref DataSet ds, int tableIdx)
        {
            List<Option> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new Option()
            {

                value = Convert.ToString(spR["serviceId"]),
                name = (string)spR["Name"]
            }).ToList();
            return r;
        }

        private List<BillingInsuranceCompany> GetBillingInsuranceCompanies(int? id = null)
        {
            var toReturn = new List<BillingInsuranceCompany>();
            var result = new DataTable();
            try
            {
                using (SqlConnection connection = new SqlConnection(UserClaim.conStr))
                {
                    SQLHelper sqlHelper = new SQLHelper();
                    SqlCommand sqlCommand = new SqlCommand("sp_GetBillingInsuranceCompanies", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlCommand.Parameters.AddWithValue("@ID", id);
                    sqlHelper.ExecuteSqlDataAdapter(sqlCommand, result);
                }

                if (result.HasRows())
                {
                    // get list of insurance companies
                    toReturn = result.Rows.Cast<DataRow>().Select(x => new BillingInsuranceCompany()
                    {
                        InsuranceCompanyId = x.GetValueOrDefault<Int32>("InsuranceCompanyId"),
                        PatientCount = x.GetValueOrDefault<Int32>("ClientCount"),
                        EnableEligibility = x.GetValueOrDefault<bool>("EnableEligibility"),
                        StatusDelay = x.GetValueOrDefault<Int16>("StatusDelay"),
                        StatusFreq = x.GetValueOrDefault<Int16>("StatusFreq"),
                        ExcludeRenderer = x.GetValueOrDefault<bool>("ExcludeRenderer")

                    }).ToList();
                    // get list of insurance company ids
                    var insuranceIds = toReturn.Select(x => x.InsuranceCompanyId).ToList();
                    // create string of comma delimited insurance company Ids
                    var ids = String.Join(",", insuranceIds);
                    InsuranceCompanyController InsuranceCompanyCnt = new InsuranceCompanyController();


                    //get all insurance companies from main sp_InsuranceGetInsuranceList - gets table[0]
                    var insuranceCompaniesFull = InsuranceCompanyCnt.GetInsuranceCompanies();

                    //from the full list remove anything we dont have 
                    insuranceCompaniesFull.RemoveAll(x => !insuranceIds.Contains(x.InsuranceCompanyId));

                    // for those left set the name, isgovt and insCode 
                    insuranceCompaniesFull.ForEach(x =>
                    {
                        toReturn.FirstOrDefault(c => c.InsuranceCompanyId == x.InsuranceCompanyId).Name = x.Name;
                        toReturn.FirstOrDefault(c => c.InsuranceCompanyId == x.InsuranceCompanyId).IsGovt = x.IsGovt;
                        toReturn.FirstOrDefault(c => c.InsuranceCompanyId == x.InsuranceCompanyId).InsCode = x.InsCode;
                    });
                }
            }
            catch (Exception ex)
            {
            }
            return toReturn.OrderBy(x => x.Name).ToList();
        }

        [HttpGet]
        public JsonResult IsPolicyAlreadyAdded(int insurancePriorityId, int clientId, DateTime? startDate, DateTime? endDate, int? insurancePolicyId = null)
        {
            var data = PolicyExistance(insurancePriorityId, clientId, startDate, endDate, insurancePolicyId);
            return Json(new
            {
                PreviousTierRequired = data.IsPreviousTierRequired,
                DateRangeOverlapped = data.IsDateRangeOverlapped,
                CanProceed = data.CanProceed,
            }, JsonRequestBehavior.AllowGet);
        }

        private ExistanceCheck PolicyExistance(int insurancePriorityId, int clientId, DateTime? startDate, DateTime? endDate, int? InsurancePolicyId)
        {
            var fromDate = Convert.ToDateTime(startDate).ToString("yyyy-MM-dd");
            var toDate = Convert.ToDateTime(endDate).ToString("yyyy-MM-dd");
            var dataTable = new DataTable();
            var result = new ExistanceCheck();

            using (SqlConnection sqlConnection = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand sqlCommand = new SqlCommand("sp_IsPolicyAlreadyAdded", sqlConnection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                try
                {
                    sqlCommand.Parameters.AddWithValue("@ClientId", clientId);
                    sqlCommand.Parameters.AddWithValue("@InsurancePriorityId", insurancePriorityId);
                    sqlCommand.Parameters.AddWithValue("@StartDate", fromDate);
                    sqlCommand.Parameters.AddWithValue("@EndDate", toDate);
                    sqlCommand.Parameters.AddWithValue("@InsurancePolicyId", InsurancePolicyId);

                    if (sqlConnection.State == ConnectionState.Closed)
                    {
                        sqlConnection.Open();
                    }
                    sqlHelper.ExecuteSqlDataAdapter(sqlCommand, dataTable);

                    if (dataTable.HasRows())
                    {
                        result = dataTable.Rows.Cast<DataRow>().Select(x => new ExistanceCheck()
                        {
                            IsDateRangeOverlapped = x.GetValueOrDefault<bool>("IsDateRangeOverlapped"),
                            IsPreviousTierRequired = x.GetValueOrDefault<bool>("IsPreviousTierRequired"),
                            CanProceed = x.GetValueOrDefault<bool>("CanProceed"),
                        }).FirstOrDefault();
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
                return result;
            }
        }

        private bool PolicyResequenceRequired(int clientId, int tierId)
        {
            var result = false;
            using (SqlConnection sqlConnection = new SqlConnection(UserClaim.conStr))
            {
                var dataTable = new DataTable();
                SqlCommand sqlCommand = new SqlCommand("sp_IsResequencePolicy", sqlConnection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                try
                {
                    sqlCommand.Parameters.AddWithValue("@ClientId", clientId);
                    sqlCommand.Parameters.AddWithValue("@TierId", tierId);

                    if (sqlConnection.State == ConnectionState.Closed)
                    {
                        sqlConnection.Open();
                    }
                    result = (bool)sqlCommand.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    throw;
                }
                return result;
            }


        }

        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public JsonResult WaiverExistance(List<PolicyWaiverDTO> waivers)
        {
            
            var newWaivers = waivers;
            bool hasWaiver = false;
            foreach (var item in waivers)
            {
                SqlParameter isWaiverExist = new SqlParameter("@IsWaiver", SqlDbType.Bit);
                using (SqlConnection sqlConnection = new SqlConnection(UserClaim.conStr))
                {
                    var dataTable = new DataTable();
                    SqlCommand sqlCommand = new SqlCommand("sp_IsWaiverAdded", sqlConnection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    try
                    {
                        isWaiverExist.Direction = ParameterDirection.ReturnValue;
                        sqlCommand.Parameters.Add(isWaiverExist);
                        sqlCommand.Parameters.AddWithValue("@ServiceId", item.ServiceId);
                        sqlCommand.Parameters.AddWithValue("@ServiceStartDate", item.StartDate);
                        sqlCommand.Parameters.AddWithValue("@ServiceEndDate", item.EndDate);
                        sqlCommand.Parameters.AddWithValue("@InsurancePolicyId", item.InsurancePolicyId);

                        if (sqlConnection.State == ConnectionState.Closed)
                        {
                            sqlConnection.Open();
                        }
                        sqlCommand.ExecuteNonQuery();
                        hasWaiver = Convert.ToBoolean(isWaiverExist.Value);
                    }

                    catch (Exception ex)
                    {
                    }
                }
                item.IsExist = hasWaiver;
            }
            return Json(newWaivers.ToList(), JsonRequestBehavior.AllowGet);
        }


        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public JsonResult DeletePolicyWaiver(int serviceId, int policyWaiverId)
        {
            bool hasWaiver = false;
            using (SqlConnection sqlConnection = new SqlConnection(UserClaim.conStr))
            {
                var dataTable = new DataTable();
                SqlCommand sqlCommand = new SqlCommand("sp_DeletePolicyWaiver", sqlConnection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                try
                {
                    sqlCommand.Parameters.AddWithValue("@ServiceId", serviceId);
                    sqlCommand.Parameters.AddWithValue("@PolicyWaiverId", policyWaiverId);

                    if (sqlConnection.State == ConnectionState.Closed)
                    {
                        sqlConnection.Open();
                    }
                    sqlCommand.ExecuteNonQuery();
                    hasWaiver = true;
                }

                catch (Exception ex)
                {
                }
            }
            return Json(hasWaiver, JsonRequestBehavior.AllowGet);
        }

        private string RenderRazorViewToString(string viewName, object model)
        {
            ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }
        private List<SelectListItem> GetClientCPTCode()
        {
            DataTable dt = new DataTable();
            List<SelectListItem> CPTCodeList = new List<SelectListItem>();

            try
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_ClientGetCPTCode", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlHelper.ExecuteSqlDataAdapter(cmd, dt);
                }

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        SelectListItem cptCode = new SelectListItem();
                        cptCode.Text = row["Name"].ToString();
                        cptCode.Value = (string)row["Code"];
                        CPTCodeList.Add(cptCode);
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return CPTCodeList;
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> UpdateCPTCode(CPTCode cptCode)
        {
            Er r = new Er();
            List<SelectListItem> CPTCodeList = new List<SelectListItem>();
            if (!IsCPTCodeExists(cptCode.Code))
            {
                DataTable dt = new DataTable();
                try
                {
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_ClientAddCPTCode", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@code", cptCode.Code);
                            cmd.Parameters.AddWithValue("@name", cptCode.Name);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, dt);
                        }
                    });
                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            SelectListItem item = new SelectListItem();
                            item.Text = row["Name"].ToString();
                            item.Value = (string)row["Code"];
                            CPTCodeList.Add(item);
                        }
                    }
                    dt.Dispose();
                    return Json(CPTCodeList);
                }
                catch (Exception ex)
                {
                    r.code = 1;
                    r.msg = ex.Message;

                }
            }
            else
            {
                r.code = 1;
                r.msg = "CPT Code already exists.";
            }
            return Json(r);
        }

        private bool IsCPTCodeExists(string cptCode)
        {
            List<SelectListItem> CPTCodeList = new List<SelectListItem>();
            CPTCodeList = GetClientCPTCode();
            if (CPTCodeList.Where(c => c.Value == cptCode).Count() > 0)
                return true;
            else return false;
        }


        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> GetClientDocumentation(int clsvId, string documentationStart, string documentationEnd, bool documentationSelNotes, bool documentationSelReports)
        {
            ClientPageData r = new ClientPageData();
            r.documentationStart = documentationStart;
            r.documentationEnd = documentationEnd;
            r.documentationSelNotes = documentationSelNotes;
            r.documentationSelReports = documentationSelReports;
            r.userLevel = UserClaim.userLevel;


            Er er = new Er();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetClientDocumentation", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@clsvId", clsvId);
                        cmd.Parameters.AddWithValue("@docStartDate", r.documentationStart);
                        cmd.Parameters.AddWithValue("@docEndDate", r.documentationEnd);
                        cmd.Parameters.AddWithValue("@docSelectNotes", r.documentationSelNotes);
                        cmd.Parameters.AddWithValue("@docSelectReports", r.documentationSelReports);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.documentation = setClientDocumentation(ref ds, 0);
            }

            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("Client_Documents", r);
        }

        [HttpGet]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> ToggleClientSvcManualInOut(ManualInOutOn r)
        {

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientSetClientServiceManualInOut", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clientServiceId", r.clsvidId);
                        cmd.Parameters.AddWithValue("@on", r.on);
                        cn.Open();
                        cmd.ExecuteNonQuery();
                        cn.Close();
                    }
                });
            }

            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }



            return Json(r, JsonRequestBehavior.AllowGet);
        }


        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> UpdateAssignedRate(int clientServiceId, int rateId)
        {

            Er er = new Er();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ClientSetClientServiceAssignedRate", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@clientServiceId", clientServiceId);
                        cmd.Parameters.AddWithValue("@rateId", rateId);
                        cn.Open();
                        cmd.ExecuteNonQuery();
                        cn.Close();
                    }
                });
            }

            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }

            return Json(er);
        }




    }

}


