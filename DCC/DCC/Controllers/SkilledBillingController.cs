using DCC.Models.Providers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using DCC.Helpers;
using System.Text;
using System.IO;
using DCC.Models;
using DCC.SQLHelpers.Helpers;
using DCCHelper;
using Newtonsoft.Json;
using ICSharpCode.SharpZipLib.Zip;
using NPOI.Util;
using DCC.ModelsLegacy;

namespace DCC.Controllers
{
    public class SkilledBillingController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;
        public SkilledBillingController()
        {
            sqlHelper = new SQLHelper();
        }




        // SqlConnection cn = new SqlConnection(UserClaim.conStr);
        [Authorize]
        public async Task<ActionResult> Index()
        {
            ProviderInit r = new ProviderInit();
            // For Admin get current clients & clients that have been deactivated in the last 12 months
            DataSet ds = null;
            DataSet ds1 = null;
            DataSet ds2 = null;

            try
            {
                //alphabetic order
                ds = await getInsuranceList();
                ds1 = await getClaimStatusList();
                ds2 = await getGovernmentProgramList();

            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            if (ds != null)
            {
                r.providerList = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new Provider()
                {
                    payerId = spR["PayerId"] == DBNull.Value ? "" : (string)spR["PayerId"],
                    name = spR["Name"] == DBNull.Value ? "" : (string)spR["Name"],
                    insuranceCompanyId = Convert.ToInt32(spR["InsuranceCompanyId"] == DBNull.Value ? 0 : spR["InsuranceCompanyId"])
                    //line1 = (string)spR["Line1"],
                    //line2 = (string)spR["Line2"],
                    //city = (string)spR["City"],
                    //state = (string)spR["State"],
                    //postalCode = (string)spR["PostalCode"]
                }).ToList();
            }

            DataTable dt = getInsuranceCompanyIds().Result;
            r.GovernmentPrograms = ds2.Tables[0].Rows.Cast<DataRow>().Select(spR => new GovernmentProgram()
            {
                Id = int.Parse(spR["Id"] == DBNull.Value ? "0" : spR["Id"].ToString()),
                Name = spR["Name"] == DBNull.Value ? "" : (string)spR["Name"],
                InsuranceID = int.Parse(dt.Rows[0][0] == DBNull.Value ? "0" : dt.Rows[0][0].ToString())
            }).ToList();



            r.claimStatusList = ds1.Tables[0].Rows.Cast<DataRow>().Select(spR => new ClaimsStatus()
            {
                claimstatusid = int.Parse(spR["ClaimStatusid"].ToString()),
                name = spR["Name"] == DBNull.Value ? "" : (string)spR["Name"],
                statusType = spR["statusType"].ToString()
            }).ToList();
            r.claimStatusList.Add(new ClaimsStatus() { claimstatusid = -2, name = "All Statuses" });

            DataSet ds3 = await getDenialReasonList();
            r.DenialReasonList = ds3.Tables[0].Rows.Cast<DataRow>().Select(spR => new DenialReason()
            {
                // id = Convert.ToInt32(spR["id"] == DBNull.Value ? 0 : spR["id"]),
                id = ((string)spR["id"]).Trim(),
                name = (string)spR["name"]
            }).OrderBy(d => d.id).ToList();
            r.defaultReason = r.DenialReasonList.Where(d => d.id == "1").Select(d => d.id + ": " + d.name).FirstOrDefault();

            //EmptyView r = new EmptyView();
            setViewModelBase((ViewModelBase)r);

            // View1 is the new layout //
            return View("View3", r);
        }

        public async Task<ActionResult> SampleTest()
        {
            ProviderInit r = new ProviderInit();
            // For Admin get current clients & clients that have been deactivated in the last 12 months
            DataSet ds = null;
            DataSet ds1 = null;
            try
            {
                //alphabetic order
                ds = await getInsuranceList();
                ds1 = await getClaimStatusList();
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            r.providerList = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new Provider()
            {
                payerId = (string)spR["PayerId"],
                name = (string)spR["Name"]

            }).ToList();

            r.claimStatusList = ds1.Tables[0].Rows.Cast<DataRow>().Select(spR => new ClaimsStatus()
            {
                name = (string)spR["Name"],

            }).ToList();
            setViewModelBase((ViewModelBase)r);
            // View1 is the new layout //
            return View("View2", r);
        }

