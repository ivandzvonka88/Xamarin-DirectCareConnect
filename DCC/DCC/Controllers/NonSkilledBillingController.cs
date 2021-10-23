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

namespace DCC.Controllers
{
    public class NonSkilledBillingController : DCCBaseController
    {
        SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString);
        [Authorize]
        public async Task<ActionResult> Index()
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


            r.claimStatusList = ds1.Tables[0].Rows.Cast<DataRow>().Select(spR => new ClaimsStatus()
            {
                claimstatusid = int.Parse(spR["ClaimStatusid"].ToString()),
                name = spR["Name"] == DBNull.Value ? "" : (string)spR["Name"],

            }).ToList();

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

        private Task<DataSet> getInsuranceList()
        {

            return Task.Run(() =>
            {
                List<int> companyids = new List<int>();
                var str = "";
                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString))
                {
                    //need to add the storeproc to get Insurance
                    //SqlCommand cmd = new SqlCommand("sp_InsuranceGetInsuranceList", cn)
                    SqlCommand cmd = new SqlCommand("sp_ClaimGetInsuranceList", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    da.Fill(ds);
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
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(ds);
                    }
                    else
                        ds = null;
                    return ds;


                }
            });
        }
        private Task<DataSet> getClaimStatusList()
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_ClaimStatusGetClaimStatusList", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    return ds;
                }
            });
        }

        private Task<DataSet> getClientListByInsurance(string payerID)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_InsuranceGetClientList", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@PayerID", payerID);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    return ds;
                }
            });
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> ClientList(Provider s, int status)
        {
            ProviderInit r = new ProviderInit();
            DataSet ds = null;
            DataTable dt = null;

            try
            {
                ds = await getClientListByInsurance(s.payerId);
                //if(status!=7)
                //ds.Tables[0].DefaultView.RowFilter = "ClaimStatusId <> '7' ";
                dt = (ds.Tables[0].DefaultView).ToTable();
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
                    r.clientInfoList = dt.Rows.Cast<DataRow>().Select(spR => new ClientInfo()
                    {
                        name = Convert.ToString(spR["Name"]),
                        dob = Convert.ToDateTime(spR["dob"] == DBNull.Value ? default(DateTime) : spR["dob"]),//Convert.ToDateTime(spR["dob"]),
                        clientId = Convert.ToInt32(spR["clsvID"] == DBNull.Value ? 0 : spR["clsvID"])

                    }).ToList();
                }
                catch (Exception ex1)
                {
                    r.er.msg = ex1.Message;
                }

            }
            if (ds != null)
                ds.Dispose();

            DataSet dsDccMain = new DataSet();
            try
            {
                //ds = await getClientDetailsByClientid(clientId, policyId);
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                    {
                        //need to add the storeproc to get Insurance
                        SqlCommand cmd = new SqlCommand("sp_InsuranceGetInsuranceList", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        SqlDataAdapter da = new SqlDataAdapter(cmd);

                        da.Fill(dsDccMain);
                    }
                });
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }

            r.companyInsuranceList = dsDccMain.Tables[0].Rows.Cast<DataRow>().Select(spR => new Option()
            {
                value = Convert.ToString(spR["InsuranceCompanyId"]),
                name = (string)spR["Name"]
            }).ToList();


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

        private Task<DataSet> getClientDetailsByClientid(string clientId, string policyId)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_BillingGetBillingDetails", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@ClientID", clientId);
                    cmd.Parameters.AddWithValue("@InsurancePolicyId", policyId);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    return ds;
                }
            });
        }

        private Task<DataSet> getClaimsPaymentByClientid(string clientId, string policyId)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_BillingGetClaimPayments", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    cmd.Parameters.AddWithValue("@PayerId", policyId);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    return ds;
                }
            });
        }
        private Task<DataSet> getClaimsPaymentAmounts(string clientId)//,long therapyId
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_BillingGetClaimPaymentAmount", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    //cmd.Parameters.AddWithValue("@ClientSessionTherapyID", therapyId);
                    //cmd.Parameters.AddWithValue("@InsurancePolicyId", policyId);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    return ds;
                }
            });
        }
        // Getting client details by Id //
        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> ClientDetails(string clientId, string selrowid, int selectedClientIndex, string policyId, int claimStatusId = -1)
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
            try
            {
                ds = await getClientDetailsByClientid(clientId, policyId);
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                    {
                        //need to add the storeproc to get Insurance
                        SqlCommand cmd = new SqlCommand("sp_InsuranceGetInsuranceList", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        SqlDataAdapter da = new SqlDataAdapter(cmd);

                        da.Fill(dsDccMain);
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
            if (r.er.code == 0)
            {
                DataRow dr = ds.Tables[0].Rows[0];
                DateTime dosOld = DateTime.MinValue;

                ds.Tables[0].DefaultView.RowFilter = claimStatusId == -1 ? "ClaimStatusID NOT IN (7,5)" : "ClaimStatusID = " + claimStatusId;
                DataTable dt = (ds.Tables[0].DefaultView).ToTable();
                int iOld = 0;

                for (int i = 0; i < dt.Rows.Count; i++)
                {

                    DataRow drDet = dt.Rows[i];
                    ClientDetails rc = new ClientDetails();
                    rc.dos = Convert.ToDateTime(drDet["DateOfService"] == DBNull.Value ? default(DateTime) : drDet["DateOfService"]);
                    ClientDetailInfo cdi = new ClientDetailInfo();
                    cdi.provider = Convert.ToString(drDet["Provider"]);
                    cdi.cptCode = Convert.ToString(drDet["CPTCode"]);
                    cdi.billedAmount = Math.Round(Convert.ToDecimal(drDet["AmtBilled"] == DBNull.Value ? 0 : drDet["AmtBilled"]), 2);
                    cdi.paidAmount = Math.Round(Convert.ToDecimal(drDet["PaidAmt"] == DBNull.Value ? 0 : drDet["PaidAmt"]), 2);
                    cdi.policyNumber = Convert.ToString(drDet["PolicyNbr"]);
                    cdi.groupNumber = Convert.ToString(drDet["GroupNbr"]);
                    cdi.dddStartDate = (Convert.ToDateTime(drDet["DDDStart"] == DBNull.Value ? default(DateTime) : drDet["DDDStart"])).ToString("MM/dd/yy");
                    cdi.dddEndDate = (Convert.ToDateTime(drDet["DDDEnd"] == DBNull.Value ? default(DateTime) : drDet["DDDEnd"])).ToString("MM/dd/yy");
                    cdi.dddUnit = Convert.ToString(drDet["DDDUnits"]);
                    cdi.claimId = Convert.ToInt64(drDet["ClaimID"] == DBNull.Value ? 0 : drDet["ClaimID"]);
                    cdi.comments = Convert.ToString(drDet["Comments"] == DBNull.Value ? "" : drDet["Comments"]);
                    cdi.InsurancePolicyId = Convert.ToInt32(drDet["InsurancePolicyId"] == DBNull.Value ? 0 : drDet["InsurancePolicyId"]);
                    cdi.ClaimStatusID = Convert.ToInt32(drDet["ClaimStatusID"] == DBNull.Value ? 0 : drDet["ClaimStatusID"]);
                    cdi.Client = Convert.ToString(drDet["Client"]);
                    cdi.paymentId = Convert.ToInt32(drDet["PaymentId"] == DBNull.Value ? 0 : drDet["PaymentId"]);
                    cdi.PresStart = (Convert.ToDateTime(drDet["PresStart"] == DBNull.Value ? default(DateTime) : drDet["PresStart"])).ToString("MM/dd/yy");
                    cdi.PresEnd = (Convert.ToDateTime(drDet["PresEnd"] == DBNull.Value ? default(DateTime) : drDet["PresEnd"])).ToString("MM/dd/yy");
                    cdi.PAuthStart = (Convert.ToDateTime(drDet["PAuthStart"] == DBNull.Value ? default(DateTime) : drDet["PAuthStart"])).ToString("MM/dd/yy");
                    cdi.PAuthEnd = (Convert.ToDateTime(drDet["PAuthEnd"] == DBNull.Value ? default(DateTime) : drDet["PAuthEnd"])).ToString("MM/dd/yy");
                    cdi.serviceId = Convert.ToInt32(drDet["serviceId"] == DBNull.Value ? 0 : drDet["serviceId"]);
                    cdi.staffId = Convert.ToInt32(drDet["prID"] == DBNull.Value ? 0 : drDet["prID"]);
                    cdi.clientSessionTherapyID = Convert.ToInt32(drDet["ClientSessionTherapyID"] == DBNull.Value ? 0 : drDet["ClientSessionTherapyID"]);
                    cdi.fileId = Convert.ToInt32(drDet["clTherapyNoteId"] == DBNull.Value ? 0 : drDet["clTherapyNoteId"]);
                    cdi.fileExtension = Convert.ToString(drDet["fileExtension"] == DBNull.Value ? 0 : drDet["fileExtension"]);
                    cdi.insurancePriority = Convert.ToInt32(drDet["InsurancePriorityId"] == DBNull.Value ? 0 : drDet["InsurancePriorityId"]);
                    cdi.therapistSupervisor = Convert.ToInt32(drDet["TherapistSupervisor"] == DBNull.Value ? 0 : drDet["TherapistSupervisor"]);
                    if (!downLoadCategories.Keys.Contains(cdi.fileId))
                    {
                        List<SelectListItem> downLoadCategory = new List<SelectListItem>();
                        if (cdi.fileId != 0) downLoadCategory.Add(new SelectListItem { Selected = false, Text = "Download HCFA", Value = cdi.fileId.ToString() });
                        downLoadCategory.Add(new SelectListItem { Selected = false, Text = "Evaluation Report", Value = "Evaluation Report" });
                        downLoadCategory.Add(new SelectListItem { Selected = false, Text = "2019 Q3 Quaterly Report", Value = "2019 Q3 Quaterly Report" });
                        downLoadCategory.Add(new SelectListItem { Selected = false, Text = "Session Document", Value = "Session Document" });
                        downLoadCategory.Add(new SelectListItem { Selected = false, Text = "Download All", Value = "99" });

                        downLoadCategories.Add(cdi.fileId, downLoadCategory);
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
                    cdi.ReceivedAt = (Convert.ToDateTime(drPayment["ReceivedAt"] == DBNull.Value ? default(DateTime) : drPayment["ReceivedAt"])).ToString("MM/dd/yy");
                    cdi.PaymentTypeId = Convert.ToInt32(drPayment["PaymentTypeId"] == DBNull.Value ? 0 : drPayment["PaymentTypeId"]);
                    cdi.VoidedAt = (Convert.ToDateTime(drPayment["VoidedAt"] == DBNull.Value ? default(DateTime) : drPayment["VoidedAt"]));//.ToString("MM/dd/yy");
                    cdi.DenialReasonId = Convert.ToInt32(drPayment["DenialReasonId"] == DBNull.Value ? 0 : drPayment["DenialReasonId"]);
                    cdi.ReasonText = Convert.ToString(drPayment["ReasonText"]);
                    cdi.StaffId = Convert.ToInt32(drPayment["StaffId"] == DBNull.Value ? 0 : drPayment["StaffId"]);
                    cdi.OABatchID = Convert.ToInt64(drPayment["OABatchID"] == DBNull.Value ? 0 : drPayment["OABatchID"]);
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

                DataSet ds1 = await getClaimStatusList();

                r.claimStatusList = ds1.Tables[0].Rows.Cast<DataRow>().Select(spR => new ClaimsStatus()
                {
                    claimstatusid = int.Parse(spR["ClaimStatusid"].ToString()),
                    name = spR["Name"] == DBNull.Value ? "" : (string)spR["Name"],

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
                    policyId = Convert.ToInt32(drPa["InsurancePolicyId"] == DBNull.Value ? 0 : drPa["InsurancePolicyId"])
                }).ToList();
                // dummy data
                //r.OtherClaimPayments = new List<PaymentAmount>();
                //r.OtherClaimPayments.Add(new PaymentAmount() { allowedAmount = 100, billedamount = 250, claimId = 1, coInsuranceAmount = 15, insurancePriority = 1, paidAmount = 50, policyId = 31 });
                //r.OtherClaimPayments.Add(new PaymentAmount() { allowedAmount = 150, billedamount = 50, claimId = 1, coInsuranceAmount = 15, insurancePriority = 3, paidAmount = 35, policyId = 31 });
            }

            var genderType = from GenderTypeEnum e in Enum.GetValues(typeof(GenderTypeEnum))
                             select new
                             {
                                 ID = (int)e,
                                 Name = e.ToString()
                             };

            ViewBag.EnumList = new SelectList(genderType, "ID", "Name");
            ViewBag.downloadCategory = downLoadCategories;
            if (ds != null)
                ds.Dispose();
            return PartialView("ClaimDetails", r);
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
        public string UpdateClaim(int ClientID, long ClaimId, int ServiceId, decimal? AmountBilled = null, decimal? Amount = null, DateTime? DDDstdt = null, DateTime? DDDeddt = null,
            decimal? DDDau = null, string Comments = null, int ClaimStatusId = 0)
        {
            ProviderInit r = new ProviderInit();

            try
            {
                updateClaims(ClientID, ClaimId, ServiceId, AmountBilled, Amount, DDDstdt, DDDeddt,
              DDDau, Comments, ClaimStatusId);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            // return Json(r);
            //return PartialView("ClientDetails", r);
            return "";
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

        private string updateClaims(int ClientID, long ClaimId, int ServiceId, decimal? AmountBilled = null, decimal? Amount = null, DateTime? DDDstdt = null, DateTime? DDDeddt = null,
            decimal? DDDau = null, string Comments = null, int claimStatusId = 0)
        {

            // return Task.Run(() =>
            //{
            // using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString))
            // {
            //need to add the storeproc to get Insurance
            SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString);
            cn.Open();
            // SqlCommand cmd = new SqlCommand(sql, conn);
            //cmd.ExecuteNonQuery();
            SqlCommand cmd = new SqlCommand("sp_BillingUpdateBillingDetails", cn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ClientID", ClientID);
            cmd.Parameters.AddWithValue("@ClaimId", ClaimId);
            cmd.Parameters.AddWithValue("@ServiceId", ServiceId);
            cmd.Parameters.AddWithValue("@AmountBilled", AmountBilled);
            cmd.Parameters.AddWithValue("@Amount", Amount);
            cmd.Parameters.AddWithValue("@DDDstdt", DDDstdt);
            cmd.Parameters.AddWithValue("@DDDeddt", DDDeddt);
            cmd.Parameters.AddWithValue("@DDDau", DDDau);
            cmd.Parameters.AddWithValue("@Comments", Comments);
            cmd.Parameters.AddWithValue("@ClaimStatusId", claimStatusId);
            cmd.ExecuteNonQuery();
            // return Json(r);
            return "";
            //}
            //  });
        }
        private string updateClaimStatus(long ClaimId, int claimStatusId)
        {
            SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString);
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
                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_BillingGetInsuranceDetails", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@InsurancePolicyId", InsurancePolicyId);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    da.Fill(ds);
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
                    id = (string)spR["id"],
                    name = (string)spR["name"]
                }).ToList();
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
                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_DenialReasonGetList", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    return ds;
                }
            });
        }

        // Get Denial Reasons List //
        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public ActionResult UpdateDeductible(long ClaimId, long PaymentId, decimal Amount, string ReasonCode)
        {
            ProviderInit r = new ProviderInit();
            r.DenialReasonList = new List<DenialReason>();
            string msg = "";

            try
            {
                msg = updateDeductible(ClaimId, PaymentId, Amount, ReasonCode);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            return Json(new { message = msg });

        }
        private string updateDeductible(long claimId, long paymentId, decimal amount, string reasonCode)
        {

            string msg = "Success";
            try
            {
                SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString);
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_BillingUpdateDeductible", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ClaimId", claimId);
                cmd.Parameters.AddWithValue("@PaymentId", paymentId);
                cmd.Parameters.AddWithValue("@Amount", amount);
                cmd.Parameters.AddWithValue("@DenialReason", reasonCode);
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
        public ActionResult UpdateCleanDenial(long ClaimId, long PaymentId, decimal Amount, string ReasonCode)
        {
            ProviderInit r = new ProviderInit();
            r.DenialReasonList = new List<DenialReason>();
            string msg = "";

            try
            {
                msg = updateCleanDenial(ClaimId, PaymentId, Amount, ReasonCode);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            return Json(new { message = msg });

        }
        private string updateCleanDenial(long claimId, long paymentId, decimal amount, string reasonCode)
        {

            string msg = "Success";
            try
            {
                SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString);
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_BillingUpdateDenialPayment", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ClaimId", claimId);
                cmd.Parameters.AddWithValue("@PaymentId", paymentId);
                cmd.Parameters.AddWithValue("@Amount", amount);
                cmd.Parameters.AddWithValue("@DenialReason", reasonCode);
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
        public ActionResult UpdatePriorAuth()
        {
            ProviderInit r = new ProviderInit();
            string msg = "";

            try
            {
                msg = updatePriorAuth();
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            return Json(new { message = msg });

        }
        private string updatePriorAuth()
        {

            string msg = "Success";
            try
            {
                SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString);
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_BillingInsertAuthAlert", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@prId", UserClaim.prid);
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
                SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString);
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
                SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString);
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
            Helpers.EmailHelper em = new Helpers.EmailHelper();
            try
            {
                msg = em.communication("", "", message, phone, false, null);
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
        public ActionResult VoidPayments(string comment, int claimid, int paymentid, int staffid)
        {
            ProviderInit r = new ProviderInit();
            string msg = "";

            try
            {
                msg = updateVoidPayments(comment, claimid, paymentid, staffid);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;

            }
            return Json(new { message = msg });

        }
        private string updateVoidPayments(string comment, int claimId, int paymentId, int staffId)
        {

            string msg = "Success";
            try
            {
                SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["THPL"].ConnectionString);
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ClaimPaymentUpdate", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@StaffId", staffId);
                cmd.Parameters.AddWithValue("@PaymentId", paymentId);
                cmd.Parameters.AddWithValue("@ClaimId", claimId);
                cmd.Parameters.AddWithValue("@Reason", comment);
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
                payerId = spR["PayerId"] == DBNull.Value ? "" : (string)spR["PayerId"],
                name = spR["Name"] == DBNull.Value ? "" : (string)spR["Name"],
                //line1 = (string)spR["Line1"],
                //line2 = (string)spR["Line2"],
                //city = (string)spR["City"],
                //state = (string)spR["State"],
                //postalCode = (string)spR["PostalCode"]
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

            Helpers.EmailHelper em = new Helpers.EmailHelper();
            try
            {
                HttpPostedFileBase file = null;
                if (Request.Files != null && Request.Files.Count > 0)
                {
                    file = Request.Files[0];
                }
                msg = em.communication(Request.Form["email"], Request.Form["subject"], Request.Form["message"], "", true, file);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                //msg = ex.Message;
                r.er.msg = ex.Message;
            }
            return View("Communication", r);
        }
    }
}