        private Task<DataSet> getInsuranceList(bool includeGovt = false)
        {

            return Task.Run(() =>
            {
                List<int> companyids = new List<int>();
                var str = "";
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    //need to add the storeproc to get Insurance
                    //SqlCommand cmd = new SqlCommand("sp_InsuranceGetInsuranceList", cn)
                    SqlCommand cmd = new SqlCommand("sp_ClaimGetInsuranceList", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    // datatable1.AsEnumerable().Select(r => r.Field<string>("Name")).ToArray();
                    companyids = ds.Tables[0].AsEnumerable().Select(r => r.Field<int>("InsuranceCompanyId")).ToList();
                    //companyids = ds.Tables[0].Rows.Cast<string>().ToList();                    
                    str = String.Join(",", companyids);

                }
                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    DataSet ds = new DataSet();
                    if (str != "")
                    {
                        SqlCommand cmd = new SqlCommand("sp_getInsuranceCompanies", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@insuranceCompanyids", str);
                        cmd.Parameters.AddWithValue("@isNonGovt", !includeGovt);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                    else
                        ds = null;
                    return ds;


                }
            });
        }
        private Task<DataSet> getGovernmentProgramList()
        {

            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    DataSet ds = new DataSet();
                    SqlCommand cmd = new SqlCommand("sp_GetGovernmentProgram", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
        }
        private Task<DataSet> getClaimStatusList(int? currentStatusId = null)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_ClaimStatusGetClaimStatusList", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    if (currentStatusId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@currentStatusId", currentStatusId.Value);
                    }
                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
        }

        private Task<DataSet> getClientListByInsurance(string payerID, bool selfpay, int claimStatus, int tierId = -1, int insuranceFlag = 0)
        {
            DataTable dt = getInsuranceCompanyIds().Result;
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_InsuranceGetClientList", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    if (!(string.IsNullOrEmpty(payerID) || payerID == "0")) cmd.Parameters.AddWithValue("@PayerID", payerID);
                    if (selfpay) cmd.Parameters.AddWithValue("@isSelfPay", 1);
                    if (tierId != -1) cmd.Parameters.AddWithValue("@TierId", tierId);
                    if (insuranceFlag != 0)
                        cmd.Parameters.AddWithValue("@InsuaranceFlag", tierId);
                    cmd.Parameters.AddWithValue("@GovtInsnIds", dt);
                    //cmd.Parameters.AddWithValue("@claimStatus", claimStatus);
                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
        }

        private Task<DataSet> getClientListByInsuranceClaimStatus(string payerID, bool selfpay, int claimStatus, int tierId = -1, int insuranceFlag = 0, bool isGovtIns = false, string dddClientFlag = null)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("BillingGetClientClaimsListByInsurance", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    if (!(string.IsNullOrEmpty(payerID) || payerID == "0")) cmd.Parameters.AddWithValue("@insuranceCompanyID", payerID);
                    if (selfpay) cmd.Parameters.AddWithValue("@isSelfPay", 1);
                    //cmd.Parameters.AddWithValue("@isSelfPay", selfpay);
                    //if (tierId != -1) cmd.Parameters.AddWithValue("@TierId", tierId);
                    //if (insuranceFlag != 0)
                    //    cmd.Parameters.AddWithValue("@InsuaranceFlag", tierId);
                    cmd.Parameters.AddWithValue("@claimStatusID", claimStatus);
                    cmd.Parameters.AddWithValue("@isGovtIns", isGovtIns ? 1 : 0);
                    int dddFlag = dddClientFlag == "all" ? -1 : dddClientFlag == "ddd" ? 0 : 1;
                    cmd.Parameters.AddWithValue("@dddNonDDDFlag", dddFlag);
                    cmd.CommandTimeout = 0;
                    //cmd.Parameters.AddWithValue("@claimStatus", claimStatus);
                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
        }

        private Task<DataTable> getInsuranceCompanyIds()
        {
            return Task.Run(() =>
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("InsuranceCompanyId");
                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    DataSet ds = new DataSet();
                    SqlCommand cmd = new SqlCommand("sp_getInsuranceCompanies", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    ds.Tables[0].DefaultView.RowFilter = "IsGovt = 1";
                    foreach (DataRow dr in ds.Tables[0].DefaultView.ToTable().Rows)
                    {
                        DataRow drNew = dt.NewRow();
                        drNew["InsuranceCompanyId"] = dr["InsuranceCompanyId"];
                        dt.Rows.Add(drNew);
                    }
                }
                return dt;
            });
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> ClientList(Provider s, int status, bool selfpay = false, int tierId = -1, int insuranceFlag = 0, bool isGovtIns = false, string dddClientFlag = null)
        {
            ProviderInit r = new ProviderInit();
            DataSet ds = null;
            DataTable dt = null;

            try
            {
                ds = await getClientListByInsuranceClaimStatus(s.payerId, selfpay, status, tierId, insuranceFlag, isGovtIns, dddClientFlag);
                dt = (ds.Tables[0].DefaultView).ToTable();
                r.clientInfoList = dt.Rows.Cast<DataRow>().Select(spR => new ClientInfo()
                {
                    name = Convert.ToString(spR["Name"]),
                    dob = Convert.ToDateTime(spR["dob"] == DBNull.Value ? default(DateTime) : spR["dob"]),//Convert.ToDateTime(spR["dob"]),
                    clientId = Convert.ToInt32(spR["clsvID"] == DBNull.Value ? 0 : spR["clsvID"]),
                    claimIds = spR["ClaimIds"] == DBNull.Value ? "" : Convert.ToString(spR["ClaimIds"]),
                    filteredCount = 1
                }).ToList();
            }
            catch (Exception ex1)
            {
                r.er.msg = ex1.Message;
            }

            if (ds != null)
                ds.Dispose();

            var genderType = from GenderTypeEnum e in Enum.GetValues(typeof(GenderTypeEnum))
                             select new
                             {
                                 ID = (int)e,
                                 Name = e.ToString()
                             };

            ViewBag.EnumList = new SelectList(genderType, "ID", "Name");

            //return Json(r);
            return PartialView("ClientDetails", r);
        }

        private Task<DataSet> getClientDetailsByClientid(string clientId, string policyId, int tierId = -1, int insuranceFlag = 0, int claimStatusId = -2)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_BillingGetBillingDetails", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@ClientID", clientId);
                    if (policyId != "" && policyId != "0")
                        cmd.Parameters.AddWithValue("@InsurancePolicyId", policyId);
                    if (tierId != -1)
                        cmd.Parameters.AddWithValue("@TierId", tierId);
                    if (insuranceFlag != 0)
                        cmd.Parameters.AddWithValue("@InsuaranceFlag", insuranceFlag);
                    if (claimStatusId >= 0)
                        cmd.Parameters.AddWithValue("@claimStatusId", claimStatusId);
                    DataSet ds = new DataSet();
                    cmd.CommandTimeout = 120;
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
        }

        private Task<DataSet> getClaimsPaymentByClientid(string clientId, string policyId)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_BillingGetClaimPayments", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    cmd.Parameters.AddWithValue("@PayerId", policyId);
                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
        }
        private Task<DataSet> getClaimsPaymentAmounts(string clientId)//,long therapyId
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_BillingGetClaimPaymentAmount", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    //cmd.Parameters.AddWithValue("@ClientSessionTherapyID", therapyId);
                    //cmd.Parameters.AddWithValue("@InsurancePolicyId", policyId);                   
                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
        }

        private Task<DataSet> getClaimsCommentListing(int claimId)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("BillingGetClaimComments", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@claimID", claimId);

                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
        }

        private Task<DataSet> getClaimStatusListByClient(string clientId, string policyId, int filterStatus, int tierId = -1, int insuranceFlag = 0, bool isGovtPgm = false, int isSelfPay = 0)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("BillingGetClientClaimsListByInsurance", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@clientID", clientId);
                    cmd.Parameters.AddWithValue("@insuranceCompanyID", policyId);
                    cmd.Parameters.AddWithValue("@claimStatusID", filterStatus);
                    cmd.Parameters.AddWithValue("@isGovtIns", isGovtPgm ? 1 : 0);
                    cmd.Parameters.AddWithValue("@isSelfPay", isSelfPay);

                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
        }

        private Task<DataSet> getClientClaimsCount(string clientId, string insuranceCompanyId)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ClientGetClaimsCount", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@ClientID", clientId);
                    cmd.Parameters.AddWithValue("@InsuranceCompanyId", insuranceCompanyId);            
                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
        }

        [HttpGet]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public async Task<JsonResult> GetClientDetailStatuses(string clientId, string policyId, int filterStatus, int tierId = -1, int insuranceFlag = 0, bool isGovtPgm = false, int isSelfPay = 0)
        {
            DataSet ds = await getClaimStatusListByClient(clientId, policyId, filterStatus, tierId, insuranceFlag, isGovtPgm, isSelfPay);
            var lstStatus = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new Option()
            {
                value = spR["ClaimStatusId"] == DBNull.Value ? "100" : Convert.ToString(spR["ClaimStatusId"]),
                name = spR["ClaimStatusId"] == DBNull.Value ? "Pending Processing" : spR["ClaimStatusName"].ToString()
            }).ToList();

            return Json(lstStatus, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> ClaimCommentList(int claimId)
        {
            DataSet ds = await getClaimsCommentListing(claimId);

            ClaimComments cc = new ClaimComments();

            cc.comments = ds.Tables[0].Rows.Cast<DataRow>().Select(x => new ClaimComment
            {
                StaffId = Convert.ToInt32(x["StaffId"] ?? 0),
                staffName = x["staffName"].ToString(),
                fn = x["fn"].ToString(),
                ln = x["ln"].ToString(),
                MadeAt = x["MadeAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(x["MadeAt"]),
                Comments = x["Comments"].ToString()
            }).ToList();

            return PartialView("ClaimComments", cc);
        }

        // Getting client details by Id //
        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> ClientDetails(string clientId, string selrowid, int selectedClientIndex, string policyId, int claimStatusId = -1, int claimStatusId2 = -1, int tierId = -1, int insuranceFlag = 0, bool isGovtPgm = false)
        {
            ViewBag.selrowid = selrowid;
            ViewBag.selectedClientIndex = selectedClientIndex;
            ProviderInit r = new ProviderInit();
            DataSet ds = null;
            DataSet dsPayment = null;
            DataSet dsAmount = null;
            List<ClientDetails> clDetails = new List<ClientDetails>();
            r.ClientDetailsList = new List<ClientDetails>();
            DataSet dsDccMain = new DataSet();
            DataSet ds3 = await getDenialReasonList();
            r.DenialReasonList = ds3.Tables[0].Rows.Cast<DataRow>().Select(spR => new DenialReason()
            {
                // id = Convert.ToInt32(spR["id"] == DBNull.Value ? 0 : spR["id"]),
                id = ((string)spR["id"]).Trim(),
                name = (string)spR["name"]
            }).OrderBy(d => d.id).ToList();
            r.defaultReason = r.DenialReasonList.Where(d => d.id == "1").Select(d => d.id + ": " + d.name).FirstOrDefault();

            try
            {
                ds = await getClientDetailsByClientid(clientId, policyId, tierId, insuranceFlag, claimStatusId2);
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                    {
                        //need to add the storeproc to get Insurance
                        SqlCommand cmd = new SqlCommand("sp_InsuranceGetInsuranceList", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        sqlHelper.ExecuteSqlDataAdapter(cmd, dsDccMain);
                    }
                });
                dsPayment = await getClaimsPaymentByClientid(clientId, policyId);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            Dictionary<long, List<SelectListItem>> downLoadCategories = new Dictionary<long, List<SelectListItem>>();
            //string t = ds.Tables[0].Rows[0]["AmtBilled"].ToString();
            if (r.er.code == 0 && ds.Tables[0].Rows.Count > 0)
            {
                DataRow dr = ds.Tables[0].Rows[0];
                DateTime dosOld = DateTime.MinValue;

                //if (policyId != "")
                //ds.Tables[0].DefaultView.RowFilter = claimStatusId2 == -1 ? claimStatusId == -2 ? "" : (claimStatusId == -1 ? "ClaimStatusID = 1" : (claimStatusId == 100 ? "ClaimStatusID = 0" : "ClaimStatusID = " + claimStatusId)) : (claimStatusId2 == 100 ? "ClaimStatusID = 0" : "ClaimStatusID = " + claimStatusId2);
                ds.Tables[0].DefaultView.RowFilter = claimStatusId2 == -1 ? claimStatusId == -2 ? "" : (claimStatusId == -1 ? "ClaimStatusID = 1" : ("ClaimStatusID = " + claimStatusId)) : ("ClaimStatusID = " + claimStatusId2);
                //else if (policyId == "" && claimStatusId != -1)
                //{
                //    ds.Tables[0].DefaultView.RowFilter = claimStatusId == 100 ? "ClaimStatusID = 0" : "ClaimStatusID = " + claimStatusId;
                //}
                DataTable dt = (ds.Tables[0].DefaultView).ToTable();
                int iOld = 0;

                for (int i = 0; i < dt.Rows.Count; i++)
                {

                    DataRow drDet = dt.Rows[i];
                    ClientDetails rc = new ClientDetails
                    {
                        dos = Convert.ToDateTime(drDet["DateOfService"] == DBNull.Value ? default(DateTime) : drDet["DateOfService"])
                    };
                    List<ClaimComment> comments = drDet["Comments"] != DBNull.Value ? new List<ClaimComment>(){new ClaimComment{ StaffId = 0, staffName =  "", fn = "", ln = "", MadeAt = null, Comments = Convert.ToString(drDet["Comments"])}} : new List<ClaimComment>();
                    // List<ClaimComment> comments = drDet["Comments"] != DBNull.Value ? JsonConvert.DeserializeObject<List<ClaimComment>>(drDet["Comments"].ToString()) : new List<ClaimComment>();
                    ClientDetailInfo cdi = new ClientDetailInfo
                    {
                        provider = Convert.ToString(drDet["Provider"]),
                        cptCode = Convert.ToString(drDet["CPTCode"]),
                        billedAmount = Math.Round(Convert.ToDecimal(drDet["AmtBilled"] == DBNull.Value ? 0 : drDet["AmtBilled"]), 2),
                        paidAmount = Math.Round(Convert.ToDecimal(drDet["PaidAmt"] == DBNull.Value ? 0 : drDet["PaidAmt"]), 2),
                        paymentDate = drDet["PaymentDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(drDet["PaymentDate"]),
                        policyNumber = Convert.ToString(drDet["PolicyNbr"]),
                        groupNumber = Convert.ToString(drDet["GroupNbr"]),
                        dddStartDate = (drDet["DDDStart"] == DBNull.Value ? "" : Convert.ToDateTime(drDet["DDDStart"]).ToString("MM/dd/yy")),
                        dddEndDate = (drDet["DDDEnd"] == DBNull.Value ? "" : Convert.ToDateTime(drDet["DDDEnd"]).ToString("MM/dd/yy")),
                        dddUnit = Convert.ToString(drDet["DDDUnits"]),
                        claimId = Convert.ToInt64(drDet["ClaimID"] == DBNull.Value ? 0 : drDet["ClaimID"]),
                        comments = comments,
                        InsurancePolicyId = Convert.ToInt32(drDet["InsurancePolicyId"] == DBNull.Value ? 0 : drDet["InsurancePolicyId"]),
                        ClaimStatusID = Convert.ToInt32(drDet["ClaimStatusID"] == DBNull.Value ? 100 : drDet["ClaimStatusID"]),
                        Client = Convert.ToString(drDet["Client"]),
                        paymentId = Convert.ToInt32(drDet["PaymentId"] == DBNull.Value ? 0 : drDet["PaymentId"]),
                        PresStart = (drDet["PresStart"] == DBNull.Value ? null : Convert.ToDateTime(drDet["PresStart"]).ToString("MM/dd/yy")),
                        PresEnd = (drDet["PresEnd"] == DBNull.Value ? null : Convert.ToDateTime(drDet["PresEnd"]).ToString("MM/dd/yy")),
                        PAuthStart = (drDet["PAuthStart"] == DBNull.Value ? "" : Convert.ToDateTime(drDet["PAuthStart"]).ToString("MM/dd/yy")),
                        PAuthEnd = (drDet["PAuthEnd"] == DBNull.Value ? "" : Convert.ToDateTime(drDet["PAuthEnd"]).ToString("MM/dd/yy")),
                        serviceId = Convert.ToInt32(drDet["serviceId"] == DBNull.Value ? 0 : drDet["serviceId"]),
                        staffId = Convert.ToInt32(drDet["prID"] == DBNull.Value ? 0 : drDet["prID"]),
                        clientSessionTherapyID = Convert.ToInt32(drDet["ClientSessionTherapyID"] == DBNull.Value ? 0 : drDet["ClientSessionTherapyID"]),
                        clientChartFileId = Convert.ToInt32(drDet["chartId"] == DBNull.Value ? 0 : drDet["chartId"]),
                        clientChartFileExtension = Convert.ToString(drDet["ChartFileExtension"] == DBNull.Value ? "" : drDet["ChartFileExtension"]),
                        clientChartFileName = Convert.ToString(drDet["ChartFileName"] == DBNull.Value ? "" : drDet["ChartFileName"]),
                        progressReportFileId = Convert.ToInt32(drDet["progressTherapyId"] == DBNull.Value ? 0 : drDet["progressTherapyId"]),
                        progressReportFileExtension = Convert.ToString(drDet["ProgressFileExtension"] == DBNull.Value ? "" : drDet["ProgressFileExtension"]),
                        progressReportFileName = Convert.ToString(drDet["ProgressFileName"] == DBNull.Value ? "" : drDet["ProgressFileName"]),
                        noteTherapyFileId = Convert.ToInt32(drDet["clTherapyNoteId"] == DBNull.Value ? 0 : drDet["clTherapyNoteId"]),
                        noteTherapyFileExtension = Convert.ToString(drDet["NoteExtension"] == DBNull.Value ? "" : drDet["NoteExtension"]),
                        noteTherapyFileName = Convert.ToString(drDet["NoteFileName"] == DBNull.Value ? "" : drDet["NoteFileName"]),
                        insurancePriority = Convert.ToInt32(drDet["InsurancePriorityId"] == DBNull.Value ? 0 : drDet["InsurancePriorityId"]),
                        therapistSupervisor = Convert.ToString(drDet["TherapistSupervisor"] == DBNull.Value || Convert.ToString(drDet["TherapistSupervisor"]) == "0" ? "" : drDet["TherapistSupervisor"]),
                        SessionTherapyStatusID = Convert.ToInt32(drDet["SessionTherapyStatusID"] == DBNull.Value ? 0 : drDet["SessionTherapyStatusID"]),
                        DenialReason = Convert.ToString(drDet["DenialReason"] == DBNull.Value ? "" : drDet["DenialReason"]),
                        DeductibleInd = Convert.ToInt32(drDet["DeductibleInd"] == DBNull.Value ? 0 : drDet["DeductibleInd"]),
                        ClaimAgingBucket = Convert.ToString(drDet["AgingBucket"]),
                    };
                    cdi.DenialReasonText = r.DenialReasonList.Where(d => d.id == cdi.DenialReason.Trim()).Select(d => d.id + ": " + d.name).FirstOrDefault();
                    cdi.allowedAmount = Math.Round(Convert.ToDecimal(drDet["AllowedAmt"] == DBNull.Value ? 0 : drDet["AllowedAmt"]), 2);
                    cdi.coInsuranceAmount = Math.Round(Convert.ToDecimal(drDet["CoInsuranceAmt"] == DBNull.Value ? 0 : drDet["CoInsuranceAmt"]), 2);
                    if (!downLoadCategories.Keys.Contains(cdi.claimId))
                    {
                        bool anyFile = false;
                        List<SelectListItem> downLoadCategory = new List<SelectListItem>();
                        if (!string.IsNullOrEmpty(policyId))
                        {
                            downLoadCategory.Add(new SelectListItem { Selected = false, Text = "Download HCFA", Value = "HCFA" }); anyFile = true;
                        }
                        if (cdi.clientChartFileId > 0)
                        {
                            downLoadCategory.Add(new SelectListItem { Selected = false, Text = "Prescription", Value = "Prescription" });//cdi.clientChartFileName + "." + cdi.clientChartFileExtension
                            anyFile = true;
                        }
                        if (cdi.progressReportFileId > 0)
                        {
                            downLoadCategory.Add(new SelectListItem { Selected = false, Text = "Progress Report", Value = "ProgressReport" });
                            anyFile = true;
                        }
                        if (cdi.noteTherapyFileId > 0)
                        {
                            downLoadCategory.Add(new SelectListItem { Selected = false, Text = "Client Notes", Value = "ClientNotes" });
                            anyFile = true;
                        }
                        //downLoadCategory.Add(new SelectListItem { Selected = false, Text = "Evaluation Report", Value = "Evaluation Report" });
                        //downLoadCategory.Add(new SelectListItem { Selected = false, Text = "2019 Q3 Quaterly Report", Value = "2019 Q3 Quaterly Report" });
                        //downLoadCategory.Add(new SelectListItem { Selected = false, Text = "Session Document", Value = "Session Document" });
                        if (anyFile)
                            downLoadCategory.Add(new SelectListItem { Selected = false, Text = "Download All", Value = "99" });

                        downLoadCategories.Add(cdi.claimId, downLoadCategory);
                    }

                    if (rc.dos != dosOld || dosOld == DateTime.MinValue)
                    {
                        r.ClientDetailsList.Add(rc);
                        iOld = i;
                        r.ClientDetailsList[r.ClientDetailsList.Count - 1].ClientDetailsInfoList = new List<ClientDetailInfo>();
                    }
                    r.ClientDetailsList[r.ClientDetailsList.Count - 1].ClientDetailsInfoList.Add(cdi);
                    dosOld = rc.dos;

                }

                r.companyInsuranceList = dsDccMain.Tables[0].Rows.Cast<DataRow>().Select(spR => new Option()
                {
                    value = Convert.ToString(spR["InsuranceCompanyId"]),
                    name = (string)spR["Name"]
                }).ToList();

                DataTable dtPayments = (dsPayment.Tables[0].DefaultView).ToTable();
                r.ClaimPaymentList = new List<ClaimPayment>();
                for (int i = 0; i < dtPayments.Rows.Count; i++)
                {

                    DataRow drPayment = dtPayments.Rows[i];//ds.Tables[0].Rows[i]; Code Change
                    ClaimPayment cdi = new ClaimPayment();
                    cdi.DOS = (Convert.ToDateTime(drPayment["DOS"] == DBNull.Value ? default(DateTime) : drPayment["DOS"])).ToString("MM/dd/yy");
                    cdi.Code = Convert.ToString(drPayment["Code"]);
                    cdi.Amount = Math.Round(Convert.ToDecimal(drPayment["Amount"] == DBNull.Value ? 0 : drPayment["Amount"]), 2);
                    cdi.ClaimId = Convert.ToInt32(drPayment["ClaimId"] == DBNull.Value ? 0 : drPayment["ClaimId"]);
                    cdi.GovernmentProgramId = Convert.ToInt32(drPayment["GovernmentProgramId"] == DBNull.Value ? 0 : drPayment["GovernmentProgramId"]);
                    cdi.InsurancePolicyId = Convert.ToInt32(drPayment["InsurancePolicyId"] == DBNull.Value ? 0 : drPayment["InsurancePolicyId"]);
                    cdi.InsuredIdNo = Convert.ToString(drPayment["InsuredIdNo"]);
                    cdi.InsuranceCompanyId = Convert.ToInt32(drPayment["InsuranceCompanyId"] == DBNull.Value ? 0 : drPayment["InsuranceCompanyId"]);
                    cdi.IsDenial = Convert.ToBoolean(drPayment["IsDenial"]);
                    cdi.PaymentId = Convert.ToInt32(drPayment["PaymentId"] == DBNull.Value ? 0 : drPayment["PaymentId"]);
                    cdi.Payer = Convert.ToString(drPayment["Payer"]);
                    cdi.Notes = Convert.ToString(drPayment["Notes"]);
                    cdi.ReceivedAt = (Convert.ToDateTime(drPayment["ReceivedAt"] == DBNull.Value ? default(DateTime) : drPayment["ReceivedAt"]));//.ToString("MM/dd/yy");
                    cdi.PaymentTypeId = Convert.ToInt32(drPayment["PaymentTypeId"] == DBNull.Value ? 0 : drPayment["PaymentTypeId"]);
                    cdi.VoidedAt = (Convert.ToDateTime(drPayment["VoidedAt"] == DBNull.Value ? default(DateTime) : drPayment["VoidedAt"]));//.ToString("MM/dd/yy");
                    cdi.DenialReasonId = Convert.ToInt32(drPayment["DenialReasonId"] == DBNull.Value ? 0 : drPayment["DenialReasonId"]);
                    cdi.ReasonText = Convert.ToString(drPayment["ReasonText"]);
                    cdi.StaffId = Convert.ToInt32(drPayment["StaffId"] == DBNull.Value ? 0 : drPayment["StaffId"]);
                    cdi.OABatchID = Convert.ToInt64(drPayment["BatchID"] == DBNull.Value ? 0 : drPayment["BatchID"]);
                    cdi.HCFAFileId = Convert.ToInt32(drPayment["HCFAFileId"] == DBNull.Value ? 0 : drPayment["HCFAFileId"]);
                    r.ClaimPaymentList.Add(cdi);
                    //if (!downLoadCategories.Keys.Contains(cdi.ClaimId))
                    //{
                    //    List<SelectListItem> downLoadCategory = new List<SelectListItem>();
                    //    if (cdi.OABatchID != 0) downLoadCategory.Add(new SelectListItem { Selected = false, Text = "Download HCFA", Value = cdi.HCFAFileId.ToString() });
                    //    downLoadCategory.Add(new SelectListItem { Selected = false, Text = "Evaluation Report", Value = "Evaluation Report" });
                    //    downLoadCategory.Add(new SelectListItem { Selected = false, Text = "2019 Q3 Quaterly Report", Value = "2019 Q3 Quaterly Report" });
                    //    downLoadCategory.Add(new SelectListItem { Selected = false, Text = "Session Document", Value = "Session Document" });
                    //    downLoadCategory.Add(new SelectListItem { Selected = false, Text = "Download All", Value = "99" });

                    //    downLoadCategories.Add(cdi.ClaimId, downLoadCategory);
                    //}
                }

                DataSet ds1 = await getClaimStatusList(claimStatusId2);

                r.claimStatusList = ds1.Tables[0].Rows.Cast<DataRow>().Select(spR => new ClaimsStatus()
                {
                    claimstatusid = int.Parse(spR["ClaimStatusid"].ToString()),
                    name = spR["Name"] == DBNull.Value ? "" : (string)spR["Name"],
                    statusType = spR["statusType"].ToString()
                }).ToList();

                dsAmount = await getClaimsPaymentAmounts(clientId);
                r.OtherClaimPayments = dsAmount.Tables[0].Rows.Cast<DataRow>().Select(drPa => new PaymentAmount()
                {
                    allowedAmount = Convert.ToDecimal(drPa["AllowedAmount"] == DBNull.Value ? 0 : drPa["AllowedAmount"]),
                    billedamount = Convert.ToDecimal(drPa["BilledAmount"] == DBNull.Value ? 0 : drPa["BilledAmount"]),
                    claimId = Convert.ToInt64(drPa["ClaimID"] == DBNull.Value ? 0 : drPa["ClaimID"]),
                    clientSessionTherapyId = Convert.ToInt64(drPa["ClientSessionTherapyId"] == DBNull.Value ? 0 : drPa["ClientSessionTherapyId"]),
                    //claimStatusId = 1,
                    insurancePriority = Convert.ToInt32(drPa["InsurancePriorityId"] == DBNull.Value ? 0 : drPa["InsurancePriorityId"]),
                    coInsuranceAmount = Convert.ToDecimal(drPa["CoInsuranceAmount"] == DBNull.Value ? 0 : drPa["CoInsuranceAmount"]),
                    paidAmount = Convert.ToDecimal(drPa["PaidAmount"] == DBNull.Value ? 0 : drPa["PaidAmount"]),
                    policyId = Convert.ToInt32(drPa["InsurancePolicyId"] == DBNull.Value ? 0 : drPa["InsurancePolicyId"]),
                    paymentDate = drPa["PaymentDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(drPa["PaymentDate"])
                }).ToList();
                // dummy data
                //r.OtherClaimPayments = new List<PaymentAmount>();
                //r.OtherClaimPayments.Add(new PaymentAmount() { allowedAmount = 100, billedamount = 250, claimId = 1, coInsuranceAmount = 15, insurancePriority = 1, paidAmount = 50, policyId = 31, clientSessionTherapyId = 1 });
                //r.OtherClaimPayments.Add(new PaymentAmount() { allowedAmount = 150, billedamount = 50, claimId = 1, coInsuranceAmount = 15, insurancePriority = 3, paidAmount = 35, policyId = 31, clientSessionTherapyId = 1 });
            }

            var genderType = from GenderTypeEnum e in Enum.GetValues(typeof(GenderTypeEnum))
                             select new
                             {
                                 ID = (int)e,
                                 Name = e.ToString()
                             };

            ViewBag.EnumList = new SelectList(genderType, "ID", "Name");
            ViewBag.downloadCategory = downLoadCategories;
            ViewBag.insuranceFlag = insuranceFlag;
            r.serviceList = LoadService(int.Parse(clientId));
            if (ds != null)
                ds.Dispose();
            return PartialView("ClaimDetails", r);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> GetValidClaimStatuses(int currentClaimStatusId)
        {
            DataSet ds1 = await getClaimStatusList(currentClaimStatusId);
            List<ClaimsStatus> statusList = new List<ClaimsStatus>();

            if (ds1.HasTables() && ds1.Tables[0].HasRows())
            {
                statusList = ds1.Tables[0].Rows.Cast<DataRow>().Select(spR => new ClaimsStatus()
                {
                    claimstatusid = int.Parse(spR["ClaimStatusid"].ToString()),
                    name = spR["Name"] == DBNull.Value ? "" : (string)spR["Name"],
                    statusType = spR["statusType"].ToString()
                }).ToList();
            }

            return Json(statusList);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> ApproveClaims(ClaimsApprove s)
        {
            ProviderInit r = new ProviderInit();
            DataSet ds = null;

            try
            {
                //logic to update all the claims of given provider in db
                //ds = await deleteClientChart(UserClaim.userLevel, UserClaim.prid, s.clsvId, s.chartId);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }

            if (ds != null)
                ds.Dispose();


            return Json(r);
        }

        public async Task<ActionResult> ClientInvoice()
        {
            ProviderInit r = new ProviderInit();
            // For Admin get current clients & clients that have been deactivated in the last 12 months
            DataSet ds = null;
            try
            {
                //alphabetic order
                //ds = await getClientInvoice(UserClaim.userLevel, UserClaim.prid);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            //EmptyView r = new EmptyView();
            setViewModelBase((ViewModelBase)r);
            return View("ClientInvoice", r);
        }
        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public JsonResult UpdateClaim(int ClientID, long ClaimId, int InsurancePolicyId, int ServiceId, decimal? AmountBilled = null, decimal? Amount = null,
             decimal? CoInsuranceAmount = null, decimal? AllowedAmount = null, DateTime? DDDstdt = null, DateTime? DDDeddt = null,
            decimal? DDDau = null, string Comments = null, int? SelfPayStatus = null, int ClaimStatusId = -1, DateTime? PaymentDate = null)
        {
            ProviderInit r = new ProviderInit();
            JsonResult result = new JsonResult();

            try
            {
                result = updateClaims(ClientID, ClaimId, InsurancePolicyId, ServiceId, AmountBilled, Amount, CoInsuranceAmount, AllowedAmount, DDDstdt, DDDeddt,
                DDDau, Comments, SelfPayStatus, ClaimStatusId, PaymentDate);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
                return Json(new { success = false });
            }
            // return Json(r);
            //return PartialView("ClientDetails", r);
            return result;
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public string UpdateClaimStatus(long ClaimId, int ClaimStatusId)
        {
            ProviderInit r = new ProviderInit();

            try
            {
                updateClaimStatus(ClaimId, ClaimStatusId);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            return "";
        }

        private JsonResult updateClaims(int ClientID, long ClaimId, int InsurancePolicyId, int ServiceId, decimal? AmountBilled = null, decimal? Amount = null,
            decimal? CoInsuranceAmount = null, decimal? AllowedAmount = null, DateTime? DDDstdt = null, DateTime? DDDeddt = null,
            decimal? DDDau = null, string Comments = null, int? SelfPayStatus = null, int claimStatusId = -1, DateTime? paymentDate = null)
        {
            bool bSuccess = true;

            SqlConnection cn = new SqlConnection(UserClaim.conStr);
            cn.Open();

            SqlCommand cmd = new SqlCommand("sp_BillingUpdateBillingDetails", cn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ClientID", ClientID);
            cmd.Parameters.AddWithValue("@ClaimId", ClaimId);
            cmd.Parameters.AddWithValue("@InsurancePolicyId", InsurancePolicyId);
            cmd.Parameters.AddWithValue("@ServiceId", ServiceId);
            cmd.Parameters.AddWithValue("@AmountBilled", AmountBilled);
            cmd.Parameters.AddWithValue("@Amount", Amount);
            cmd.Parameters.AddWithValue("@CoInsuranceAmount", CoInsuranceAmount);
            cmd.Parameters.AddWithValue("@AllowedAmount", AllowedAmount);
            cmd.Parameters.AddWithValue("@DDDstdt", DDDstdt);
            cmd.Parameters.AddWithValue("@DDDeddt", DDDeddt);
            cmd.Parameters.AddWithValue("@DDDau", DDDau);
            cmd.Parameters.AddWithValue("@Comments", Comments);
            cmd.Parameters.AddWithValue("@ClaimStatusId", claimStatusId);
            cmd.Parameters.AddWithValue("@SelfPayStatus", SelfPayStatus);
            cmd.Parameters.AddWithValue("@StaffId", UserClaim.prid);
            cmd.Parameters.AddWithValue("@PaymentDate", paymentDate);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                bSuccess = false;
            }
            finally
            {
                cmd.Dispose();
                cn.Close();
                cn.Dispose();
            }

            return Json(new { success = bSuccess });
        }
        private string updateClaimStatus(long ClaimId, int claimStatusId)
        {
            SqlConnection cn = new SqlConnection(UserClaim.conStr);
            cn.Open();
            SqlCommand cmd = new SqlCommand("sp_BillingClaimStatus", cn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ClaimId", ClaimId);
            cmd.Parameters.AddWithValue("@ClaimStatusId", claimStatusId);
            cmd.ExecuteNonQuery();
            return "";
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> GetInsurancePolicy(string InsurancePolicyId)
        {
            ProviderInit r = new ProviderInit();
            r.InsurancePolicy = new InsurancePolicy();
            DataSet ds = new DataSet();
            try
            {
                ds = await getInsurancePolicy(InsurancePolicyId);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            r.InsurancePolicy.Company = ds.Tables[0].Rows[0]["Company"].ToString();
            r.InsurancePolicy.PolicyNumber = ds.Tables[0].Rows[0]["PolicyNumber"].ToString();
            r.InsurancePolicy.FirstName = ds.Tables[0].Rows[0]["FirstName"].ToString();
            r.InsurancePolicy.LastName = ds.Tables[0].Rows[0]["LastName"].ToString();
            r.InsurancePolicy.InsuredDoB = Convert.ToDateTime(ds.Tables[0].Rows[0]["InsuredDoB"]);
            r.InsurancePolicy.Gender = ds.Tables[0].Rows[0]["Gender"].ToString();
            r.InsurancePolicy.MCID = ds.Tables[0].Rows[0]["MCID"].ToString();
            r.InsurancePolicy.PatientIdNo = ds.Tables[0].Rows[0]["PatientIdNo"].ToString();
            r.InsurancePolicy.Relationship = ds.Tables[0].Rows[0]["Relationship"].ToString();
            //r.InsurancePolicy.IsPrimary = Convert.ToBoolean(ds.Tables[0].Rows[0]["IsPrimary"]);
            r.InsurancePolicy.StartDate = Convert.ToDateTime(ds.Tables[0].Rows[0]["StartDate"]);
            r.InsurancePolicy.EndDate = Convert.ToDateTime(ds.Tables[0].Rows[0]["EndDate"]);

            // return Json(r.InsurancePolicy);
            return PartialView("InsurancePolicyModal", r);

        }
        private Task<DataSet> getInsurancePolicy(string InsurancePolicyId)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_BillingGetInsuranceDetails", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@InsurancePolicyId", InsurancePolicyId);
                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
        }

        // Get Denial Reasons List //
        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> DenialReasonList()
        {
            ProviderInit r = new ProviderInit();
            r.DenialReasonList = new List<DenialReason>();
            DataSet ds = new DataSet();
            try
            {
                ds = await getDenialReasonList();
                r.DenialReasonList = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new DenialReason()
                {
                    // id = Convert.ToInt32(spR["id"] == DBNull.Value ? 0 : spR["id"]),
                    id = ((string)spR["id"]).Trim(),
                    name = (string)spR["name"]
                }).OrderBy(d => d.id).ToList();
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            return Json(r.DenialReasonList);

        }
        private Task<DataSet> getDenialReasonList()
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_DenialReasonGetList", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
        }

        // Get Denial Reasons List //
        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public ActionResult UpdateDeductible(long ClaimId, long PaymentId, decimal Amount, string ReasonCode, int clientId)
        {
            ProviderInit r = new ProviderInit();
            r.DenialReasonList = new List<DenialReason>();
            string msg = "";

            try
            {
                msg = updateDeductible(ClaimId, PaymentId, Amount, ReasonCode, clientId);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }

            return Json(new { message = msg });

        }
        private string updateDeductible(long claimId, long paymentId, decimal amount, string reasonCode, int clientId)
        {

            string msg = "Success";
            try
            {
                SqlConnection cn = new SqlConnection(UserClaim.conStr);
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_BillingUpdateDeductible", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ClaimId", claimId);
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                cmd.Parameters.AddWithValue("@PaymentId", paymentId);
                cmd.Parameters.AddWithValue("@Amount", amount);
                cmd.Parameters.AddWithValue("@DenialReason", reasonCode);
                cmd.Parameters.AddWithValue("@StaffId", UserClaim.prid);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return msg;
        }

        // Get Denial Reasons List //
        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public ActionResult UpdateCleanDenial(long ClaimId, long PaymentId, decimal Amount, string ReasonCode, string StartDate, long ClientId)
        {
            ProviderInit r = new ProviderInit();
            r.DenialReasonList = new List<DenialReason>();
            string msg = "";

            try
            {
                msg = updateCleanDenial(ClaimId, PaymentId, Amount, ReasonCode, StartDate, ClientId);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            return Json(new { message = msg });

        }
        private string updateCleanDenial(long claimId, long paymentId, decimal amount, string reasonCode, string startDate, long clientId)
        {

            string msg = "Success";
            try
            {
                SqlConnection cn = new SqlConnection(UserClaim.conStr);
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_BillingUpdateDenialPayment", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ClaimId", claimId);
                cmd.Parameters.AddWithValue("@PaymentId", paymentId);
                cmd.Parameters.AddWithValue("@Amount", amount);
                cmd.Parameters.AddWithValue("@DenialReason", reasonCode);
                cmd.Parameters.AddWithValue("@StaffId", UserClaim.prid);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("sp_UpdateClaimStatusToPendingWaiver", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ClientID", clientId);
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                cmd.Parameters.AddWithValue("@claimID", claimId);
                cmd.Parameters.AddWithValue("@Status", (int)ClaimStatusEnum.PendingWaiver);
                da = new SqlDataAdapter(cmd);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return msg;
        }

        // Get Denial Reasons List //
        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public ActionResult UpdatePriorAuth(int claimId)
        {
            ProviderInit r = new ProviderInit();
            string msg = "";

            try
            {
                msg = updatePriorAuth(claimId);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            return Json(new { message = msg });

        }
        private string updatePriorAuth(int claimId)
        {

            string msg = "Success";
            try
            {
                SqlConnection cn = new SqlConnection(UserClaim.conStr);
                cn.Open();
                // SqlCommand cmd = new SqlCommand("sp_BillingInsertAuthAlert", cn)
                SqlCommand cmd = new SqlCommand("sp_BillingInsertPriorAuthAlert", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@claimId", claimId);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return msg;
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public ActionResult UpdateBatchPendingWaiver(string claimIds)
        {
            ProviderInit r = new ProviderInit();
            string msg = "";

            try
            {
                msg = updateBatchPendingWaiver(claimIds);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            return Json(new { message = msg });

        }
        private string updateBatchPendingWaiver(string claimIds)
        {

            string msg = "Success";
            try
            {
                SqlConnection cn = new SqlConnection(UserClaim.conStr);
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_BillingUpdatePendingWaiver", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ClaimIds", claimIds);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return msg;
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public ActionResult UpdateBatchApproveAll(string claimIds, int status)
        {
            ProviderInit r = new ProviderInit();
            string msg = "";

            try
            {
                msg = updateBatchApproveAll(claimIds);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            return Json(new { message = msg });

        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public ActionResult UpdateBatchStatus(string claimIds, int status, bool isHCFA = false)
        {
            ProviderInit r = new ProviderInit();
            string msg = "";

            try
            {
                msg = updateBatchApproveAll(claimIds, status, isHCFA);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            return Json(new { message = msg });
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public ActionResult UpdateBatchApproveAllClients(string clientIds, string payerID, bool selfpay, int claimStatus, int tierId = -1)
        {
            ProviderInit r = new ProviderInit();
            string msg = "";
            try
            {
                string claimIds = getClaimListByClients(clientIds, payerID, selfpay, claimStatus, tierId);
                msg = updateSubStatusBatchApproveAll(claimIds);
                claimIds = getClaimListByClients(clientIds, payerID, selfpay, 2, tierId);
                msg = updateBatchApproveAll(claimIds);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            return Json(new { message = msg });

        }

        private string updateSubStatusBatchApproveAll(string claimIds)
        {

            DataTable claims = new DataTable();
            claims.Columns.Add("claimId");
            foreach (string id in claimIds.Split(','))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    DataRow dr = claims.NewRow();
                    dr["claimId"] = int.Parse(id);
                    claims.Rows.Add(dr);
                }
            }
            string msg = "Success";
            try
            {
                SqlConnection cn = new SqlConnection(UserClaim.conStr);
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_SetTherapySubStatus", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Claims", claims);
                cmd.Parameters.AddWithValue("@SubStatus", 1);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return msg;
        }

        [HttpGet]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> GetClaimPayments(string claimIds)
        {
            ProviderInit r = new ProviderInit();
            DataSet ds = new DataSet();
            try
            {
                ds = await getClaimPayments(claimIds);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            DataSet dsCom = await getInsuranceList();
            r.providerList = dsCom.Tables[0].Rows.Cast<DataRow>().Select(spR => new Provider()
            {
                payerId = spR["PayerId"] == DBNull.Value ? "" : (string)spR["PayerId"],
                name = spR["Name"] == DBNull.Value ? "" : (string)spR["Name"],
                insuranceCompanyId = Convert.ToInt32(spR["InsuranceCompanyId"] == DBNull.Value ? "0" : spR["InsuranceCompanyId"])
            }).ToList();

            r.ClaimPaymentList = new List<ClaimPayment>();
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    ClaimPayment cp = new ClaimPayment()
                    {
                        AllowedAmount = Convert.ToDecimal(dr["AllowedAmount"] == DBNull.Value ? 0 : dr["AllowedAmount"]),
                        Amount = Convert.ToDecimal(dr["AmountDue"] == DBNull.Value ? 0 : dr["AmountDue"]),
                        ClaimId = Convert.ToInt32(dr["ClaimId"] == DBNull.Value ? 0 : dr["ClaimId"]),
                        Code = Convert.ToString(dr["Code"] == DBNull.Value ? "" : dr["Code"]),
                        CoInsAmount = Convert.ToDecimal(dr["CoInsuranceAmount"] == DBNull.Value ? 0 : dr["CoInsuranceAmount"]),
                        DOS = Convert.ToDateTime(dr["DOS"] == DBNull.Value ? "" : dr["DOS"]).ToShortDateString(),
                        PaidAmount = Convert.ToDecimal(dr["Amount"] == DBNull.Value ? 0 : dr["Amount"]),
                        PaymentId = Convert.ToInt32(dr["PaymentId"] == DBNull.Value ? 0 : dr["PaymentId"]),
                        StaffId = Convert.ToInt32(dr["StaffId"] == DBNull.Value ? 0 : dr["StaffId"]),
                        VoidedAt = Convert.ToDateTime(dr["VOIDEDAT"] == DBNull.Value ? DateTime.MinValue : dr["VOIDEDAT"]),
                        InsuranceCompanyId = Convert.ToInt32(dr["InsuranceCompanyId"] == DBNull.Value ? 0 : dr["InsuranceCompanyId"]),
                        InsuranceCompany = r.providerList.Where(i => i.insuranceCompanyId == Convert.ToInt32(dr["InsuranceCompanyId"] == DBNull.Value ? 0 : dr["InsuranceCompanyId"])).Select(i => i.name).FirstOrDefault(),
                        PaymentDate = Convert.ToDateTime(dr["PaymentDate"] == DBNull.Value ? DateTime.MinValue : dr["PaymentDate"]),
                        DeductibleInd = Convert.ToInt32(dr["DeductibleInd"]),
                        DeductibleAmount = Convert.ToDecimal(dr["DeductibleAmt"] == DBNull.Value ? 0 : dr["DeductibleAmt"]),
                        DeductibleReasonCode = Convert.ToInt32(dr["DeductibleReasonCode"] == DBNull.Value ? 0 : dr["DeductibleReasonCode"]),
                        ReasonText = Convert.ToString(dr["ReasonText"])
                    };
                    r.ClaimPaymentList.Add(cp);
                }
            }
            return PartialView("Payments", r);
        }
        private Task<DataSet> getClaimPayments(string claimIds)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    DataTable claims = new DataTable();
                    claims.Columns.Add("claimId");
                    foreach (string id in claimIds.Split(','))
                    {
                        if (!string.IsNullOrEmpty(id))
                        {
                            DataRow dr = claims.NewRow();
                            dr["claimId"] = int.Parse(id);
                            claims.Rows.Add(dr);
                        }
                    }
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("sp_GetClaimPayments", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@Claims", claims);
                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
        }

        private string getClaimListByClients(string clientIds, string payerID, bool selfpay, int claimStatus, int tierId = -1)
        {
            DataTable clients = new DataTable();
            clients.Columns.Add("clientId");
            foreach (string id in clientIds.Split(','))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    DataRow dr = clients.NewRow();
                    dr["clientId"] = int.Parse(id);
                    clients.Rows.Add(dr);
                }
            }
            List<int> claimids = new List<int>();
            var str = "";
            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_BillingGetClaims", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                if (!selfpay) cmd.Parameters.AddWithValue("@InsurancePolicyId", payerID);
                cmd.Parameters.AddWithValue("@TierId", tierId);
                cmd.Parameters.AddWithValue("@Status", claimStatus);
                SqlParameter clntParam = cmd.Parameters.AddWithValue("@Clients", clients);
                clntParam.SqlDbType = SqlDbType.Structured;
                clntParam.TypeName = "dbo.ClientIds";
                DataSet ds = new DataSet();
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                //claimids = ds.Tables[0].AsEnumerable().Select(r => int.Parse(r.Field<int>("Claimid").ToString())).ToList();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    claimids.Add(int.Parse(dr["Claimid"].ToString()));
                }
                str = String.Join(",", claimids);

            }
            return str;
        }

        private string updateBatchApproveAll(string claimIds, int status = 12, bool isHCFA = false)
        {

            string msg = "Success";
            try
            {
                SqlConnection cn = new SqlConnection(UserClaim.conStr);
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_BillingUpdateBatchApproveAll", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ClaimIds", claimIds);
                cmd.Parameters.AddWithValue("@ClaimStatusId", status);

                if (isHCFA)
                {
                    cmd.Parameters.AddWithValue("@HCFA_Flag", 1);
                }

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return msg;
        }
        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public ActionResult UpdateBatchDenialClaims(string claimIds, string ReasonCode)
        {
            ProviderInit r = new ProviderInit();
            string msg = "";

            try
            {
                msg = updateBatchDenialClaims(claimIds, ReasonCode);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            return Json(new { message = msg });

        }
        private string updateBatchDenialClaims(string claimIds, string ReasonCode)
        {

            string msg = "Success";
            try
            {
                SqlConnection cn = new SqlConnection(UserClaim.conStr);
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_BillingUpdateBatchDenialPayment", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ClaimIds", claimIds);
                cmd.Parameters.AddWithValue("@DenialReason", ReasonCode);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return msg;
        }

        //[HttpPost]
        //[ValidateJsonAntiForgeryToken]
        //[Authorize]
        //public ActionResult Communication(string email, string subject, string message, string phone, bool ismail, string filepath)
        //{
        //    ProviderInit r = new ProviderInit();
        //    string msg = "";

        //    try
        //    {
        //        msg = communication(email, subject, message, phone, ismail, null);
        //    }
        //    catch (Exception ex)
        //    {
        //        r.er.code = 1;
        //        //msg = ex.Message;

        //    }
        //    return Json(new { message = msg });

        //}
        public string SendSMS(string message, string phone)
        {
            string msg = "";
            ProviderInit r = new ProviderInit();
            //Helpers.EmailHelper em = new Helpers.EmailHelper();
            try
            {
                //msg = em.communication("", "", message, phone, false, null);
                msg = CommunicationHelper.SendSMS(phone, message);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                msg = ex.Message;

            }
            return msg;

        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public ActionResult VoidPayments(string comment, int claimid, int paymentid, int staffid, int insuranceCompanyId)
        {
            ProviderInit r = new ProviderInit();
            string msg = "";

            try
            {
                msg = updateVoidPayments(comment, claimid, paymentid, staffid, insuranceCompanyId);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            return Json(new { message = msg });

        }
        private string updateVoidPayments(string comment, int claimId, int paymentId, int staffId, int insuranceCompanyId)
        {

            string msg = "Success";
            try
            {
                SqlConnection cn = new SqlConnection(UserClaim.conStr);
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ClaimPaymentUpdate", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@StaffId", staffId);
                cmd.Parameters.AddWithValue("@PaymentId", paymentId);
                cmd.Parameters.AddWithValue("@ClaimId", claimId);
                cmd.Parameters.AddWithValue("@Reason", comment);
                cmd.Parameters.AddWithValue("@InsuranceCompanyId", insuranceCompanyId);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return msg;
        }

        // function to update denail error //
        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public ActionResult DenialError(int claimid, int staffid, int clientid)
        {
            ProviderInit r = new ProviderInit();
            string msg = "";
            try
            {
                msg = updateDenialErrors(claimid, staffid, clientid);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            return Json(new { message = msg });

        }
        private string updateDenialErrors(int claimId, int staffId, int clientId)
        {
            string msg = "Success";
            try
            {
                SqlConnection cn = new SqlConnection(UserClaim.conStr);
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_BillingDenialError", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ClaimId", claimId);
                cmd.Parameters.AddWithValue("@StaffId", staffId);
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return msg;
        }

        [Authorize]
        public async Task<ActionResult> Communication()
        {
            ProviderInit r = new ProviderInit();
            // For Admin get current clients & clients that have been deactivated in the last 12 months
            DataSet ds = null;
            DataSet ds1 = null;
            DataSet ds2 = null;
            try
            {
                //alphabetic order
                ds = await getInsuranceList();
                ds1 = await getClaimStatusList();
                ds2 = await getGovernmentProgramList();

            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            r.providerList = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new Provider()
            {
                payerId = spR["PayerId"] == DBNull.Value ? "" : (string)spR["PayerId"],
                name = spR["Name"] == DBNull.Value ? "" : (string)spR["Name"],
                //line1 = (string)spR["Line1"],
                //line2 = (string)spR["Line2"],
                //city = (string)spR["City"],
                //state = (string)spR["State"],
                //postalCode = (string)spR["PostalCode"]
            }).ToList();

            DataTable dt = getInsuranceCompanyIds().Result;
            r.GovernmentPrograms = ds2.Tables[0].Rows.Cast<DataRow>().Select(spR => new GovernmentProgram()
            {
                Id = int.Parse(spR["Id"] == DBNull.Value ? "0" : spR["Id"].ToString()),
                Name = spR["Name"] == DBNull.Value ? "" : (string)spR["Name"],
                InsuranceID = int.Parse(dt.Rows[0][0] == DBNull.Value ? "0" : dt.Rows[0][0].ToString())
            }).ToList();


            r.claimStatusList = ds1.Tables[0].Rows.Cast<DataRow>().Select(spR => new ClaimsStatus()
            {
                claimstatusid = int.Parse(spR["ClaimStatusid"].ToString()),
                name = spR["Name"] == DBNull.Value ? "" : (string)spR["Name"],

            }).ToList();

            //EmptyView r = new EmptyView();
            setViewModelBase((ViewModelBase)r);
            return View("Communication", r);
        }
        [Authorize]
        [HttpPost]
        //[ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SendEmail()
        {
            ProviderInit r = new ProviderInit();
            string msg = "";

            //Helpers.EmailHelper em = new Helpers.EmailHelper();
            try
            {
                HttpPostedFileBase file = null;
                if (Request.Files != null && Request.Files.Count > 0)
                {
                    file = Request.Files[0];
                }
                //msg = em.communication(Request.Form["email"], Request.Form["subject"], Request.Form["message"], "", true, file);
                msg = CommunicationHelper.SendEmail(Request.Form["email"], Request.Form["subject"], Request.Form["message"], file);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                //msg = ex.Message;
                r.er.msg = ex.Message;
            }
            return View("Communication", r);
        }
        //sp_GetBillingInsuranceCompanies

        public List<BillingInsuranceCompanyDTO> GetBillingInsuranceCompanies()
        {
            var toReturn = new List<BillingInsuranceCompanyDTO>();
            var result = new DataTable();

            using (SqlConnection connection = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand sqlCommand = new SqlCommand("sp_GetBillingInsuranceCompanies", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                sqlHelper.ExecuteSqlDataAdapter(sqlCommand, result);

            }

            if (result.HasRows())
            {
                toReturn = result.Rows.Cast<DataRow>().Select(x => new BillingInsuranceCompanyDTO()
                {
                    InsuranceCompanyId = int.Parse(x["InsuranceCompanyId"] == DBNull.Value ? "0" : x["InsuranceCompanyId"].ToString()),
                    ExcludeRenderer = x.GetValueOrDefault<bool>("ExcludeRenderer"),

                }).ToList();
            }
            SetCompanyDetails(ref toReturn);
            return toReturn;
        }
        public List<DiagnosisCodeDTO> GetDiagnosisCodes(int clientId)
        {
            var toReturn = new List<DiagnosisCodeDTO>();
            var result = new DataTable();

            using (SqlConnection connection = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand sqlCommand = new SqlCommand("sp_GetClientServiceDiagnosisCode", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                sqlCommand.Parameters.AddWithValue("@ClientId", clientId);
                sqlHelper.ExecuteSqlDataAdapter(sqlCommand, result);

            }

            if (result.HasRows())
            {
                toReturn = result.Rows.Cast<DataRow>().Select(x => new DiagnosisCodeDTO()
                {
                    Code = x.GetValueOrDefault<string>("Code"),
                    ClientServiceId = x.GetValueOrDefault<int>("ClientServiceId"),
                    InsurancePolicyId = x.GetValueOrDefault<int>("InsurancePolicyId"),
                }).ToList();
            }
            return toReturn;
        }
        public CompanyInfoDTO GetCompanyDetails(int companyId)
        {
            var toReturn = new List<CompanyInfoDTO>();
            var result = new DataTable();

            using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                SqlCommand sqlCommand = new SqlCommand("sp_GetCompanyById", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                sqlCommand.Parameters.AddWithValue("@coId", companyId);
                sqlHelper.ExecuteSqlDataAdapter(sqlCommand, result);

            }

            if (result.HasRows())
            {
                toReturn = result.Rows.Cast<DataRow>().Select(x => new CompanyInfoDTO()
                {
                    Name = x.GetValueOrDefault<string>("name"),
                    NPI = x.GetValueOrDefault<string>("NPI"),
                    Phone = x.GetValueOrDefault<string>("Phone"),
                    TaxId = x.GetValueOrDefault<string>("TaxId"),
                    CompanyCode = x.GetValueOrDefault<string>("pCode"),
                    NextDDDFileName = x.GetValueOrDefault<string>("NextDDDFileName"),
                    CompanyID = x.GetValueOrDefault<Int32>("coId"),
                    BlobStorageConnection = x.GetValueOrDefault<string>("blobStorageCn"),
                    Address = new AddressDTO()
                    {
                        City = x.GetValueOrDefault<string>("City"),
                        Line1 = x.GetValueOrDefault<string>("AddressLine1"),
                        PostalCode = x.GetValueOrDefault<string>("PostalCode"),
                        State = x.GetValueOrDefault<string>("state"),
                    },
                    ProvAhcccsId = x.GetValueOrDefault<string>("ProvAhcccsId"),
                    DDDPrefix = x.GetValueOrDefault<string>("pCode"),
                    //SkilledBilling Info						
                    SkilledBillingAddress = x.GetValueOrDefault<string>("SkilledBillingAddress"),
                    SkilledBillingContactName = x.GetValueOrDefault<string>("SkilledBillingContact"),
                    SkilledBillingEmail = x.GetValueOrDefault<string>("SkilledBillingEmail"),
                    SkilledBillingPhone = x.GetValueOrDefault<string>("SkilledBillingPhone"),
                    SkilledBillingZipCode = x.GetValueOrDefault<string>("SkilledBillingZip"),
                    SkilledBillingCity = x.GetValueOrDefault<string>("SkilledBillingCity"),
                    SkilledBillingState = x.GetValueOrDefault<string>("SkilledBillingState")
                }).ToList();
            }
            return toReturn.FirstOrDefault();
        }
        public List<ClientDTO> GetClients(string clientIds)
        {
            var toReturn = new List<ClientDTO>();
            var result = new DataTable();

            DataTable clients = new DataTable();
            clients.Columns.Add("clientId");
            foreach (string id in clientIds.Split(','))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    DataRow dr = clients.NewRow();
                    dr["clientId"] = int.Parse(id);
                    clients.Rows.Add(dr);
                }
            }
            using (SqlConnection connection = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand sqlCommand = new SqlCommand("sp_GetClients", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                sqlCommand.Parameters.AddWithValue("@ClientIds", clients);
                sqlHelper.ExecuteSqlDataAdapter(sqlCommand, result);

            }

            if (result.HasRows())
            {
                toReturn = result.Rows.Cast<DataRow>().Select(x => new ClientDTO()
                {
                    ClientId = int.Parse(x["clsvID"] == DBNull.Value ? "0" : x["clsvID"].ToString()),
                    DoB = x.GetValueOrDefault<DateTime>("dob"),
                    FirstName = x.GetValueOrDefault<string>("fn"),
                    LastName = x.GetValueOrDefault<string>("ln"),
                    MiddleName = string.Empty,
                    GenderId = int.Parse(x["Gender"] == DBNull.Value ? "0" : x["Gender"].ToString()),
                    Address = new AddressDTO()
                    {
                        City = x.GetValueOrDefault<string>("clientLocCity"),
                        Line1 = x["clientLocAd1"] == DBNull.Value ? "" : x["clientLocAd1"].ToString(),
                        Line2 = x["clientLocAd2"] == DBNull.Value ? "" : x["clientLocAd2"].ToString(),
                        PostalCode = x.GetValueOrDefault<string>("clientLocZip"),
                        State = x.GetValueOrDefault<string>("clientLocSt")
                    },
                    Policies = GetPolicies(int.Parse(x["clsvID"] == DBNull.Value ? "0" : x["clsvID"].ToString()), 0),
                    DiagnosisCodes = GetDiagnosisCodes(int.Parse(x["clsvID"] == DBNull.Value ? "0" : x["clsvID"].ToString())),
                }).ToList();
            }
            return toReturn;
        }
        public List<ClientClaimDTO> GetClaims(string clientIds, int companyId, int claimId = 0, string claimIds = null)
        {
            var toReturn = new List<ClientClaimDTO>();
            var result = new DataTable();

            if (!string.IsNullOrEmpty(claimIds))
            {
                using (SqlConnection connection = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_GetClaimsByClaimIDs", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlCommand.Parameters.AddWithValue("@IDs", claimIds);
                    sqlHelper.ExecuteSqlDataAdapter(sqlCommand, result);
                }
            }
            else
            {
                DataTable clients = new DataTable();
                clients.Columns.Add("clientId");
                foreach (string id in clientIds.Split(','))
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        DataRow dr = clients.NewRow();
                        dr["clientId"] = int.Parse(id);
                        clients.Rows.Add(dr);
                    }
                }

                using (SqlConnection connection = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_GetClaimsByClients", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlCommand.Parameters.AddWithValue("@ClientIds", clients);
                    sqlCommand.Parameters.AddWithValue("@CompanyId", companyId);
                    if (claimId != 0) sqlCommand.Parameters.AddWithValue("@ClaimId", claimId);
                    sqlHelper.ExecuteSqlDataAdapter(sqlCommand, result);

                }
            }
            if (result.HasRows())
            {
                toReturn = result.Rows.Cast<DataRow>().Select(x => new ClientClaimDTO()
                {
                    ClaimId = int.Parse(x["ClaimId"] == DBNull.Value ? "0" : x["ClaimId"].ToString()),
                    Claim = new ClaimDTO()
                    {
                        ClientId = int.Parse(x["ClientId"] == DBNull.Value ? "0" : x["ClientId"].ToString()),
                        ClaimDate = x.GetValueOrDefault<DateTime>("ClaimDate"),
                        ApproverUserId = int.Parse(x["ApprovingStaffId"] == DBNull.Value ? "0" : x["ApprovingStaffId"].ToString()),
                        StaffUserID = int.Parse(x["StaffId"] == DBNull.Value ? "0" : x["StaffId"].ToString()),
                        LocationTypeId = int.Parse(x["LocationTypeId"] == DBNull.Value ? "1" : x["LocationTypeId"].ToString()),
                        PlaceOfService = new AddressDTO
                        {
                            Line1 = x["POSad1"] != DBNull.Value && !string.IsNullOrEmpty(x["POSad1"].ToString()) ? x["POSad1"].ToString().Trim() : "",
                            City = x["POScty"] != DBNull.Value && !string.IsNullOrEmpty(x["POScty"].ToString()) ? x["POScty"].ToString().Trim() : "",
                            State = x["POSst"] != DBNull.Value && !string.IsNullOrEmpty(x["POSst"].ToString()) ? x["POSst"].ToString().Trim() : "",
                            PostalCode = x["POSzip"] != DBNull.Value && !string.IsNullOrEmpty(x["POSzip"].ToString()) ? x["POSzip"].ToString().Trim() : ""
                        },
                        StatusUpdatedAt = x.GetValueOrDefault<DateTime>("StatusUpdatedAt"),
                        Policy = GetPolicies(0, int.Parse(x["InsurancePolicyId"] == DBNull.Value ? "0" : x["InsurancePolicyId"].ToString())).FirstOrDefault(),
                        Payments = GetPayments(int.Parse(x["ClaimId"] == DBNull.Value ? "0" : x["ClaimId"].ToString())),
                        Appointments = GetAppoinments(int.Parse(x["ClaimId"] == DBNull.Value ? "0" : x["ClaimId"].ToString()), int.Parse(x["InsurancePolicyId"] == DBNull.Value ? "0" : x["InsurancePolicyId"].ToString())),
                        ClaimId = int.Parse(x["ClaimId"] == DBNull.Value ? "0" : x["ClaimId"].ToString()),
                        AmountDue = decimal.Parse(x["AmountDue"] == DBNull.Value ? "0" : x["AmountDue"].ToString()),
                        ClientGovtProgramId = x["ClientGovtProgramId"] == DBNull.Value ? "" : x["ClientGovtProgramId"].ToString().Trim(),
                        Comments = "",
                        IsNonTelehealth = x["teletherapy"] == DBNull.Value ? bool.Parse(x["teletherapy"].ToString()) : true,
                        ProviderNPI = x["npi"] == DBNull.Value || string.IsNullOrEmpty(x["npi"].ToString()) ? x["SupervisorNPI"].ToString().Trim() : x["npi"].ToString().Trim(),
                        ProviderStateMedicaid = x["ahcccsID"] == DBNull.Value || string.IsNullOrEmpty(x["ahcccsID"].ToString()) ? x["SupervisorAHCCCSID"].ToString().Trim() : x["ahcccsID"].ToString().Trim(),
                        StatusId = int.Parse(x["ClaimStatusId"] == DBNull.Value ? "0" : x["ClaimStatusId"].ToString()),
                        SubStatus = x["SubStatus"] == DBNull.Value ? "" : x["SubStatus"].ToString(),
                        InsurancePolicyId = x.GetValueOrDefault<int?>("InsurancePolicyId"),
                        OrderingPhysicianNPI = x["OrderingPhysicianNPI"] == DBNull.Value ? "" : x["OrderingPhysicianNPI"].ToString().Trim(),
                    },


                }).ToList();
            }
            return toReturn;
        }
        public void SetCompanyDetails(ref List<BillingInsuranceCompanyDTO> companies)
        {
            DataSet dsDccMain = new DataSet();
            using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                //need to add the storeproc to get Insurance
                SqlCommand cmd = new SqlCommand("sp_InsuranceGetInsuranceList", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                sqlHelper.ExecuteSqlDataAdapter(cmd, dsDccMain);
            }
            foreach (var com in companies)
            {
                dsDccMain.Tables[0].DefaultView.RowFilter = "insurancecompanyid=" + com.InsuranceCompanyId;
                if (dsDccMain.Tables[0].DefaultView.Count > 0)
                {
                    com.Name = dsDccMain.Tables[0].DefaultView.ToTable().Rows[0]["NAME"].ToString();
                    com.Code = dsDccMain.Tables[0].DefaultView.ToTable().Rows[0]["InsCode"].ToString();
                }
                else
                {
                    continue;
                }
            }
        }
        public List<PolicyDTO> GetPolicies(int clientId, int policyId)
        {
            var toReturn = new List<PolicyDTO>();
            var result = new DataTable();

            using (SqlConnection connection = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand sqlCommand = new SqlCommand("sp_GetInsurancePolicies", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                if (clientId != 0) sqlCommand.Parameters.AddWithValue("@ClientId", clientId);
                if (policyId != 0) sqlCommand.Parameters.AddWithValue("@PolicyId", policyId);
                sqlHelper.ExecuteSqlDataAdapter(sqlCommand, result);
            }

            if (result.HasRows())
            {
                toReturn = result.Rows.Cast<DataRow>().Select(x => new PolicyDTO()
                {
                    FirstName = x.GetValueOrDefault<string>("FirstName"),
                    LastName = x.GetValueOrDefault<string>("LastName"),
                    InsuredIdNo = x.GetValueOrDefault<string>("InsuredIdNo"),
                    PolicyNumber = x.GetValueOrDefault<string>("PolicyNumber"),
                    InsuranceCompanyId = int.Parse(x["InsuranceCompanyId"] == DBNull.Value ? "0" : x["InsuranceCompanyId"].ToString()),
                    InsurancePolicyId = int.Parse(x["InsurancePolicyId"] == DBNull.Value ? "0" : x["InsurancePolicyId"].ToString()),
                    Address = new AddressDTO()
                    {
                        City = x.GetValueOrDefault<string>("City"),
                        Line1 = x.GetValueOrDefault<string>("AddressLine"),
                        PostalCode = x.GetValueOrDefault<string>("PostalCode"),
                        State = x.GetValueOrDefault<string>("State")
                    },
                    InsuranceRelationshipId = int.Parse(x["InsuranceRelationshipId"] == DBNull.Value ? "0" : x["InsuranceRelationshipId"].ToString())
                }).ToList();
            }
            return toReturn;
        }
        public List<ClaimPaymentDTO> GetPayments(int claimId)
        {
            var toReturn = new List<ClaimPaymentDTO>();
            var result = new DataTable();

            using (SqlConnection connection = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand sqlCommand = new SqlCommand("sp_GetClaimPaymentsByClaim", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                sqlCommand.Parameters.AddWithValue("@ClaimId", claimId);
                sqlHelper.ExecuteSqlDataAdapter(sqlCommand, result);
            }

            if (result.HasRows())
            {
                toReturn = result.Rows.Cast<DataRow>().Select(x => new ClaimPaymentDTO()
                {
                    Amount = x.GetValueOrDefault<decimal>("Amount"),
                    DenialReasonId = x.GetValueOrDefault<string>("DenialReason"),
                    IsDenial = x.GetValueOrDefault<bool>("IsDenial"),
                    VoidedAt = x["VOIDEDAT"] == DBNull.Value ? (DateTime?)null : x.GetValueOrDefault<DateTime>("VOIDEDAT"),
                    InsurancePolicyId = int.Parse(x["InsurancePolicyId"] == DBNull.Value ? "0" : x["InsurancePolicyId"].ToString()),
                    MCID = x.GetValueOrDefault<string>("MCID"),
                    PayDate = x.GetValueOrDefault<DateTime>("ReceivedAt"),
                    PaymentTypeId = int.Parse(x["PaymentTypeId"] == DBNull.Value ? "0" : x["PaymentTypeId"].ToString()),
                    InsuranceCompanyId = int.Parse(x["InsuranceCompanyId"] == DBNull.Value ? "0" : x["InsuranceCompanyId"].ToString())
                }).ToList();
            }
            return toReturn;
        }
        public List<AppointmentDTO> GetAppoinments(int claimId, int policyId)
        {
            var toReturn = new List<AppointmentDTO>();
            var result = new DataTable();

            using (SqlConnection connection = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand sqlCommand = new SqlCommand("sp_GetClientSessionTherapy", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                sqlCommand.Parameters.AddWithValue("@ClaimId", claimId);
                sqlHelper.ExecuteSqlDataAdapter(sqlCommand, result);
            }

            if (result.HasRows())
            {
                toReturn = result.Rows.Cast<DataRow>().Select(x => new AppointmentDTO()
                {
                    Amount = x.GetValueOrDefault<decimal>("Amount"),
                    DisciplineCode = x.GetValueOrDefault<string>("svc"),
                    ServiceId = int.Parse(x["ClientServiceId"] == DBNull.Value ? "0" : x["ClientServiceId"].ToString()),
                    CPTRates = GetCPTRates(int.Parse(x["ClientServiceId"] == DBNull.Value ? "0" : x["ClientServiceId"].ToString()), policyId),
                    Units = decimal.Parse(x["Units"] == DBNull.Value ? "0" : x["Units"].ToString()),
                    GovtUnits = decimal.Parse(x["GovtUnits"] == DBNull.Value ? "0" : x["GovtUnits"].ToString()),
                    IsTeletherapy = bool.Parse(x["teletherapy"] == DBNull.Value ? "0" : x["teletherapy"].ToString()),
                    StatusId = int.Parse(x["SessionTherapyStatusID"] == DBNull.Value ? "0" : x["SessionTherapyStatusID"].ToString()),
                    ClientServiceID = x.GetValueOrDefault<int>("ClientServiceId"),
                    StartAt = x.GetValueOrDefault<DateTime>("StartAt"),
                    EndAt = x.GetValueOrDefault<DateTime>("EndAt")
                }).ToList();
            }
            return toReturn;
        }
        public List<CPTRateDTO> GetCPTRates(int serviceId, int policyId)
        {
            var toReturn = new List<CPTRateDTO>();
            var result = new DataTable();

            using (SqlConnection connection = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand sqlCommand = new SqlCommand("sp_GetServiceCPTRate", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                sqlCommand.Parameters.AddWithValue("@ServiceId", serviceId);
                sqlCommand.Parameters.AddWithValue("@PolicyId", policyId);
                sqlHelper.ExecuteSqlDataAdapter(sqlCommand, result);
            }

            if (result.HasRows())
            {
                toReturn = result.Rows.Cast<DataRow>().Select(x => new CPTRateDTO()
                {
                    Amount = x.GetValueOrDefault<decimal>("Amount"),
                    CPTCode = x.GetValueOrDefault<string>("CPTCode"),
                    Modifier1 = x.GetValueOrDefault<string>("Mod1"),
                    Modifier2 = x.GetValueOrDefault<string>("Mod2"),
                    Modifier3 = x.GetValueOrDefault<string>("Mod3"),
                    Units = x.GetValueOrDefault<string>("Units"),
                    ServiceCPTRateId = x.GetValueOrDefault<int>("ServiceCPTRateId"),
                    ClientServiceId = x.GetValueOrDefault<int>("ClientServiceId"),
                    InsurancePolicyId = x.GetValueOrDefault<int>("InsurancePolicyId"),
                }).ToList();
            }
            return toReturn;
        }
        public Dictionary<int, StaffDTO> GetStaffDetails(List<int?> userIds)
        {
            DataTable users = new DataTable();
            users.Columns.Add("userId");
            foreach (int id in userIds.Distinct())
            {
                if (id != 0)
                {
                    DataRow dr = users.NewRow();
                    dr["userId"] = id;
                    users.Rows.Add(dr);
                }
            }
            var lstReturn = new List<StaffDTO>();
            var result = new DataTable();

            using (SqlConnection connection = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand sqlCommand = new SqlCommand("sp_GetStaffDetails", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                sqlCommand.Parameters.AddWithValue("@UserIds", users);
                sqlHelper.ExecuteSqlDataAdapter(sqlCommand, result);
            }

            if (result.HasRows())
            {
                lstReturn = result.Rows.Cast<DataRow>().Select(x => new StaffDTO()
                {
                    UserId = int.Parse(x["prID"] == DBNull.Value ? "0" : x["prID"].ToString()),
                    FirstName = x["fn"] == DBNull.Value ? "" : x["fn"].ToString(),
                    LastName = x["ln"] == DBNull.Value ? "" : x["ln"].ToString(),
                    NPI = x.GetValueOrDefault<string>("npi"),
                    MedicaidID = x.GetValueOrDefault<string>("ahcccsID"),
                    Address = new AddressDTO()
                    {
                        City = x.GetValueOrDefault<string>("cty"),
                        Line1 = x.GetValueOrDefault<string>("ad1"),
                        PostalCode = x.GetValueOrDefault<string>("z"),
                        State = x.GetValueOrDefault<string>("st")
                    }
                }).ToList();
            }
            Dictionary<int, StaffDTO> dicReturn = new Dictionary<int, StaffDTO>();

            foreach (int id in userIds.Distinct())
            {
                dicReturn.Add(id, lstReturn.Where(s => s.UserId == id).Select(s => s).FirstOrDefault());
            }
            return dicReturn;
        }
        public StringBuilder HCFA(List<BillingInsuranceCompanyDTO> billingInsuranceCompanies, List<ClientDTO> clients, CompanyInfoDTO company,
            List<ClientClaimDTO> _claims, string userId, Dictionary<int, StaffDTO> users, Dictionary<int, List<CredentialDTO>> credentials,
           out Dictionary<int, string> errors, int clearingHouse, out string groupTracking, List<TeleHealthDTO> telehealthChanges = null,
            bool enableTelehealth = false)
        {
            errors = new Dictionary<int, string>();
            var nw = DateTime.UtcNow;
            groupTracking = nw.ToString("yyMMddmm");
            StringBuilder finalResult = new StringBuilder();
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

                finalResult.AppendLine(string.Format("ISA*00*          *00*          *30*{0}*30*330897513               *{1}*{2}*^*00501*{3}*0*P*:", company.TaxId.Replace("-", "").PadRight(15), nw.ToString("yyMMdd"), nw.ToString("HHmm"), userId.PadLeft(9, '0')));
                finalResult.AppendLine(string.Format("GS*HC*{0}*OA*{1}*{2}*{3}*X*005010X222A1", company.TaxId.Replace("-", "").PadRight(15), nw.ToString("yyMMdd"), nw.ToString("HHmm"), groupTracking));

                foreach (var claim in _claims)
                {
                    StringBuilder result = new StringBuilder();

                    var policy = claim.Claim.Policy;
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

                    //if the client has a DDD client ID, check if the length is valid
                    if (!string.IsNullOrEmpty(claim.Claim.ClientGovtProgramId))
                    {
                        if (claim.Claim.ClientGovtProgramId.Length != 10)
                        {
                            errors.Add(claim.ClaimId, string.Format("Client {0} has an invalid Client DDD ID. Cannot generate insurance submissions for this client.", (client.FirstName + " " + client.LastName)));
                            result.Clear();
                            continue;
                        }
                    }
                    
                    //if client has physician NPI, check if the length is valid
                    if (!string.IsNullOrEmpty(claim.Claim.OrderingPhysicianNPI))
                    {
                        if (claim.Claim.OrderingPhysicianNPI.Length != 10)
                        {
                            errors.Add(claim.ClaimId, string.Format("Client {0} has an invalid Physician NPI. Cannot generate insurance submissions for this client.", (client.FirstName + " " + client.LastName)));
                            result.Clear();
                            continue;
                        }
                    }

                    var usr = users[claim.Claim.ApproverUserId];
                    string npi = "";
                    if (!string.IsNullOrWhiteSpace(usr?.NPI))
                    {
                        npi = usr.NPI.Trim();
                    }

                    //if staff has providerNPI, check if the length is valid
                    if (!string.IsNullOrEmpty(npi))
                    {
                        if (npi.Length != 10)
                        {
                            errors.Add(claim.ClaimId, string.Format("Staff {0} has an invalid NPI. Cannot generate insurance submissions for this client({1}).", 
                                (usr.FirstName + " " + usr.LastName ) , (client.FirstName + " " + client.LastName)));
                            result.Clear();
                            continue;
                        }
                    }
                    
                    //if staff has ProviderStateMedicaid, check if the length is valid
                    if (!string.IsNullOrEmpty(usr.MedicaidID))
                    {
                        if (usr.MedicaidID.Trim().Length != 6)
                        {
                            errors.Add(claim.ClaimId, string.Format(
                                "Staff {0} has an invalid Medicaid ID. Cannot generate insurance submissions for this client({1}).",
                                (usr.FirstName + " " + usr.LastName), (client.FirstName + " " + client.LastName)));
                            result.Clear();
                            continue;
                        }
                    }

                    //billing provider name 
                    result.AppendLine(string.Format("NM1*85*1*{0}*****XX*{1}", company.Name.ToUpper(), company.NPI.Replace("-", "").Trim()));
                    lineCount++;

                    if (!string.IsNullOrEmpty(company.SkilledBillingAddress) && !string.IsNullOrEmpty(company.SkilledBillingCity) && !string.IsNullOrEmpty(company.SkilledBillingState) && !string.IsNullOrEmpty(company.SkilledBillingZipCode))
                    {
                        //billing provider address 
                        result.AppendLine(string.Format("N3*{0}", company.SkilledBillingAddress));
                        lineCount++;

                        //billing provider city,state,zip 
                        result.AppendLine(string.Format("N4*{0}*{1}*{2}", company.SkilledBillingCity.Length > 30 ? company.SkilledBillingCity.Substring(0, 30).ToUpper() : company.SkilledBillingCity.ToUpper(), company.SkilledBillingState.ToUpper(), company.SkilledBillingZipCode.Substring(0, 5)));
                        lineCount++;
                    }
                    else
                    {
                        //billing provider address 
                        result.AppendLine(string.Format("N3*{0}", company.Address.Line1));
                        lineCount++;

                        //billing provider city,state,zip 
                        result.AppendLine(string.Format("N4*{0}*{1}*{2}", company.Address.City.Length > 30 ? company.Address.City.Substring(0, 30).ToUpper() : company.Address.City.ToUpper(), company.Address.State.ToUpper(), company.Address.PostalCode.Substring(0, 5)));
                        lineCount++;
                    }

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


                    //client city,state,zip 

                    #region Get Client Address from policy if not present in Client
                    if (string.IsNullOrWhiteSpace(client.Address.Line1) || string.IsNullOrWhiteSpace(client.Address.City) || string.IsNullOrWhiteSpace(client.Address.PostalCode) || string.IsNullOrWhiteSpace(client.Address.State))
                    {
                        client.Address.Line1 = policy.Address.Line1;
                        client.Address.City = policy.Address.City;
                        client.Address.State = policy.Address.State;
                        client.Address.PostalCode = policy.Address.PostalCode;
                    }
                    #endregion

                    //patient  address 
                    result.AppendLine(string.Format("N3*{0}", client.Address.Line1.ToUpper()));
                    lineCount++;

                    result.AppendLine(string.Format("N4*{0}*{1}*{2}", client.Address.City.Length > 30 ? client.Address.City.Substring(0, 30).ToUpper() : client.Address.City.ToUpper(), client.Address.State.ToUpper(), client.Address.PostalCode.Length > 5 ? client.Address.PostalCode.Substring(0, 5) : client.Address.PostalCode));
                    lineCount++;

                    //client  demographics 
                    result.AppendLine(string.Format("DMG*D8*{0}*{1}", client.DoB.Value.ToString("yyyyMMdd"), client.GenderId.GetValueOrDefault((int)GenderTypeEnum.Other) == (int)GenderTypeEnum.Male ? "M" : client.GenderId.GetValueOrDefault((int)GenderTypeEnum.Other) == (int)GenderTypeEnum.Female ? "F" : "U"));
                    lineCount++;

                    var clientServiceIds = claim.Claim.Appointments.Select(x => x.ClientServiceID).ToArray();
                    var diagnosis = (from d in client.DiagnosisCodes
                                     where d.InsurancePolicyId == policy.InsurancePolicyId && clientServiceIds.Contains(d.ClientServiceId)
                                     select string.Format("ABF:{0}", d.Code)).ToArray();

                    if (!diagnosis.Any())
                    {
                        errors.Add(claim.ClaimId, string.Format("Client {0} has no diagnosis on their profile. Cannot generate insurance submissions for this client.", (client.FirstName + " " + client.LastName)));
                        result.Clear();
                        continue;
                    }
                    diagnosis[0] = diagnosis[0].Replace("ABF:", "ABK:");
                    //Add in the claims
                    decimal amountDue = 0;

                    //Break costs/units down by cpt code
                    //AccountData account = new AccountData();
                    var cpts = new List<CPTBreakOutDTO>();
                    decimal cptClaimTotal = 0;
                    foreach (var apt in claim.Claim.Appointments)
                    {
                        var validCPTs = apt.CPTRates.Where(x => x.InsurancePolicyId == claim.Claim.InsurancePolicyId && x.ClientServiceId == apt.ClientServiceID).ToList();

                        // There will always be one valid CPT as per the architecture
                        var cpt = apt.CPTRates.FirstOrDefault(x => x.InsurancePolicyId == claim.Claim.InsurancePolicyId && x.ClientServiceId == apt.ClientServiceID);
                        if (cpt != null)
                        {
                            cpts.Add(new CPTBreakOutDTO(cpt.CPTCode, claim.Claim.AmountDue, cpt.Modifier1, cpt.Modifier2, cpt.Modifier3) { Units = Convert.ToDecimal(apt.Units) });
                        }
                    }


                    if (!cpts.Any())
                    {
                        if (errors.Any())
                            errors.Add(claim.ClaimId, string.Format("Cannot generate insurance submissions for this claim."));
                        else
                            errors.Add(claim.ClaimId, string.Format("Claim {0} has no CPT's assigned from its current insurance. Cannot generate insurance submissions for this claim.", claim.ClaimId));

                        result.Clear();
                        continue;
                    }
                    else
                    {
                        // errors.Clear();
                    }

                    //Claim header
                    amountDue = cpts.Sum(c => c.Amount);
                    result.AppendLine(string.Format("CLM*{0}*{1}***{2}:B:1*Y*A*Y*Y", claim.ClaimId, amountDue, claim.Claim.Appointments.FirstOrDefault().IsTeletherapy ? "2" :( claim.Claim.LocationTypeId == (int)LocationEnum.Clinic ? "11" : "12")));
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
                            break;
                        case (int)LocationEnum.ProviderHome:
                            usr = users[claim.Claim.StaffUserID.GetValueOrDefault()];
                            if (!string.IsNullOrWhiteSpace(usr?.NPI))
                            {
                                npi = usr.NPI;
                            }
                            result.AppendLine(string.Format("NM1*77*2*{0}", usr.LastName.ToUpper(), usr.FirstName.Length > 35 ? usr.FirstName.Substring(0, 35).ToUpper() : usr.FirstName.ToUpper()));
                            lineCount++;
                            break;
                    }
                    result.AppendLine(string.Format("N3*{0}", claim.Claim.PlaceOfService.Line1.ToUpper()));
                    lineCount++;

                    //client city,state,zip 
                    result.AppendLine(string.Format("N4*{0}*{1}*{2}", claim.Claim.PlaceOfService.City.Length > 30 ? claim.Claim.PlaceOfService.City.Substring(0, 30).ToUpper() : claim.Claim.PlaceOfService.City.ToUpper(), claim.Claim.PlaceOfService.State.ToUpper(), claim.Claim.PlaceOfService.PostalCode.Substring(0, 5)));
                    lineCount++;


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
                            result.AppendLine(string.Format("SBR*{2}*{0}*{1}******C1", relationshipCode, payPolicy.PolicyNumber ?? "", oName));
                            lineCount++;
                            if (py.IsDenial)
                            {
                                result.AppendLine(string.Format("CAS*OA*{0}*0", py.DenialReasonId));
                                lineCount++;
                            }
                            amountDue -= py.Amount.GetValueOrDefault(0);
                            result.AppendLine(string.Format("AMT*D*{0}", py.Amount.GetValueOrDefault(0).ToString("0.00")));
                            lineCount++;
                            if (py.AllowedAmount.HasValue)
                            {
                                result.AppendLine(string.Format("AMT*B6*{0}", py.AllowedAmount.GetValueOrDefault(0).ToString("0.00")));
                                lineCount++;
                            }
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
                        result.AppendLine(string.Format("SV1*HC:{0}{4}*{1}*UN*{2}*{3}**1", (replacementRequried ? telehealthCode : c.Code.Trim()), c.Amount.ToString("0.00"), c.Units.ToString("0.00"), locCode, (replacementRequried ? telehealthMods : c.Mods)));
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

                    finalResult.Append(result.ToString());
                }

                finalResult.AppendLine(string.Format("GE*10*{0}", groupTracking));
                finalResult.AppendLine(string.Format("IEA*1*{0}", userId.PadLeft(9, '0')));

            }
            catch (Exception ex)
            {
                throw (ex);
            }

            return finalResult;
        }

        private string GetRelationshipCode(PolicyDTO policy)
        {
            string retVal = "G8";

            switch (policy.InsuranceRelationshipId)
            {

                case (int)InsuranceRelationshipEnum.Self:
                    retVal = "18";
                    break;
                case (int)InsuranceRelationshipEnum.Spouse:
                    retVal = "01";
                    break;
                case (int)InsuranceRelationshipEnum.Child:
                    retVal = "19";
                    break;
                default:
                    retVal = "G8";
                    break;
            }

            return retVal;
        }

        //public StringBuilder HCFA(ProviderInit r, List<BillingInsuranceCompanyDTO> insuranceCompanies, List<ClientDTO> clients, string userId, Dictionary<string, StaffDTO> users, Dictionary<long, List<CredentialDTO>> credentials, List<string> errors, int clearingHouse, out string groupTracking)
        public StringBuilder HCFA(ProviderInit r, int clientId, int companyId, int claimId, out string groupTracking)
        {
            var nw = DateTime.UtcNow;
            var company = r.providerList != null ? r.providerList.Where(p => p.insuranceCompanyId == companyId).Select(p => p).FirstOrDefault() : new Provider() { name = r.companyName };
            var client = r.clientInfoList != null ? r.clientInfoList.Where(c => c.clientId == clientId).Select(c => c).FirstOrDefault() : new ClientInfo() { clientId = clientId };
            groupTracking = nw.ToString("yyMMddmm");

            int segmentCount = 1;
            int svcLine = 1;

            //Generate standar submitter lines so do not have to repeat
            StringBuilder submitterLines = new StringBuilder();
            if (company.name.Length > 60) company.name = company.name.Substring(0, 60);
            company.name = company.name.ToUpper();

            int submitterLineCount = 0;
            //submitter line
            //submitterLines.AppendLine(string.Format("NM1*41*2*{0}*****46*{1}", r.companyName, company.TaxId.Replace("-", "").Trim()));
            submitterLines.AppendLine(string.Format("NM1*41*2*{0}*****46*{1}", company.name, company.payerId.Replace("-", "").Trim()).Trim());
            submitterLineCount++;
            //submitter contact line
            //submitterLines.AppendLine(string.Format("PER*IC**TE*{0}", company.Phone));
            submitterLines.AppendLine(string.Format("PER*IC**TE*{0}", company.phone));
            submitterLineCount++;
            //receiver line

            submitterLines.AppendLine("NM1*40*2*OFFICE ALLY*****46*330897513");

            submitterLineCount++;


            StringBuilder result = new StringBuilder();

            //result.AppendLine(string.Format("ISA*00*          *00*          *30*{0}*30*330897513330897513      *{1}*{2}*^*00501*{3}*0*P*:", company.TaxId.Replace("-", "").PadRight(15), nw.ToString("yyMMdd"), nw.ToString("HHmm"), userId.PadLeft(9, '0')));
            result.AppendLine(string.Format("ISA*00*          *00*          *30*{0}*30*330897513330897513      *{1}*{2}*^*00501*{3}*0*P*:", company.payerId.Replace("-", "").PadRight(15), nw.ToString("yyMMdd"), nw.ToString("HHmm"), UserClaim.uid.ToString().PadLeft(9, '0')));
            //result.AppendLine(string.Format("GS*HC*{0}*OA*{1}*{2}*{3}*X*005010X222A1", company.TaxId.Replace("-", "").PadRight(15), nw.ToString("yyMMdd"), nw.ToString("HHmm"), groupTracking));
            result.AppendLine(string.Format("GS*HC*{0}*OA*{1}*{2}*{3}*X*005010X222A1", company.payerId.Replace("-", "").PadRight(15), nw.ToString("yyMMdd"), nw.ToString("HHmm"), groupTracking));
            if (r.ClientDetailsList != null && r.ClientDetailsList.Any())
            {
                foreach (var claim in r.ClientDetailsList.FirstOrDefault().ClientDetailsInfoList.Where(c => c.claimId == claimId).Select(c => c))
                {
                    //var policy = claim.;
                    //var client = clients.Find(c => c.ClientId == claim.Claim.ClientId);
                    //string polId = claim.ClaimId < 1000 ? claim.ClaimId.ToString().PadLeft(4, '0') : claim.ClaimId.ToString();
                    int lineCount = 0;
                    //Start transaction set
                    result.AppendLine(string.Format("ST*837*{0}*005010X222A1", claim.policyNumber));
                    lineCount++;
                    //Transaction header
                    result.AppendLine(string.Format("BHT*0019*00*CF-{0}-{1}-{4}*{2}*{3}*CH", claim.InsurancePolicyId, claim.claimId, claim.StartDate.ToString("yyMMdd"), claim.StartDate.ToString("yyMMdd"), claim.EndDate.ToString("HHmm"), nw.ToString("yyMMdd")));
                    lineCount++;

                    //Add in submitter/receiver lines
                    result.Append(submitterLines.ToString());
                    lineCount += submitterLineCount;

                    //billing provider start
                    result.AppendLine(string.Format("HL*{0}**20*1", segmentCount));
                    segmentCount++;
                    lineCount++;

                    //billing provider name 
                    result.AppendLine(string.Format("NM1*85*2*{0}*****XX*{1}", company.name, UserClaim.npi.Replace("-", "").Trim()));
                    lineCount++;

                    //billing provider address 
                    result.AppendLine(string.Format("N3*{0}", company.line1));
                    lineCount++;

                    //billing provider city,state,zip 
                    //result.AppendLine(string.Format("N4*P{0}*{1}*{2}", company.Address.City.Length > 30 ? company.Address.City.Substring(0, 30).ToUpper() : company.Address.City.ToUpper(), company.Address.State.ToUpper(), company.Address.PostalCode.Substring(0, 5)));
                    result.AppendLine(string.Format("N4*P{0}*{1}*{2}", company.city.Length > 30 ? company.city.Substring(0, 30).ToUpper() : company.city.ToUpper(), company.state.ToUpper(), company.postalCode.Length > 5 ? company.postalCode.Substring(0, 5) : company.postalCode));
                    lineCount++;

                    //billing provider tax id
                    result.AppendLine(string.Format("REF*EI*{0}", company.payerId));
                    lineCount++;

                    //Subscriber start line
                    result.AppendLine(string.Format("HL*{0}*{1}*22*1", segmentCount, segmentCount - 1));
                    segmentCount++;
                    lineCount++;

                    //Subscriber info line
                    string relationshipCode = "21";
                    relationshipCode = r.InsurancePolicy == null ? "21" : GetRelationshipCode(int.Parse(r.InsurancePolicy.Relationship));
                    int prvPayCount = 0;
                    //if (claim..Payments != null && claim.Claim.Payments.Any())
                    //{
                    //    var prvPolicies = (from p in claim.Claim.Payments
                    //                       where p.PaymentTypeId.GetValueOrDefault((int)PaymentTypeEnum.Private) == (int)PaymentTypeEnum.Insurance
                    //                       select p.InsurancePolicyId.GetValueOrDefault(0)).Distinct();
                    //    prvPayCount = prvPolicies.Count();
                    //}
                    prvPayCount = r.ClaimPaymentList.Where(p => p.PaymentTypeId == 1).Count();//PaymentTypeEnum.Private
                    var policyOrder = PolicyOrderCode(prvPayCount + 1);
                    //result.AppendLine(string.Format("SBR*{2}*{0}*{1}******CI", relationshipCode, policy.Number ?? "", policyOrder));
                    result.AppendLine(string.Format("SBR*{2}*{0}*{1}******CI", relationshipCode, claim.policyNumber ?? "", policyOrder));
                    lineCount++;

                    //subscriber name
                    //result.AppendLine(string.Format("NM1*IL*1*{0}*{1}****MI*{2}", policy.LastName.ToUpper(), policy.FirstName.Length > 35 ? policy.FirstName.Substring(0, 35).ToUpper() : policy.FirstName.ToUpper(), policy.InsuredIdNo));
                    result.AppendLine(string.Format("NM1*IL*1*{0}*{1}****MI*{2}", "", "", claim.InsurancePolicyId));
                    lineCount++;

                    //subscriber  address 
                    //result.AppendLine(string.Format("N3*{0}", policy.Address.Line1.ToUpper()));
                    result.AppendLine(string.Format("N3*{0}", ""));
                    lineCount++;

                    //subscriber city,state,zip 
                    //result.AppendLine(string.Format("N4*{0}*{1}*{2}", policy.Address.City.Length > 30 ? policy.Address.City.Substring(0, 30).ToUpper() : policy.Address.City.ToUpper(), policy.Address.State.ToUpper(), policy.Address.PostalCode.Substring(0, 5)));
                    result.AppendLine(string.Format("N4*{0}*{1}*{2}", "", "", ""));
                    lineCount++;

                    //Payer
                    //var ic = insuranceCompanies.Find(i => i.InsuranceCompanyId == policy.InsuranceCompanyId);
                    //var ic = r.companies.Where(c=>c.id==claim);
                    //if (ic.Name.Length > 60) ic.Name = ic.Name.Substring(0, 60);
                    result.AppendLine(string.Format("NM1*PR*2*{0}*****PR*{1}", company.name, company.insuranceCompanyId));
                    lineCount++;

                    //Patient start line
                    result.AppendLine(string.Format("HL*{0}*{1}*23*0", segmentCount, segmentCount - 1));
                    segmentCount++;
                    lineCount++;

                    //Patient name
                    //result.AppendLine(string.Format("NM1*QC*1*{0}*{1}{2}", client.LastName.ToUpper(), client.FirstName.Length > 35 ? client.FirstName.Substring(0, 35).ToUpper() : client.FirstName.ToUpper(), string.IsNullOrWhiteSpace(client.MiddleName) ? "" : client.MiddleName.Length > 25 ? client.MiddleName.Substring(0, 25).ToUpper() : client.MiddleName.ToUpper()));
                    result.AppendLine(string.Format("NM1*QC*1*{0}*{1}{2}", client.lastName.ToUpper(), client.firstName.Length > 35 ? client.firstName.Substring(0, 35).ToUpper() : client.firstName.ToUpper(), ""));
                    lineCount++;


                    //patient  address 
                    //result.AppendLine(string.Format("N3*{0}", client.Address.Line1.ToUpper()));
                    result.AppendLine(string.Format("N3*{0}", client.adLine.ToUpper()));
                    lineCount++;

                    //client city,state,zip 
                    //result.AppendLine(string.Format("N4*{0}*{1}*{2}", client.Address.City.Length > 30 ? client.Address.City.Substring(0, 30).ToUpper() : client.Address.City.ToUpper(), client.Address.State.ToUpper(), client.Address.PostalCode.Substring(0, 5)));
                    result.AppendLine(string.Format("N4*{0}*{1}*{2}", client.city.Length > 30 ? client.city.Substring(0, 30).ToUpper() : client.city.ToUpper(), client.state.ToUpper(), client.postalCode.Length > 5 ? client.postalCode.Substring(0, 5) : client.postalCode));
                    lineCount++;

                    //client  demographics 
                    //result.AppendLine(string.Format("DMG*D8*{0}*{1}", client.DoB.Value.ToString("yyyyMMdd"), client.GenderId.GetValueOrDefault((int)GenderTypeEnum.Other) == (int)GenderTypeEnum.Male ? "M" : client.GenderId.GetValueOrDefault((int)GenderTypeEnum.Other) == (int)GenderTypeEnum.Female ? "F" : "U"));
                    result.AppendLine(string.Format("DMG*D8*{0}*{1}", client.dob.ToString("yyyyMMdd"), client.sex));
                    lineCount++;

                    string[] diagnosis = new string[] { };
                    if (client.DiagnosisCodes != null)
                    {
                        diagnosis = (from d in client.DiagnosisCodes
                                     select string.Format("ABF:{0}", d)).ToArray();

                        if (diagnosis.Any())
                        {
                            //errors.Add(string.Format("Client {0} has no diagnosis on their profile. Cannot generate insurance submissions for this client.", client.clientId));
                            result.Clear();
                            return result;
                        }
                        diagnosis[0] = diagnosis[0].Replace("ABF:", "ABK:");
                    }
                    //Add in the claims
                    decimal amountDue = 0;

                    //Break costs/units down by cpt code
                    //AccountData account = new AccountData();
                    //var cpts = new List<CPTBreakOutDTO>();
                    decimal cptClaimTotal = 0;
                    //foreach (var apt in claim.Claim.Appointments)
                    //{
                    //    apt.AdjustCPTsToUnits();
                    //    var cptTotal = apt.CPTRates.Sum(c => c.Amount.GetValueOrDefault(0));
                    //    if (cptTotal == 0) continue; //This appointment does not contribute so move to next
                    //    cptClaimTotal += cptTotal;
                    //    foreach (var cpt in apt.CPTRates)
                    //    {
                    //        if (cpt.Amount.GetValueOrDefault(0) == 0) continue;
                    //        var bo = cpts.Find(b => b.Code == cpt.CPTCode);
                    //        decimal amountShare = Math.Ceiling(100 * apt.Amount.GetValueOrDefault(0) * cpt.Amount.GetValueOrDefault(0) / cptTotal) / 100;
                    //        if (bo == null)
                    //        {
                    //            cpts.Add(new CPTBreakOutDTO(cpt.CPTCode, cpt.Amount.GetValueOrDefault(0), cpt.Mod1, cpt.Mod2, cpt.Mod3) { Amount = amountShare });
                    //        }
                    //        else
                    //        {
                    //            bo.Units += cClientSessionTherapyIDpt.Amount.GetValueOrDefault(0);
                    //            bo.Amount += amountShare;
                    //        }
                    //    }
                    //}


                    //if (cpts.Count == 0)
                    //{
                    //errors.Add(string.Format("Claim {0} has no CPT's assigned from its current insurance. Cannot generate insurance submissions for this claim.", claim.ClaimId));
                    //result.Clear();
                    //return result;
                    //}

                    //Claim header
                    //amountDue = cpts.Sum(c => c.Amount);
                    //result.AppendLine(string.Format("CLM*{0}*{1}***{2}:B:1*Y*A*Y*Y", claim.ClaimId, amountDue, claim.Claim.LocationTypeId == (int)LocationEnum.Clinic ? "11" : "12"));
                    //lineCount++;
                    //Private payment notes
                    //if (claim.Claim.Payments != null && claim.Claim.Payments.Exists(py => !py.VoidedAt.HasValue && py.Amount.GetValueOrDefault(0) > 0 && py.PaymentTypeId == (int)PaymentTypeEnum.Private))
                    //{
                    //    var ppay = claim.Claim.Payments.FindAll(py => !py.VoidedAt.HasValue && py.Amount.GetValueOrDefault(0) > 0 && py.PaymentTypeId == (int)PaymentTypeEnum.Private).Sum(py => py.Amount.GetValueOrDefault(0));
                    //    result.AppendLine(string.Format("AMT*F5*{0}", ppay.ToString("f2")));
                    //    lineCount++;
                    //    amountDue -= ppay;
                    //}
                    if (r.ClaimPaymentList != null && r.ClaimPaymentList.Exists(py => py.VoidedAt != DateTime.MinValue && py.Amount > 0 && py.PaymentTypeId == 1))
                    {
                        var ppay = r.ClaimPaymentList.FindAll(py => py.VoidedAt != DateTime.MinValue && py.Amount > 0 && py.PaymentTypeId == 1).Sum(py => py.Amount);
                        result.AppendLine(string.Format("AMT*F5*{0}", ppay.ToString("f2")));
                        lineCount++;
                        cptClaimTotal += ppay;
                        amountDue -= ppay;
                    }
                    //Diagnosis
                    result.AppendLine(string.Format("HI*{0}", string.Join("*", diagnosis)));
                    lineCount++;

                    //var usr = users[claim.Claim.ApproverUserId.ToString()];
                    //string npi = "";
                    //if (!string.IsNullOrWhiteSpace(usr?.NPI))
                    //{
                    //    npi = usr.NPI;
                    //}
                    string npi = UserClaim.npi;

                    //Provider name
                    //if (!ic.ExcludeRenderer.GetValueOrDefault(false))
                    //{
                    //    result.AppendLine(string.Format("NM1*82*1*{0}*{1}{2}", usr.LastName.ToUpper(), usr.FirstName.Length > 35 ? usr.FirstName.Substring(0, 35).ToUpper() : usr.FirstName.ToUpper(), npi == null ? "" : string.Format("****XX*{0}", npi)));
                    //    lineCount++;
                    //}
                    //if (!company..ExcludeRenderer.GetValueOrDefault(false))
                    //{
                    result.AppendLine(string.Format("NM1*82*1*{0}*{1}{2}", "", "", npi == null ? "" : string.Format("****XX*{0}", npi)));
                    lineCount++;
                    //}
                    //location
                    switch (claim.LocationTypeId)
                    {
                        //case (int)LocationEnum.ClientHome:
                        //    result.AppendLine(string.Format("NM1*77*2*{0}", client.LastName.ToUpper(), client.FirstName.Length > 35 ? client.FirstName.Substring(0, 35).ToUpper() : client.FirstName.ToUpper()));
                        //    lineCount++;
                        //    result.AppendLine(string.Format("N3*{0}", client.Address.Line1.ToUpper()));
                        //    lineCount++;

                        //    //client city,state,zip 
                        //    result.AppendLine(string.Format("N4*{0}*{1}*{2}", client.Address.City.Length > 30 ? client.Address.City.Substring(0, 30).ToUpper() : client.Address.City.ToUpper(), client.Address.State.ToUpper(), client.Address.PostalCode.Substring(0, 5)));
                        //    lineCount++;

                        //    break;
                        //case (int)LocationEnum.ProviderHome:
                        //    usr = users[claim.Claim.StaffUserID.ToString()];
                        //    if (!string.IsNullOrWhiteSpace(usr?.NPI))
                        //    {
                        //        npi = usr.NPI;
                        //    }
                        //    result.AppendLine(string.Format("NM1*77*2*{0}*{2}", usr.LastName.ToUpper(), usr.FirstName.Length > 35 ? usr.FirstName.Substring(0, 35).ToUpper() : usr.FirstName.ToUpper(), npi == null ? "" : string.Format("*****XX*{0}", npi)));
                        //    lineCount++;
                        //    result.AppendLine(string.Format("N3*{0}", usr.Address.Line1.ToUpper()));
                        //    lineCount++;

                        //    //client city,state,zip 
                        //    result.AppendLine(string.Format("N4*{0}*{1}*{2}", usr.Address.City.Length > 30 ? usr.Address.City.Substring(0, 30).ToUpper() : usr.Address.City.ToUpper(), usr.Address.State.ToUpper(), usr.Address.PostalCode.Substring(0, 5)));
                        //    lineCount++;
                        //    break;
                        default:

                            result.AppendLine(string.Format("NM1*77*2*{0}", client.lastName.ToUpper(), client.firstName.Length > 35 ? client.firstName.Substring(0, 35).ToUpper() : client.firstName.ToUpper()));
                            lineCount++;
                            result.AppendLine(string.Format("N3*{0}", client.adLine.ToUpper()));
                            lineCount++;

                            //client city,state,zip 
                            result.AppendLine(string.Format("N4*{0}*{1}*{2}", client.city.Length > 30 ? client.city.Substring(0, 30).ToUpper() : client.city.ToUpper(), client.state.ToUpper(), client.postalCode.Length > 5 ? client.postalCode.Substring(0, 5) : client.postalCode));
                            lineCount++;
                            break;
                    }


                    #region Claim COB
                    //List<ClaimPaymentDTO> ipays = null;
                    //if (claim.Claim.Payments != null && claim.Claim.Payments.Count > 0)
                    //{
                    //    ipays = claim.Claim.Payments.FindAll(cp => !cp.VoidedAt.HasValue && cp.PaymentType == (int)PaymentTypeEnum.Insurance);
                    //    ipays.Sort((a, b) => a.PayDate.GetValueOrDefault().CompareTo(b.PayDate.GetValueOrDefault()));
                    //    int order = 1;
                    //    foreach (var py in claim.Claim.Payments)
                    //    {
                    //        var payPolicy = client.Policies.Find(ip => ip.InsurancePolicyId == py.InsurancePolicyId.GetValueOrDefault(0));//py.SourceId.GetValueOrDefault(0));
                    //        if (payPolicy == null || py.VoidedAt.HasValue) continue;
                    //        string oName = PolicyOrderCode(order);
                    //        order++;
                    //        relationshipCode = GetRelationshipCode(payPolicy);
                    //        result.AppendLine(string.Format("SBR*{2}*{0}*{1}******CI", relationshipCode, payPolicy.Number ?? "", oName));
                    //        lineCount++;
                    //        if (py.IsDenial)
                    //        {
                    //            result.AppendLine(string.Format("CAS*OA*{0}*0", py.DenialReasonId));
                    //            lineCount++;
                    //        }
                    //        amountDue -= py.Amount.GetValueOrDefault(0);
                    //        result.AppendLine(string.Format("AMT*D*{0}", py.Amount.GetValueOrDefault(0).ToString("0.00")));
                    //        lineCount++;
                    //        result.AppendLine(string.Format("AMT*EAF*{0}", amountDue.ToString("0.00")));
                    //        lineCount++;
                    //        result.AppendLine("OI***Y*P**Y");
                    //        lineCount++;

                    //        //subscriber name
                    //        result.AppendLine(string.Format("NM1*IL*1*{0}*{1}****MI*{2}", payPolicy.LastName.ToUpper(), payPolicy.FirstName.Length > 35 ? payPolicy.FirstName.Substring(0, 35).ToUpper() : payPolicy.FirstName.ToUpper(), payPolicy.InsuredIdNo));
                    //        lineCount++;

                    //        //subscriber  address 
                    //        result.AppendLine(string.Format("N3*{0}", payPolicy.Address.Line1.ToUpper()));
                    //        lineCount++;

                    //        //subscriber city,state,zip 
                    //        result.AppendLine(string.Format("N4*{0}*{1}*{2}", payPolicy.Address.City.Length > 30 ? payPolicy.Address.City.Substring(0, 30).ToUpper() : payPolicy.Address.City.ToUpper(), payPolicy.Address.State.ToUpper(), payPolicy.Address.PostalCode.Substring(0, 5)));
                    //        lineCount++;

                    //        //Payer
                    //        var pic = insuranceCompanies.Find(i => i.InsuranceCompanyId == payPolicy.InsuranceCompanyId);
                    //        if (pic.Name.Length > 60) pic.Name = pic.Name.Substring(0, 60);
                    //        result.AppendLine(string.Format("NM1*PR*2*{0}*****PR*{1}", pic.Name.ToUpper(), pic.Code));
                    //        lineCount++;

                    //        result.AppendLine(string.Format("DTP*573*{0}", py.PayDate.GetValueOrDefault().ToString("yyyyMMdd")));
                    //        lineCount++;
                    //    }
                    //}
                    List<ClaimPayment> ipays = null;
                    if (r.ClaimPaymentList != null && r.ClaimPaymentList.Count > 0)
                    {
                        ipays = r.ClaimPaymentList.FindAll(cp => cp.VoidedAt != DateTime.MinValue && cp.PaymentTypeId == 2);
                        ipays.Sort((a, b) => a.ReceivedAt.CompareTo(b.ReceivedAt));
                        int order = 1;
                        foreach (var py in r.ClaimPaymentList)
                        {
                            var payPolicy = r.ClientDetailsList[0].ClientDetailsInfoList.Where(i => i.InsurancePolicyId == py.InsurancePolicyId).Select(i => i).FirstOrDefault();
                            if (payPolicy == null || py.VoidedAt != DateTime.MinValue) continue;
                            string oName = PolicyOrderCode(order);
                            order++;
                            relationshipCode = GetRelationshipCode(int.Parse(r.InsurancePolicy.Relationship));
                            result.AppendLine(string.Format("SBR*{2}*{0}*{1}******CI", relationshipCode, r.InsurancePolicy.PolicyNumber ?? "", oName));
                            lineCount++;
                            if (py.IsDenial)
                            {
                                result.AppendLine(string.Format("CAS*OA*{0}*0", py.DenialReasonId));
                                lineCount++;
                            }
                            amountDue -= py.Amount;
                            result.AppendLine(string.Format("AMT*D*{0}", py.Amount.ToString("0.00")));
                            lineCount++;
                            result.AppendLine(string.Format("AMT*EAF*{0}", amountDue.ToString("0.00")));
                            lineCount++;
                            result.AppendLine("OI***Y*P**Y");
                            lineCount++;

                            //subscriber name
                            result.AppendLine(string.Format("NM1*IL*1*{0}*{1}****MI*{2}", r.InsurancePolicy.LastName.ToUpper(), r.InsurancePolicy.FirstName.Length > 35 ? r.InsurancePolicy.FirstName.Substring(0, 35).ToUpper() : r.InsurancePolicy.FirstName.ToUpper(), r.InsurancePolicy.InsurancePolicyId));
                            lineCount++;

                            //subscriber  address 
                            result.AppendLine(string.Format("N3*{0}", r.InsurancePolicy.AddressLine.ToUpper()));
                            lineCount++;

                            //subscriber city,state,zip 
                            result.AppendLine(string.Format("N4*{0}*{1}*{2}", r.InsurancePolicy.city.Length > 30 ? r.InsurancePolicy.city.Substring(0, 30).ToUpper() : r.InsurancePolicy.city.ToUpper(), r.InsurancePolicy.state.ToUpper(), r.InsurancePolicy.PostalCode.Substring(0, 5)));
                            lineCount++;

                            //Payer
                            var pic = company;// insuranceCompanies.Find(i => i.InsuranceCompanyId == payPolicy.InsuranceCompanyId);
                            if (pic.name.Length > 60) pic.name = pic.name.Substring(0, 60);
                            result.AppendLine(string.Format("NM1*PR*2*{0}*****PR*{1}", pic.name.ToUpper(), pic.insuranceCompanyId));
                            lineCount++;

                            result.AppendLine(string.Format("DTP*573*{0}", py.ReceivedAt.ToString("yyyyMMdd")));
                            lineCount++;
                        }
                    }
                    #endregion

                    foreach (var c in r.ClientDetailsList[0].ClientDetailsInfoList)
                    {
                        //Svc line header
                        result.AppendLine(string.Format("LX*{0}", svcLine));
                        svcLine++;
                        lineCount++;

                        //service details
                        result.AppendLine(string.Format("SV1*HC:{0}{4}*{1}*UN*{2}*{3}**1", c.cptCode, c.paidAmount.ToString("0.00"), c.dddUnit, claim.LocationTypeId, ""));
                        lineCount++;


                        //service date
                        result.AppendLine(string.Format("DTP*472*D8*{0}", claim.StartDate.ToString("yyyyMMdd")));
                        lineCount++;

                        if (ipays != null && ipays.Count > 0)
                        {
                            foreach (var py in ipays)
                            {
                                var payPolicy = r.ClientDetailsList[0].ClientDetailsInfoList.Where(i => i.InsurancePolicyId == py.InsurancePolicyId).Select(i => i).FirstOrDefault();
                                if (payPolicy == null || py.VoidedAt != DateTime.MinValue) continue;
                                var pic = r.providerList != null ? r.providerList.Where(p => p.insuranceCompanyId == companyId).Select(p => p).FirstOrDefault() : new Provider() { name = r.companyName };

                                //var pic = insuranceCompanies.Find(i => i.InsuranceCompanyId == payPolicy.InsuranceCompanyId);
                                var payAmount = py.Amount * decimal.Parse(c.dddUnit) / cptClaimTotal;
                                result.AppendLine(string.Format("SVD*{2}*{1}*HC:{0}**1", c.cptCode, payAmount.ToString("0.00"), pic.insuranceCompanyId));
                                lineCount++;
                                if (py.IsDenial)
                                {
                                    result.AppendLine(string.Format("CAS*OA*{0}*{1}", py.DenialReasonId, c.paidAmount - payAmount));
                                    lineCount++;

                                }
                                else if (payAmount == 0)
                                {
                                    result.AppendLine(string.Format("CAS*PR*1*{0}", c.paidAmount));
                                    lineCount++;
                                }
                                else if (payAmount != c.paidAmount)
                                {
                                    result.AppendLine(string.Format("CAS*OA*3*{0}", c.paidAmount - payAmount));
                                    lineCount++;
                                }
                                result.AppendLine(string.Format("DTP*573*D8*{0}", py.ReceivedAt.ToString("yyyyMMdd")));
                                lineCount++;
                            }
                            lineCount++;
                        }
                    }

                    //End transaction set
                    result.AppendLine(string.Format("SE*{0}*{1}", lineCount, claim.InsurancePolicyId));

                }
            }

            result.AppendLine(string.Format("GE*10*{0}", groupTracking));
            result.AppendLine(string.Format("IEA*1*{0}", ""));

            return result;
        }
        public async Task<ProviderInit> generateModel(string clientId, string selrowid, int selectedClientIndex, string policyId, string companyId, int claimId, int tierId = -1, int insuranceFlag = 0, bool forHCFA = false)
        {
            ProviderInit r = new ProviderInit();
            DataSet ds = null;
            DataSet dsProv = null;
            DataSet dsPayment = null;
            DataSet dsAmount = null;
            List<ClientDetails> clDetails = new List<ClientDetails>();
            r.ClientDetailsList = new List<ClientDetails>();
            DataSet dsDccMain = new DataSet();
            DataSet dsClient = null;
            DataTable dtClient = null;
            if (!forHCFA)
            {
                DataSet ds3 = await getDenialReasonList();
                r.DenialReasonList = ds3.Tables[0].Rows.Cast<DataRow>().Select(spR => new DenialReason()
                {
                    // id = Convert.ToInt32(spR["id"] == DBNull.Value ? 0 : spR["id"]),
                    id = ((string)spR["id"]).Trim(),
                    name = (string)spR["name"]
                }).OrderBy(d => d.id).ToList();
                r.defaultReason = r.DenialReasonList.Where(d => d.id == "1").Select(d => d.id + ": " + d.name).FirstOrDefault();
            }
            try
            {
                dsClient = await getClientListByInsurance(companyId, false, -1);
                dtClient = (dsClient.Tables[0].DefaultView).ToTable();
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
                    r.clientInfoList = dtClient.Rows.Cast<DataRow>().Select(spR => new ClientInfo()
                    {
                        name = Convert.ToString(spR["Name"]),
                        dob = Convert.ToDateTime(spR["dob"] == DBNull.Value ? default(DateTime) : spR["dob"]),//Convert.ToDateTime(spR["dob"]),
                        clientId = Convert.ToInt32(spR["clsvID"] == DBNull.Value ? 0 : spR["clsvID"]),
                        firstName = spR["fn"] == DBNull.Value ? "" : Convert.ToString(spR["fn"]),
                        lastName = spR["ln"] == DBNull.Value ? "" : Convert.ToString(spR["ln"]),
                        adLine = spR["ad1"] == DBNull.Value ? "" : Convert.ToString(spR["ad1"]),
                        city = spR["cty"] == DBNull.Value ? "" : Convert.ToString(spR["cty"]),
                        state = spR["st"] == DBNull.Value ? "" : Convert.ToString(spR["st"]),
                        postalCode = spR["z"] == DBNull.Value ? "" : Convert.ToString(spR["z"]),
                        sex = spR["sex"] == DBNull.Value ? "" : Convert.ToString(spR["sex"]),
                        DiagnosisCodes = spR["DiaCode"] == DBNull.Value ? null : Convert.ToString(spR["DiaCode"]).Split(',')
                    }).ToList();
                }
                catch (Exception ex1)
                {
                    r.er.msg = ex1.Message;
                }

            }
            try
            {
                dsProv = await getInsuranceList(true);

                if (dsProv != null)
                {
                    r.providerList = dsProv.Tables[0].Rows.Cast<DataRow>().Select(spR => new Provider()
                    {
                        payerId = spR["PayerId"] == DBNull.Value ? "" : (string)spR["PayerId"],
                        name = spR["Name"] == DBNull.Value ? "" : (string)spR["Name"],
                        insuranceCompanyId = Convert.ToInt32(spR["InsuranceCompanyId"] == DBNull.Value ? 0 : spR["InsuranceCompanyId"]),
                        line1 = spR["Line1"] == DBNull.Value ? "" : (string)spR["Line1"],
                        line2 = spR["Line2"] == DBNull.Value ? "" : (string)spR["Line2"],
                        city = spR["City"] == DBNull.Value ? "" : (string)spR["City"],
                        state = spR["State"] == DBNull.Value ? "" : (string)spR["State"],
                        postalCode = spR["PostalCode"] == DBNull.Value ? "" : (string)spR["PostalCode"]
                    }).ToList();
                }
                if (!forHCFA)
                {
                    DataSet ds2 = await getGovernmentProgramList();
                    DataTable dt = getInsuranceCompanyIds().Result;
                    r.GovernmentPrograms = ds2.Tables[0].Rows.Cast<DataRow>().Select(spR => new GovernmentProgram()
                    {
                        Id = int.Parse(spR["Id"] == DBNull.Value ? "0" : spR["Id"].ToString()),
                        Name = spR["Name"] == DBNull.Value ? "" : (string)spR["Name"],
                        InsuranceID = int.Parse(dt.Rows[0][0] == DBNull.Value ? "0" : dt.Rows[0][0].ToString())
                    }).ToList();
                }
                ds = await getClientDetailsByClientid(clientId, policyId, tierId, insuranceFlag);
                dsPayment = await getClaimsPaymentByClientid(clientId, policyId);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            if (r.er.code == 0 && ds.Tables[0].Rows.Count > 0)
            {
                DataRow dr = ds.Tables[0].Rows[0];
                DateTime dosOld = DateTime.MinValue;

                //ds.Tables[0].DefaultView.RowFilter = "ClaimId =" + claimId;
                DataTable dt = (ds.Tables[0].DefaultView).ToTable();
                int iOld = 0;

                for (int i = 0; i < dt.Rows.Count; i++)
                {

                    DataRow drDet = dt.Rows[i];
                    ClientDetails rc = new ClientDetails
                    {
                        dos = Convert.ToDateTime(drDet["DateOfService"] == DBNull.Value ? default(DateTime) : drDet["DateOfService"])
                    };
                    List<ClaimComment> comments = drDet["Comments"] != DBNull.Value ? JsonConvert.DeserializeObject<List<ClaimComment>>(drDet["Comments"].ToString()) : new List<ClaimComment>();
                    ClientDetailInfo cdi = new ClientDetailInfo
                    {
                        provider = Convert.ToString(drDet["Provider"]),
                        cptCode = Convert.ToString(drDet["CPTCode"]),
                        billedAmount = Math.Round(Convert.ToDecimal(drDet["AmtBilled"] == DBNull.Value ? 0 : drDet["AmtBilled"]), 2),
                        paidAmount = Math.Round(Convert.ToDecimal(drDet["PaidAmt"] == DBNull.Value ? 0 : drDet["PaidAmt"]), 2),
                        policyNumber = Convert.ToString(drDet["PolicyNbr"]),
                        groupNumber = Convert.ToString(drDet["GroupNbr"]),
                        dddStartDate = (Convert.ToDateTime(drDet["DDDStart"] == DBNull.Value ? default(DateTime) : drDet["DDDStart"])).ToString("MM/dd/yy"),
                        dddEndDate = (Convert.ToDateTime(drDet["DDDEnd"] == DBNull.Value ? default(DateTime) : drDet["DDDEnd"])).ToString("MM/dd/yy"),
                        StartDate = (Convert.ToDateTime(drDet["DDDStart"] == DBNull.Value ? default(DateTime) : drDet["DDDStart"])),
                        EndDate = (Convert.ToDateTime(drDet["DDDEnd"] == DBNull.Value ? default(DateTime) : drDet["DDDEnd"])),
                        dddUnit = Convert.ToString(drDet["DDDUnits"]),
                        claimId = Convert.ToInt64(drDet["ClaimID"] == DBNull.Value ? 0 : drDet["ClaimID"]),
                        comments = comments,
                        InsurancePolicyId = Convert.ToInt32(drDet["InsurancePolicyId"] == DBNull.Value ? 0 : drDet["InsurancePolicyId"]),
                        ClaimStatusID = Convert.ToInt32(drDet["ClaimStatusID"] == DBNull.Value ? 0 : drDet["ClaimStatusID"]),
                        Client = Convert.ToString(drDet["Client"]),
                        paymentId = Convert.ToInt32(drDet["PaymentId"] == DBNull.Value ? 0 : drDet["PaymentId"]),
                        PresStart = (Convert.ToDateTime(drDet["PresStart"] == DBNull.Value ? default(DateTime) : drDet["PresStart"])).ToString("MM/dd/yy"),
                        PresEnd = (Convert.ToDateTime(drDet["PresEnd"] == DBNull.Value ? default(DateTime) : drDet["PresEnd"])).ToString("MM/dd/yy"),
                        PAuthStart = (Convert.ToDateTime(drDet["PAuthStart"] == DBNull.Value ? default(DateTime) : drDet["PAuthStart"])).ToString("MM/dd/yy"),
                        PAuthEnd = (Convert.ToDateTime(drDet["PAuthEnd"] == DBNull.Value ? default(DateTime) : drDet["PAuthEnd"])).ToString("MM/dd/yy"),
                        serviceId = Convert.ToInt32(drDet["serviceId"] == DBNull.Value ? 0 : drDet["serviceId"]),
                        staffId = Convert.ToInt32(drDet["prID"] == DBNull.Value ? 0 : drDet["prID"]),
                        clientSessionTherapyID = Convert.ToInt32(drDet["ClientSessionTherapyID"] == DBNull.Value ? 0 : drDet["ClientSessionTherapyID"]),
                        clientChartFileId = Convert.ToInt32(drDet["chartId"] == DBNull.Value ? 0 : drDet["chartId"]),
                        clientChartFileExtension = Convert.ToString(drDet["ChartFileExtension"] == DBNull.Value ? "" : drDet["ChartFileExtension"]),
                        clientChartFileName = Convert.ToString(drDet["ChartFileName"] == DBNull.Value ? "" : drDet["ChartFileName"]),
                        progressReportFileId = Convert.ToInt32(drDet["progressTherapyId"] == DBNull.Value ? 0 : drDet["progressTherapyId"]),
                        progressReportFileExtension = Convert.ToString(drDet["ProgressFileExtension"] == DBNull.Value ? "" : drDet["ProgressFileExtension"]),
                        progressReportFileName = Convert.ToString(drDet["ProgressFileName"] == DBNull.Value ? "" : drDet["ProgressFileName"]),
                        noteTherapyFileId = Convert.ToInt32(drDet["clTherapyNoteId"] == DBNull.Value ? 0 : drDet["clTherapyNoteId"]),
                        noteTherapyFileExtension = Convert.ToString(drDet["NoteExtension"] == DBNull.Value ? "" : drDet["NoteExtension"]),
                        noteTherapyFileName = Convert.ToString(drDet["NoteFileName"] == DBNull.Value ? "" : drDet["NoteFileName"]),
                        insurancePriority = Convert.ToInt32(drDet["InsurancePriorityId"] == DBNull.Value ? 0 : drDet["InsurancePriorityId"]),
                        therapistSupervisor = Convert.ToString(drDet["TherapistSupervisor"] == DBNull.Value ? "" : drDet["TherapistSupervisor"]),
                        LocationTypeId = Convert.ToInt32(drDet["LocationTypeId"] == DBNull.Value ? 0 : drDet["LocationTypeId"]),
                        DenialReason = Convert.ToString(drDet["DenialReason"] == DBNull.Value ? "" : drDet["DenialReason"])
                    };
                    if (!forHCFA) cdi.DenialReasonText = r.DenialReasonList.Where(d => d.id == cdi.DenialReason).Select(d => d.id + ": " + d.name).FirstOrDefault();
                    cdi.allowedAmount = Math.Round(Convert.ToDecimal(drDet["AllowedAmt"] == DBNull.Value ? 0 : drDet["AllowedAmt"]), 2);
                    cdi.coInsuranceAmount = Math.Round(Convert.ToDecimal(drDet["CoInsuranceAmt"] == DBNull.Value ? 0 : drDet["CoInsuranceAmt"]), 2);
                    if (rc.dos != dosOld || dosOld == DateTime.MinValue)
                    {
                        r.ClientDetailsList.Add(rc);
                        iOld = i;
                        r.ClientDetailsList[r.ClientDetailsList.Count - 1].ClientDetailsInfoList = new List<ClientDetailInfo>();
                    }
                    r.ClientDetailsList[r.ClientDetailsList.Count - 1].ClientDetailsInfoList.Add(cdi);
                    dosOld = rc.dos;

                }
                if (!forHCFA)
                {
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                        {
                            //need to add the storeproc to get Insurance
                            SqlCommand cmd = new SqlCommand("sp_InsuranceGetInsuranceList", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            sqlHelper.ExecuteSqlDataAdapter(cmd, dsDccMain);
                        }
                    });
                    r.companyInsuranceList = dsDccMain.Tables[0].Rows.Cast<DataRow>().Select(spR => new Option()
                    {
                        value = Convert.ToString(spR["InsuranceCompanyId"]),
                        name = (string)spR["Name"]
                    }).ToList();
                }
                DataTable dtPayments = (dsPayment.Tables[0].DefaultView).ToTable();
                r.ClaimPaymentList = new List<ClaimPayment>();
                for (int i = 0; i < dtPayments.Rows.Count; i++)
                {

                    DataRow drPayment = dtPayments.Rows[i];//ds.Tables[0].Rows[i]; Code Change
                    ClaimPayment cdi = new ClaimPayment();
                    cdi.DOS = (Convert.ToDateTime(drPayment["DOS"] == DBNull.Value ? default(DateTime) : drPayment["DOS"])).ToString("MM/dd/yy");
                    cdi.Code = Convert.ToString(drPayment["Code"]);
                    cdi.Amount = Math.Round(Convert.ToDecimal(drPayment["Amount"] == DBNull.Value ? 0 : drPayment["Amount"]), 2);
                    cdi.ClaimId = Convert.ToInt32(drPayment["ClaimId"] == DBNull.Value ? 0 : drPayment["ClaimId"]);
                    cdi.GovernmentProgramId = Convert.ToInt32(drPayment["GovernmentProgramId"] == DBNull.Value ? 0 : drPayment["GovernmentProgramId"]);
                    cdi.InsurancePolicyId = Convert.ToInt32(drPayment["InsurancePolicyId"] == DBNull.Value ? 0 : drPayment["InsurancePolicyId"]);
                    cdi.InsuredIdNo = Convert.ToString(drPayment["InsuredIdNo"]);
                    cdi.InsuranceCompanyId = Convert.ToInt32(drPayment["InsuranceCompanyId"] == DBNull.Value ? 0 : drPayment["InsuranceCompanyId"]);
                    cdi.IsDenial = Convert.ToBoolean(drPayment["IsDenial"]);
                    cdi.PaymentId = Convert.ToInt32(drPayment["PaymentId"] == DBNull.Value ? 0 : drPayment["PaymentId"]);
                    cdi.Payer = Convert.ToString(drPayment["Payer"]);
                    cdi.Notes = Convert.ToString(drPayment["Notes"]);
                    cdi.ReceivedAt = (Convert.ToDateTime(drPayment["ReceivedAt"] == DBNull.Value ? default(DateTime) : drPayment["ReceivedAt"]));//.ToString("MM/dd/yy");
                    cdi.PaymentTypeId = Convert.ToInt32(drPayment["PaymentTypeId"] == DBNull.Value ? 0 : drPayment["PaymentTypeId"]);
                    cdi.VoidedAt = (Convert.ToDateTime(drPayment["VoidedAt"] == DBNull.Value ? default(DateTime) : drPayment["VoidedAt"]));//.ToString("MM/dd/yy");
                    cdi.DenialReasonId = Convert.ToInt32(drPayment["DenialReasonId"] == DBNull.Value ? 0 : drPayment["DenialReasonId"]);
                    cdi.ReasonText = Convert.ToString(drPayment["ReasonText"]);
                    cdi.StaffId = Convert.ToInt32(drPayment["StaffId"] == DBNull.Value ? 0 : drPayment["StaffId"]);
                    cdi.OABatchID = Convert.ToInt64(drPayment["BatchID"] == DBNull.Value ? 0 : drPayment["BatchID"]);
                    cdi.HCFAFileId = Convert.ToInt32(drPayment["HCFAFileId"] == DBNull.Value ? 0 : drPayment["HCFAFileId"]);
                    r.ClaimPaymentList.Add(cdi);
                }
                if (!forHCFA)
                {
                    DataSet ds1 = await getClaimStatusList();

                    r.claimStatusList = ds1.Tables[0].Rows.Cast<DataRow>().Select(spR => new ClaimsStatus()
                    {
                        claimstatusid = int.Parse(spR["ClaimStatusid"].ToString()),
                        name = spR["Name"] == DBNull.Value ? "" : (string)spR["Name"],

                    }).ToList();
                }
                if (!forHCFA)
                {
                    dsAmount = await getClaimsPaymentAmounts(clientId);
                    r.OtherClaimPayments = dsAmount.Tables[0].Rows.Cast<DataRow>().Select(drPa => new PaymentAmount()
                    {
                        allowedAmount = Convert.ToDecimal(drPa["AllowedAmount"] == DBNull.Value ? 0 : drPa["AllowedAmount"]),
                        billedamount = Convert.ToDecimal(drPa["BilledAmount"] == DBNull.Value ? 0 : drPa["BilledAmount"]),
                        claimId = Convert.ToInt64(drPa["ClaimID"] == DBNull.Value ? 0 : drPa["ClaimID"]),
                        clientSessionTherapyId = Convert.ToInt64(drPa["ClientSessionTherapyId"] == DBNull.Value ? 0 : drPa["ClientSessionTherapyId"]),
                        //claimStatusId = 1,
                        insurancePriority = Convert.ToInt32(drPa["InsurancePriorityId"] == DBNull.Value ? 0 : drPa["InsurancePriorityId"]),
                        coInsuranceAmount = Convert.ToDecimal(drPa["CoInsuranceAmount"] == DBNull.Value ? 0 : drPa["CoInsuranceAmount"]),
                        paidAmount = Convert.ToDecimal(drPa["PaidAmount"] == DBNull.Value ? 0 : drPa["PaidAmount"]),
                        policyId = Convert.ToInt32(drPa["InsurancePolicyId"] == DBNull.Value ? 0 : drPa["InsurancePolicyId"])
                    }).ToList();
                }
            }
            if (ds != null)
                ds.Dispose();
            return r;
        }
        private static string GetRelationshipCode(int InsuranceRelationshipId)
        {
            string relationshipCode;
            switch (InsuranceRelationshipId)
            {
                case 0:// (int)InsuranceRelationshipEnum.Self:
                    relationshipCode = "18";
                    break;
                case 1:// (int)InsuranceRelationshipEnum.Spouse:
                    relationshipCode = "01";
                    break;
                case 2:// (int)InsuranceRelationshipEnum.Child:
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
        [Authorize]
        public async Task<ActionResult> GetHCFA(string clientId, string selrowid, string selectedClientIndex, string policyId, string companyId, int claimId = 0, int tierId = 0)
        {
            string str = null;

            ProviderInit r = await generateModel(clientId, selrowid, int.Parse(selectedClientIndex), policyId, companyId, claimId, tierId, 0, true);
            StringBuilder stringb = HCFA(r, int.Parse(clientId), int.Parse(string.IsNullOrEmpty(companyId) ? "0" : companyId), claimId, out str);
            string appPath = Request.PhysicalApplicationPath;
            string filePath = appPath + "HCFA.txt";

            //this code section write stringbuilder content to physical text file.
            //using (StreamWriter swriter = new StreamWriter(filePath))
            //{
            //    swriter..Write(stringb.ToString());
            //}
            //FileData f = new FileData("charts", UserClaim.blobStorage);
            //swriter
            //byte[] data = f.GetFile(filePath);
            //Response.AddHeader("Content-Disposition", "attachment;filename=" + "HCFA.txt");

            //return new FileContentResult(data, "text/plain");

            return File(new System.Text.UTF8Encoding().GetBytes(stringb.ToString()), "text/plain", "HCFA.txt");
        }

        [Authorize]
        public async Task<ActionResult> _GetMultiHCFA(string clientIds, string companyId, int status, bool selfpay = false, int tierId = -1, int insuranceFlag = 0)
        {
            byte[] fileBytes = null;
            int index = 0;
            string key = Guid.NewGuid().ToString();
            using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream())
            {
                using (System.IO.Compression.ZipArchive zip = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    foreach (var clientId in clientIds.Split(','))
                    {
                        string str;
                        string selrowid = "dtitle-Payor_" + index.ToString() + "_Client_" + clientId;
                        string policyId = companyId;
                        DataSet ds1 = await getClientDetailsByClientid(clientId.ToString(), policyId, tierId, insuranceFlag);

                        ds1.Tables[0].DefaultView.RowFilter = status == -2 ? "" : status == -1 ? "ClaimStatusID = 1" : (status == 100 ? "ClaimStatusID = 0" : "ClaimStatusID = " + status);
                        DataTable dt1 = (ds1.Tables[0].DefaultView).ToTable();
                        foreach (DataRow dr in dt1.Rows)
                        {
                            int claimId = Convert.ToInt32(dr["ClaimID"] == DBNull.Value ? 0 : dr["ClaimID"]);
                            ProviderInit r1 = await generateModel(clientId.ToString(), selrowid, index, policyId, companyId, claimId, tierId, 0, true);
                            StringBuilder stringb = HCFA(r1, int.Parse(clientId), int.Parse(string.IsNullOrEmpty(companyId) ? "0" : companyId), claimId, out str);

                            if (stringb.Length > 0)
                            {
                                System.IO.Compression.ZipArchiveEntry zipItem = zip.CreateEntry("HCFA_" + claimId + ".txt");
                                using (System.IO.MemoryStream originalFileMemoryStream = new System.IO.MemoryStream(Encoding.ASCII.GetBytes(stringb.ToString())))
                                {
                                    using (System.IO.Stream entryStream = zipItem.Open())
                                    {
                                        originalFileMemoryStream.CopyTo(entryStream);
                                    }
                                }
                            }
                        }
                    }
                }
                fileBytes = memoryStream.ToArray();
                TempData[key] = fileBytes;
            }
            return Json(new JsonResult()
            {
                Data = new { FileKey = key, FileName = string.Format("MultipleHCFA_{0}.zip", DateTime.Now.Ticks), FileType = "application/zip" }
            }, JsonRequestBehavior.AllowGet);

            //Response.AddHeader("Content-Disposition", "attachment; filename=multipleHCFA.zip");
            //return new FileContentResult(fileBytes, "application/zip");
        }

        [Authorize]
        public async Task<ActionResult> GetMultiHCFA(string clientIds, string companyId, int status, bool selfpay = false, int tierId = -1, int insuranceFlag = 0, bool isGovt = false, int claimId = 0, string claimIds = null)
        {
            if (status == 100) status = isGovt ? 3 : 1;
            string key = Guid.NewGuid().ToString();
            string mimeType = ""; string extension = ""; string fileName = "";
            bool success = false;

            CompanyInfoDTO company = GetCompanyDetails(UserClaim.coid);
            List<ClientClaimDTO> claims = GetClaims(clientIds, int.Parse(companyId), claimId, claimIds).Where(c => c.Claim.StatusId == (status == -2 ? c.Claim.StatusId : status)).Select(c => c).ToList();
            Dictionary<int, string> errors = new Dictionary<int, string>();

            try
            {
                if (!isGovt || claimId != 0)
                {
                    extension = "txt";
                    fileName = string.Format("MultipleHCFA_{0}.{1}", DateTime.Now.Ticks, extension);
                    mimeType = "text/plain";
                    List<BillingInsuranceCompanyDTO> insComp = GetBillingInsuranceCompanies();
                    Dictionary<int, List<CredentialDTO>> credentials = new Dictionary<int, List<CredentialDTO>>();
                    Dictionary<int, StaffDTO> staffs = GetStaffDetails(claims.SelectMany(c => new[] { c.Claim.ApproverUserId, c.Claim.StaffUserID }).ToList());
                    List<ClientDTO> clients = string.IsNullOrEmpty(clientIds) ? null : GetClients(clientIds);
                    if (clients == null)
                    {
                        clientIds = claims.Select(c => c.Claim.ClientId.ToString()).Distinct().Aggregate((a, b) => string.Format("{0},{1}", a, b));
                        clients = GetClients(clientIds);
                    }
                    StringBuilder stringb = HCFA(insComp, clients, company, claims, UserClaim.uid.ToString(), staffs, credentials, out errors, 0, out string groupTracking);
                    var hcfabytes = new System.Text.UTF8Encoding().GetBytes(stringb.ToString());
                    if (hcfabytes.Length > 0) success = true;
                    TempData[key] = hcfabytes;
                }
                else
                {
                    extension = "xls";
                    mimeType = "application/xls";
                    fileName = string.Format("{0}{1}{2}{3}.{4}", company.DDDPrefix, DateTime.Now.ToString("yy"), DateTime.Now.ToString("MM"), "001", extension);

                    var ids = (from c in claims where c.Claim != null && c.Claim.StatusId != (int)ClaimStatusEnum.PendingWaiver select c.ClaimId);
                    CompanyData companyData = new CompanyData(UserClaim.conStr);
                    List<ClaimDTO> claimsdetails = new List<ClaimDTO>();
                    claimsdetails = companyData.ListClaimsWithFullInfo(string.Join(",", ids));
                    AccountData _account = new AccountData() { CompanyID = UserClaim.coid, NPI = UserClaim.npi, TaxId = company.TaxId, ProvAhcccsId = company.ProvAhcccsId };
                    var exporter = new ClaimListExport(claimsdetails, _account.CompanyID, out errors, true, false, false);
                    var renderedBytes = ExportUtilities.ToExcel(exporter.AZMedicaidClaimSubmission(_account.NPI, _account.TaxId, _account.ProvAhcccsId), out mimeType, out extension);
                    if (renderedBytes.Length > 0) success = true;
                    TempData[key] = renderedBytes;
                }
            }
            catch (Exception ex)
            {
                errors.Add(-1, ex.ToString());
            }
            string error = errors == null || !errors.Any() ? "" : String.Join("\n", errors.Select(err => err.Value));
            return Json(new JsonResult()
            {
                Data = new { FileKey = key, FileName = fileName, FileType = mimeType, Succeed = success, Error = error, Claims = !claims.Any() ? "" : claims.Select(c => c.ClaimId.ToString()).Distinct().Aggregate((a, b) => string.Format("{0},{1}", a, b)) }
            }, JsonRequestBehavior.AllowGet);

            //return File(new System.Text.UTF8Encoding().GetBytes(stringb.ToString()), "text/plain", "HCFA.txt");
        }

        [HttpGet]
        public virtual ActionResult Download(string fileKey, string fileName, string fileType)
        {
            if (TempData[fileKey] != null)
            {
                byte[] data = TempData[fileKey] as byte[];
                //return File(data, "application/zip", fileName);

                Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);
                return new FileContentResult(data, fileType);
            }
            else
            {
                return new EmptyResult();
            }
        }

        [HttpGet]
        public virtual ActionResult StartDownload(string fileKey, string fileName, string fileType, string coversheetKey = null, bool isDDD = false)
        {
            CompanyInfoDTO company = GetCompanyDetails(UserClaim.coid);
            string blobConnection = company.BlobStorageConnection;

            if (isDDD)
            {
                if (TempData[fileKey] != null && TempData[coversheetKey] != null)
                {
                    var coversheetName = string.Empty;
                    byte[] excelBytes = TempData[fileKey] as byte[];
                    byte[] coversheetBytes = TempData[coversheetKey] as byte[];
                    List<byte[]> fileBytes = new List<byte[]>();
                    if (excelBytes != null) { fileBytes.Add(excelBytes); }
                    if (coversheetBytes != null) { fileBytes.Add(coversheetBytes); }

                    //Upload file to BLOB
                    var azureHelper = new AzureHelper("DDDFiles", blobConnection);
                    //add excel
                    azureHelper.AddToBlob(excelBytes, string.Format("{0}", fileName));
                    //add coversheet
                    coversheetName = fileName.Replace("xls", "pdf");
                    azureHelper.AddToBlob(coversheetBytes, string.Format("{0}", coversheetName));

                    Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName + ".zip");
                    Response.ContentType = "application/zip";

                    using (var zipStream = new ZipOutputStream(Response.OutputStream))
                    {
                        int count = 0;
                        foreach (byte[] file in fileBytes)
                        {
                            if (count == 1)
                            {
                                fileName = fileName.Replace("xls", "pdf");
                            }
                            var fileEntry = new ZipEntry(fileName)
                            {
                                Size = file.Length
                            };

                            zipStream.PutNextEntry(fileEntry);
                            zipStream.Write(file, 0, file.Length);
                            count++;
                        }
                        zipStream.Flush();
                        zipStream.Close();
                        var zipBytes = new UTF8Encoding().GetBytes(Response.OutputStream.ToString());

                        return new FileContentResult(zipBytes, fileType);
                    }
                }
                else
                {
                    return new EmptyResult();
                }
            }
            else
            {
                if (TempData[fileKey] != null)
                {
                    byte[] data = TempData[fileKey] as byte[];
                    //return File(data, "application/zip", fileName);

                    //upload HCFAfile to blob
                    var azureHelper = new AzureHelper("OAFiles", blobConnection);
                    azureHelper.AddToBlob(data, string.Format("{0}", fileName));

                    Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);
                    return new FileContentResult(data, fileType);
                }
                else
                {
                    return new EmptyResult();
                }
            }
        }

        [Authorize]
        public async Task<ActionResult> GetProgressReport(string id)
        {
            Er er = new Er();
            string commandStr = "SELECT [fileName],[fileExtension] FROM [dbo].[ClientProgressReportTherapy] WHERE [progressTherapyId] =" + id + ";";
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
                    string fileName = ds.Tables[0].Rows[0].ItemArray[0].ToString();// + ds.Tables[0].Rows[0].ItemArray[1].ToString();
                    //string filePath = id + ds.Tables[0].Rows[0].ItemArray[1].ToString();
                    //string filePath = "progressreports/"+ fileName;
                    FileData f = new FileData("progressreports", UserClaim.blobStorage);

                    byte[] data = f.GetFile(fileName);
                    Response.AddHeader("Content-Disposition", "attachment;filename=" + fileName);

                    return new FileContentResult(data, ds.Tables[0].Rows[0].ItemArray[1].ToString());
                }
            }

            return View(er);
        }

        [Authorize]
        public async Task<ActionResult> GetClientNote(string id)
        {
            Er er = new Er();
            string commandStr = "SELECT [fileName],[fileExtension]  FROM [dbo].[ClientNotesTherapy] WHERE [clTherapyNoteId]=" + id + ";";
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
                    string fileName = ds.Tables[0].Rows[0].ItemArray[0].ToString();// + ds.Tables[0].Rows[0].ItemArray[1].ToString();
                    string filePath = id + ds.Tables[0].Rows[0].ItemArray[1].ToString();
                    FileData f = new FileData("sessionnotes", UserClaim.blobStorage);

                    byte[] data = f.GetFile(fileName);
                    Response.AddHeader("Content-Disposition", "attachment;filename=" + fileName);
                    string extension = "";
                    if (fileName.Contains("."))
                    {
                        extension = fileName.Substring(fileName.LastIndexOf("."), fileName.Length - fileName.LastIndexOf("."));
                    }
                    else if (ds.Tables[0].Rows[0].ItemArray[1] != null)
                    {
                        extension = ds.Tables[0].Rows[0].ItemArray[1].ToString();
                    }
                    return new FileContentResult(data, extension);
                }
            }

            return View(er);
        }
        [Authorize]
        public async Task<ActionResult> GetChartZip(string chart, string chartextension, string progressreport, string prextension, string clientnote, string cnextension, string clientId, string selrowid, string selectedClientIndex, string policyId, string companyId, int claimId = 0, int tierId = 0)
        {
            List<SourceFile> sourceFiles = new List<SourceFile>();
            if (!string.IsNullOrEmpty(chart) && chart != "0")
            {
                try
                {
                    FileData f = new FileData("charts", UserClaim.blobStorage);

                    byte[] data = f.GetFile(chart);
                    sourceFiles.Add(new SourceFile() { Extension = chartextension, FileBytes = data, Name = chart });
                }
                catch { }
            }
            if (!string.IsNullOrEmpty(progressreport))
            {
                try
                {
                    FileData f = new FileData("progressreports", UserClaim.blobStorage);

                    byte[] data = f.GetFile(progressreport);
                    sourceFiles.Add(new SourceFile() { Extension = prextension, FileBytes = data, Name = progressreport });
                }
                catch { }
            }
            if (!string.IsNullOrEmpty(clientnote))
            {
                try
                {
                    FileData f = new FileData("sessionnotes", UserClaim.blobStorage);

                    byte[] data = f.GetFile(clientnote);
                    sourceFiles.Add(new SourceFile() { Extension = cnextension, FileBytes = data, Name = clientnote });
                }
                catch { }
            }
            string str;
            ProviderInit r = await generateModel(clientId, selrowid, int.Parse(selectedClientIndex), policyId, companyId, claimId, tierId);
            StringBuilder stringb = HCFA(r, int.Parse(clientId), int.Parse(string.IsNullOrEmpty(companyId) ? "0" : companyId), claimId, out str);

            // the output bytes of the zip
            byte[] fileBytes = null;

            // create a working memory stream
            using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream())
            {
                // create a zip
                using (System.IO.Compression.ZipArchive zip = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    // interate through the source files
                    foreach (SourceFile f in sourceFiles)
                    {
                        // add the item name to the zip
                        System.IO.Compression.ZipArchiveEntry zipItem = zip.CreateEntry(f.Name + "." + f.Extension);
                        // add the item bytes to the zip entry by opening the original file and copying the bytes 
                        using (System.IO.MemoryStream originalFileMemoryStream = new System.IO.MemoryStream(f.FileBytes))
                        {
                            using (System.IO.Stream entryStream = zipItem.Open())
                            {
                                originalFileMemoryStream.CopyTo(entryStream);
                            }
                        }
                    }
                    if (stringb.Length > 0)
                    {
                        // add the item name to the zip
                        System.IO.Compression.ZipArchiveEntry zipItem = zip.CreateEntry("HCFA.txt");
                        // add the item bytes to the zip entry by opening the original file and copying the bytes 
                        using (System.IO.MemoryStream originalFileMemoryStream = new System.IO.MemoryStream(Encoding.ASCII.GetBytes(stringb.ToString())))
                        {
                            using (System.IO.Stream entryStream = zipItem.Open())
                            {
                                originalFileMemoryStream.CopyTo(entryStream);
                            }
                        }
                    }
                }
                fileBytes = memoryStream.ToArray();
            }

            // download the constructed zip
            Response.AddHeader("Content-Disposition", "attachment; filename=download.zip");
            return new FileContentResult(fileBytes, "application/zip");
        }
        public class SourceFile
        {
            public string Name { get; set; }
            public string Extension { get; set; }
            public Byte[] FileBytes { get; set; }
        }

        public List<Option> LoadService(int clientId)
        {
            DataSet ds2 = new DataSet();
            SqlConnection cn = new SqlConnection(UserClaim.conStr);
            SqlCommand cmd2 = new SqlCommand("sp_ClientsServiceList", cn)
            {
                CommandType = CommandType.StoredProcedure
            };
            sqlHelper.ExecuteSqlDataAdapter(cmd2, ds2);

            List<Option> r = ds2.Tables[0].Rows.Cast<DataRow>().Select(spR => new Option()
            {

                value = Convert.ToString(spR["serviceId"]),
                name = (string)spR["Name"]
            }).ToList();

            return r;
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public ActionResult UpdatePendingWaiver(int clientid, DateTime startDate, bool changeToPW)
        {
            ProviderInit r = new ProviderInit();
            string msg = "";
            try
            {
                msg = updateStatusToPendingWaiver(clientid, startDate, changeToPW);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            return Json(new { message = msg });

        }
        private string updateStatusToPendingWaiver(int clientId, DateTime startDate, bool changeToPW)
        {
            string msg = "Success";
            try
            {
                SqlConnection cn = new SqlConnection(UserClaim.conStr);
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_UpdateClaimStatusToPendingWaiver", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                cmd.Parameters.AddWithValue("@ClientID", clientId);
                cmd.Parameters.AddWithValue("@Status", changeToPW ? 7 : 0);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return msg;
        }

        [HttpGet]
        public ActionResult ReconcileGovt()
        {
            ReconcileResponse model = new ReconcileResponse();
            return View(model);
        }


        [HttpPost]
        //[ValidateJsonAntiForgeryToken]
        public ActionResult ReconcileGovtResults(HttpPostedFileBase file)
        {
            ReconcileResponse rsp = new ReconcileResponse();
            bool fileGood = true;
            if (file == null || file.ContentLength == 0)
            {
                rsp.ErrorMessages.Add("Invalid file found!");
                fileGood = false;
            }
            if (ModelState.IsValid && fileGood)
            {
                ReconcileRequest req = new ReconcileRequest() { Source = "Arizona DDD" };
                if (file != null && file.ContentLength > 0)
                {
                    req.FileData = new byte[file.InputStream.Length];
                    file.InputStream.Read(req.FileData, 0, req.FileData.Length);
                }
                try
                {
                    rsp = govtReconcile(req);
                }
                catch (Exception ex)
                {
                    rsp.ErrorMessages.Add(ex.Message);
                    //throw ex;
                }
                finally
                {
                }

                if (!rsp.ErrorMessages.Any())
                {
                    //return PartialView(rsp);
                }
            }
            return PartialView(rsp);
        }

        private ReconcileResponse govtReconcile(ReconcileRequest request)
        {

            var rsp = new ReconcileResponse();

            Dictionary<string, ClaimInfo> claims = new Dictionary<string, ClaimInfo>();
            ClaimReconciler advancer = null;
            PaymentInfo payment = null;
            int staffId = 0;
            #region Validation

            try
            {
                advancer = new ClaimReconciler(UserClaim); 
                staffId = UserClaim.prid; 
                switch (request.Source)
                {
                    case "Arizona DDD":
                        payment = advancer.ProcessAZMedicaid(request.FileData, ref rsp, claims);
                        break;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                advancer.Dispose();
            }
            #endregion

            if (rsp.ErrorMessages.Count > 0)
            {
                rsp.IsFailure = true;
                return rsp;
            }
            if ((payment == null || payment.Amount == 0) && claims.Count == 0)
            {
                return rsp;
            }
            payment.ProcessedBy = new GenericEntity()
            {
                UniqueId = UserClaim.uid.ToString(),
                Context = "User",
                Name = UserClaim.userName
            };
            try
            {
                if (payment != null && payment.Amount > 0)
                {
                    payment.MadeOn = DateTime.Now;
                    long pid = createPayment(payment);
                    foreach (var cp in payment.Claims)
                    {
                        cp.PaymentId = pid;
                        recordClaimPayment(claims[cp.Claim.UniqueId], cp);
                    }
                }
                foreach (var claim in claims)
                {
                    if (payment.Claims.Exists(cp => cp.Claim.UniqueId == claim.Key)) continue;
                    setClaimStatus(claim.Value);
                    var cmt = claim.Value.Comments.Find(c => c.CommentId == "NEW");
                    if (cmt != null) createClaimComment(claim.Value.ClaimId, staffId, cmt.CommentText);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
            return rsp;
        }

        public long createPayment(PaymentInfo payment)
        {
            long result = 0;
            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                cn.Open();
                SqlCommand myCmd = new SqlCommand("Payment_i_sp", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                myCmd.Parameters.AddWithValue("@Type", payment.Type);
                myCmd.Parameters.AddWithValue("@User", payment.ProcessedBy == null ? null : payment.ProcessedBy.UniqueId);
                myCmd.Parameters.AddWithValue("@Notes", payment.Notes);
                myCmd.Parameters.AddWithValue("@Transaction", payment.TransactionId == null ? "" : payment.TransactionId);
                myCmd.Parameters.AddWithValue("@Payer", payment.Description);
                myCmd.Parameters.AddWithValue("@Amount", payment.Amount);
                myCmd.Parameters.AddWithValue("@Date", payment.MadeOn);
                var pr = myCmd.Parameters.Add("@ID", SqlDbType.BigInt);
                myCmd.Parameters["@ID"].Direction = ParameterDirection.Output;

                try
                {
                    myCmd.ExecuteNonQuery();
                    result = (long)pr.Value;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return result;
        }

        public void recordClaimPayment(ClaimInfo claim, ClaimPaymentRC payment)
        {
            var sb = new StringBuilder();
            foreach (var a in claim.Appointments)
            {
                sb.AppendFormat("{0}:{1};", a.AppointmentId, a.Amount);
            }
            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                cn.Open();
                SqlCommand myCmd = new SqlCommand("Claim_u2_sp", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                myCmd.Parameters.AddWithValue("@Status", (int)claim.Status);
                myCmd.Parameters.AddWithValue("@Policy", claim.PendingWith.UniqueId);
                myCmd.Parameters.AddWithValue("@ID", claim.ClaimId);

                myCmd.Parameters.AddWithValue("@Amounts", sb.ToString());
                myCmd.Parameters.AddWithValue("@Paid", payment.Amount);
                myCmd.Parameters.AddWithValue("@Payment", payment.PaymentId);
                myCmd.Parameters.AddWithValue("@Denial", payment.Denial);
                myCmd.Parameters.AddWithValue("@PaidPolicy", claim.PendingWith.UniqueId);
                myCmd.Parameters.AddWithValue("@Govt", payment.Source.Context == "GovtProgram" ? payment.Source.UniqueId : null);
                myCmd.Parameters.AddWithValue("@DenialReason", payment.DenialReason == null ? "" : payment.DenialReason.UniqueId);
                myCmd.Parameters.AddWithValue("@Activity", claim.ActivityId);
                myCmd.Parameters.AddWithValue("@TransactionId", claim.TransactionId);

                try
                {
                    myCmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public void setClaimStatus(ClaimInfo claim)
        {
            var sb = new StringBuilder();
            foreach (var a in claim.Appointments)
            {
                sb.AppendFormat("{0}:{1};", a.AppointmentId, a.Amount);
            }
            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                cn.Open();
                SqlCommand myCmd = new SqlCommand("Claim_u_sp", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                //SqlCommand myCmd = this.CreateSPCommand("Claim_u_sp");
                myCmd.Parameters.AddWithValue("@Status", claim.Status);
                myCmd.Parameters.AddWithValue("@Policy", claim.Status == ClaimStatusEnum.PendInsSubmission || claim.Status == ClaimStatusEnum.PendInsPay || claim.Status == ClaimStatusEnum.PendingWaiver ? claim.PendingWith.UniqueId : null);
                myCmd.Parameters.AddWithValue("@ID", claim.ClaimId);
                myCmd.Parameters.AddWithValue("@Amounts", sb.ToString());


                try
                {
                    myCmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public void createClaimComment(long claim, int staff, string comment)
        {
            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                cn.Open();
                SqlCommand myCmd = new SqlCommand("ClaimComment_i_sp", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                myCmd.Parameters.AddWithValue("@Claim", claim);
                myCmd.Parameters.AddWithValue("@Staff", staff);
                myCmd.Parameters.AddWithValue("@Comment", comment);


                try
                {
                    myCmd.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> DownloadDDDFile(string clientIds, string companyId, int status, bool selfpay = false, int tierId = -1, int insuranceFlag = 0, bool isGovt = false, int claimId = 0, string claimIds = null, int useNewFormat = 0)
        {
            if (status == 100) status = isGovt ? 3 : 1;
            string key = Guid.NewGuid().ToString();
            string coversheetKey = Guid.NewGuid().ToString();
            string mimeType = string.Empty; string extension = string.Empty; string fileName = string.Empty;
            bool success = false, isDDD = false;

            CompanyInfoDTO company = GetCompanyDetails(UserClaim.coid);
            List<ClientClaimDTO> claims = GetClaims(clientIds, int.Parse(companyId), claimId, claimIds).Where(c => c.Claim.StatusId == (status == -2 ? c.Claim.StatusId : status)).Select(c => c).ToList();
            Dictionary<int, string> errors = new Dictionary<int, string>();

            try
            {
                if (!isGovt || claimId != 0)
                {
                    extension = "txt";
                    fileName = string.Format("MultipleHCFA_{0}.{1}", DateTime.Now.Ticks, extension);
                    mimeType = "text/plain";
                    List<BillingInsuranceCompanyDTO> insComp = GetBillingInsuranceCompanies();
                    Dictionary<int, List<CredentialDTO>> credentials = new Dictionary<int, List<CredentialDTO>>();
                    Dictionary<int, StaffDTO> staffs = GetStaffDetails(claims.SelectMany(c => new[] { c.Claim.ApproverUserId, c.Claim.StaffUserID }).ToList());
                    List<ClientDTO> clients = string.IsNullOrEmpty(clientIds) ? null : GetClients(clientIds);
                    if (clients == null)
                    {
                        clientIds = claims.Select(c => c.Claim.ClientId.ToString()).Distinct().Aggregate((a, b) => string.Format("{0},{1}", a, b));
                        clients = GetClients(clientIds);
                    }

                    // Check if company has address info otherwise HCFA should not be treated as valid one 
                    if (company != null)
                    {
                        if ((string.IsNullOrWhiteSpace(company.SkilledBillingAddress) || string.IsNullOrWhiteSpace(company.SkilledBillingCity) || string.IsNullOrWhiteSpace(company.SkilledBillingState) || string.IsNullOrWhiteSpace(company.SkilledBillingZipCode))
                            && (string.IsNullOrWhiteSpace(company.Address?.Line1) || string.IsNullOrWhiteSpace(company.Address?.City) || string.IsNullOrWhiteSpace(company.Address?.State) || string.IsNullOrWhiteSpace(company.Address?.PostalCode)))
                        {
                            errors.Add(0, "Company Address must be filled in to generate HCFA");
                        }
                        else
                        {
                            StringBuilder stringb = HCFA(insComp, clients, company, claims, UserClaim.uid.ToString(), staffs, credentials, out errors, 0, out string groupTracking);
                            var hcfabytes = new UTF8Encoding().GetBytes(stringb.ToString());
                            if (hcfabytes.Length > 0) success = true;
                            TempData[key] = hcfabytes;
                        }
                    }
                }
                else
                {
                    fileName = $"{GetDDDFileName(company)}";
                    var ids = (from c in claims where c.Claim != null && c.Claim.StatusId != (int)ClaimStatusEnum.PendingWaiver select c.ClaimId);
                    CompanyData companyData = new CompanyData(UserClaim.conStr);
                    List<ClaimDTO> claimsdetails = new List<ClaimDTO>();
                    claimsdetails = companyData.ListClaimsWithFullInfo(string.Join(",", ids));
                    AccountData _account = new AccountData() { CompanyID = UserClaim.coid, NPI = UserClaim.npi, TaxId = company.TaxId, ProvAhcccsId = company.ProvAhcccsId, Company = company };

                    if (useNewFormat == 0)
                    {
                        extension = "xls";
                        fileName += $".{ extension}";
                        mimeType = "application/xls";
                        var exporter = new ClaimListExport(claimsdetails, _account.CompanyID, out errors, true, false, false);
                        var renderedBytes = exporter.GetDDDFiles(_account);
                        if (renderedBytes[0].Length > 0) success = true;
                        TempData[key] = renderedBytes[0];
                        TempData[coversheetKey] = renderedBytes[1];
                        isDDD = true;
                    } else
                    {
                        extension = "txt";
                        fileName += $".{ extension}";
                        mimeType = "text/plain";
                        List<BillingInsuranceCompanyDTO> insComp = GetBillingInsuranceCompanies();

                        EDI837P EDI837Helper = new EDI837P()
                        {
                            isAZDDD = true,
                            currentUserId = UserClaim.uid,
                            billingCompanyName = _account.Company.Name,
                            billingCompanyNPI = _account.Company.NPI,
                            billingCompanyAHCCSId = _account.ProvAhcccsId,
                            billingCompanyTaxId = _account.TaxId,
                            billingCompanyEmail = _account.Company.SkilledBillingEmail,
                            billingCompanyPhone = _account.Company.SkilledBillingPhone,
                            billingCompanyAddress = new AddressDTO
                            {
                                Line1 = _account.Company.SkilledBillingAddress,
                                City = _account.Company.SkilledBillingCity,
                                State = _account.Company.SkilledBillingState,
                                PostalCode = _account.Company.SkilledBillingZipCode,
                            },
                            claims = claimsdetails.Select(claim => claim.ToEDIClaim(insComp, true)).ToList()
                        };

                        string rawString = EDI837Helper.GenerateEDI837P();
                        var edi837pBytes = new UTF8Encoding().GetBytes(rawString);
                        if (edi837pBytes.Length > 0) success = true;
                        TempData[key] = edi837pBytes;
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(-1, ex.ToString());
            }
            
            string error = errors == null || !errors.Any() ? "" : String.Join("\n", errors.Select(err => err.Value).Distinct());
            //remove problematic claims from the list of claims to be returned
            if (errors.Any()) 
            {
                claims.RemoveAll(c => errors.Any(err => err.Key == c.ClaimId));
            }
            return Json(new JsonResult()
            {
                Data = new { FileKey = key, CoverSheetKey = coversheetKey, FileName = fileName, FileType = mimeType, Succeed = success, Error = error, IsDDD = isDDD, Claims = !claims.Any() ? "" : claims.Select(c => c.ClaimId.ToString()).Distinct().Aggregate((a, b) => string.Format("{0},{1}", a, b)) }
            }, JsonRequestBehavior.AllowGet);

            //return File(new System.Text.UTF8Encoding().GetBytes(stringb.ToString()), "text/plain", "HCFA.txt");
        }

        public string GetDDDFileName(CompanyInfoDTO company)
        {
            var dddFileName = string.Empty;
            var nextDDDFileName = string.Empty;
            AccountData accountData = new AccountData();
            var dbNextDDDFileName = company.NextDDDFileName;
            var currentMonth = DateTime.Now.Month.ToString("00");
            var year = DateTime.UtcNow.Month > 6 ? DateTime.UtcNow.AddYears(1).ToString("yy") : DateTime.UtcNow.ToString("yy");
            string code = company.CompanyCode;

            if (string.IsNullOrEmpty(dbNextDDDFileName))
            {
                var number = "001";

                dddFileName = string.Concat(code, year, currentMonth, number);
                nextDDDFileName = string.Format("{0}|{1}|{2}", year, currentMonth, int.Parse(number));
            }
            else
            {
                string[] txtNextDDDFile = dbNextDDDFileName.Split('|');
                var previousMonth = txtNextDDDFile[1];
                int previousNumber = int.Parse(txtNextDDDFile[2]);
                var newNumber = string.Empty;

                if (previousMonth == DateTime.Now.Month.ToString("00"))
                {
                    previousNumber = previousNumber + 1;
                    newNumber = previousNumber.ToString("000");
                }
                else
                {
                    newNumber = "001";
                }

                dddFileName = string.Concat(code, year, currentMonth, newNumber);
                nextDDDFileName = string.Format("{0}|{1}|{2}", year, currentMonth, int.Parse(newNumber));

                //update nextDDDFileName in db
                if (!string.IsNullOrEmpty(dddFileName))
                {
                    UpdateNextDDDFileName(company.CompanyID, nextDDDFileName);
                }
            }
            return dddFileName;
        }

        public void UpdateNextDDDFileName(int companyId, string nextDDDFileName)
        {
            SqlConnection sqlConnection = null;
            try
            {
                using (sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_UpdateNextDDDFileName", sqlConnection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlCommand.Parameters.AddWithValue("@CompanyId", companyId);
                    sqlCommand.Parameters.AddWithValue("@NextDDDFileName", nextDDDFileName);
                    if (sqlConnection.State == ConnectionState.Closed)
                    {
                        sqlConnection.Open();
                    }
                    sqlCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                sqlConnection.Close();
            }
        }
    }
}