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
using DCC.Models;
using DCC.Models.Home;
using DCC.Models.SessionNotes;
using DCC.Geo;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using PdfSharp.Pdf;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.AcroForms;
using PdfSharp;
using PdfSharp.Drawing;
using TheArtOfDev.HtmlRenderer.PdfSharp;
using DCC.SQLHelpers.Helpers;
using System.Device.Location;
using DCC.Helpers;

namespace DCC.Controllers
{
    [OutputCache(NoStore = true, Duration = 0)]
    public class HomeController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;
        public HomeController()
        {
            sqlHelper = new SQLHelper();
        }

        [AJAXAuthorize]
        public ActionResult Index()
        {
            PageInitializer r = new PageInitializer();
            setViewModelBase((ViewModelBase)r);
            return View(r);
        }




        [AJAXAuthorize]
        public async Task<ActionResult> GetTaskStaffPage(string prId)
        {


            if (prId == null)
            {
                prId = UserClaim.prid.ToString();

            }
            else
            {
                var s = prId.Split('-');
                if (s[1] != UserClaim.coid.ToString())
                {
                    Response.Write("You are currently logged into " + UserClaim.companyName);
                    Response.StatusCode = 403;
                    return null;
                }
                else
                    prId = s[0];
            }

            HomeStaffPage r = new HomeStaffPage();
            r.userLevel = UserClaim.userLevel;
           
          

            DataSet ds = new DataSet();



            try
            { /* Doesn't work
            var clientAlerts = getClientAlerts();
            if (UserClaim.userLevel == "BillingAdmin")
            {
                clientAlerts.AddRange(GetPreAuthAlerts());
            }
            r.clientAlerts = clientAlerts;
           */
                r.clientAlerts = getClientAlerts();
                r.staffAlerts = getStaffAlerts();

                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskGetStaffPage", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@prId", prId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                setStaffPage(ref r, ref ds);
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
                Response.StatusCode = 500;
                return null;
            }
            else
            {
                if (r.userLevel != "Provider" && r.userLevel != "TherapyAssistant")
                {
                    if (r.staffLevel == "Provider" || r.staffLevel == "TherapyAssistant")
                        return PartialView("Task_Director_Provider", r);
                    else if (r.staffLevel == "Supervisor" || r.staffLevel == "TherapySupervisor")
                        return PartialView("Task_Supervisor_home", r);
                    else
                        return PartialView("Task_Director_Home", r);
                }
                else
                    return PartialView("Task_Provider_Home", r);
            }
        }

        private void setStaffPage(ref HomeStaffPage r, ref DataSet ds)
        {
            DataRow drx = ds.Tables[0].Rows[0];
            r.fn = (string)drx["fn"];
            r.ln = (string)drx["ln"];
            r.userPrId = UserClaim.prid;
            r.prId = (int)drx["prId"];
            r.staffLevel = (string)drx["staffLevel"];


            /* Figure out which screen to use depends on userLevel (role) */
            if (r.userLevel != "Provider" && r.userLevel != "TherapyAssistant")
            {
                if (r.staffLevel == "Provider" || r.staffLevel == "TherapyAssistant")
                    r.tgtView = "boxView";
                else if (r.staffLevel == "Supervisor" || r.staffLevel == "TherapySupervisor")
                    r.tgtView = "boxView";
                else
                    r.tgtView = "boxView";
            }
            else
                r.tgtView = "boxView";


            /* comments */


            DataView comments = new DataView(ds.Tables[1]);

            r.commentHistory = comments.ToTable().Rows.Cast<DataRow>().Select(spR => new CommentHistory()
            {
                commentId = (int)spR["commentId"],
                subject = (string)spR["subject"],
                commentator = (string)spR["commentator"],
                comment = (string)spR["comment"],
                cmtType = (string)spR["cmtType"],
                cmtDt = ((DateTime)spR["cmtDt"]).ToShortDateString()

            }).ToList();


            r.newAuthorizations = ds.Tables[2].Rows.Cast<DataRow>().Select(spR => new NewAuthorization()
            {
                // new Authorizations only
                clsvId = (int)spR["clsvId"],
                nm = (string)spR["nm"],
                svc = (string)spR["svc"],
                stDate = ((DateTime)spR["stdt"]).ToShortDateString(),
                edDate = ((DateTime)spR["eddt"]).ToShortDateString(),
                units = (decimal)spR["au"]
            }).ToList();

            /* Staff Lists */
            DataView StaffList = new DataView(ds.Tables[3]);
            StaffList.RowFilter = "supervisoryLevel<>1";
            r.supervisors = StaffList.ToTable().Rows.Cast<DataRow>().Select(spR => new Staff()
            {

                prId = spR["prId"].ToString() + "-" + UserClaim.coid,
                sNm = (string)spR["sNm"],
                userLevel = (string)spR["roleNominal"],
                sendBirdUserId = UserClaim.coid + "-" + spR["prId"]
            }).ToList();
            StaffList.RowFilter = "supervisoryLevel=1";
            r.providers = StaffList.ToTable().Rows.Cast<DataRow>().Select(spR => new Staff()
            {
                prId = spR["prId"].ToString() + "-" + UserClaim.coid,
                sNm = (string)spR["sNm"],
                userLevel = (string)spR["roleNominal"],
                sendBirdUserId = UserClaim.coid + "-" + spR["prId"]
            }).ToList();
            StaffList.Dispose();

            r.clients = ds.Tables[4].Rows.Cast<DataRow>().Select(spR => new Client()
            {

                clsvId = spR["clsvId"] + "-" + UserClaim.coid,
                cNm = (string)spR["cNm"]
            }).ToList();

            r.credentials = setStaffCredentials(ref ds, r.prId, 5);
            r.requiredCredentials = setStaffRequiredCredentials(ref ds, 5);
            r.pendingDocumentation = setPendingDocumentation(ref ds, 6);
            r.providerHours.Periods = setPeriods(ref ds, 7);
            r.providerHours.PeriodId = Convert.ToInt32(ds.Tables[8].Rows[0].ItemArray[0]);
            SetProviderBillingMatrix(ref ds, 9, ref r.providerHours);
            r.providerHours.Visits = SetProviderSessionData(ref ds, 9);
           
                r.scheduleChangeRequests = SetChangeRequests(ref ds, 10);
        }
        private List<Period> setPeriods(ref DataSet ds, int tableIdx)
        {
            List<Period> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new Period()
            {
                startDate = Convert.ToDateTime(spR["s"]).ToShortDateString(),
                endDate = Convert.ToDateTime(spR["e"]).ToShortDateString(),
                periodId = (int)spR["ppId"]
            }).ToList();
            return r;
        }
        private void SetClientBillingMatrix(ref DataSet ds, int tableIdx, ref ClientHours r)
        {
            DataView ClientHoursData = new DataView(ds.Tables[tableIdx]);

            // Filter out those items Ryan says are non billable
            ClientHoursData.RowFilter = "IsEvv=0 OR " +
                "(completed<>0 AND noShow=0 AND (designeeId<>0 OR guardianId<>0) )";


            DataTable ServiceItemsDistinct = ClientHoursData.ToTable(true, "svc");
            DataTable ServiceItemsWithUnitsRatio = ClientHoursData.ToTable(true, "svc", "utcIn", "utcOut", "units", "ratio");
            // need to be careful here because ProviderHours Data has 1:2 1:3
            foreach (DataRow dr in ServiceItemsDistinct.Rows)
            {
                BillableMatrixItem billingMatrixItem = new BillableMatrixItem();
                billingMatrixItem.svc = (string)dr["svc"];
                object Units1_1 = ServiceItemsWithUnitsRatio.Compute("SUM(Units)", "svc='" + dr["svc"] + "' AND ratio=1");
                object Units1_2 = ServiceItemsWithUnitsRatio.Compute("SUM(Units)", "svc='" + dr["svc"] + "' AND ratio=2");
                object Units1_3 = ServiceItemsWithUnitsRatio.Compute("SUM(Units)", "svc='" + dr["svc"] + "' AND ratio=3");
                object Units1_4 = ServiceItemsWithUnitsRatio.Compute("SUM(Units)", "svc='" + dr["svc"] + "' AND ratio=4");
                object Units1_5 = ServiceItemsWithUnitsRatio.Compute("SUM(Units)", "svc='" + dr["svc"] + "' AND ratio=5");
                object Units1_6 = ServiceItemsWithUnitsRatio.Compute("SUM(Units)", "svc='" + dr["svc"] + "' AND ratio=6");
              
                if (Units1_1 != DBNull.Value)
                    r.has_1_1 = true;
                if (Units1_2 != DBNull.Value)
                    r.has_1_2 = true;
                if (Units1_3 != DBNull.Value)
                    r.has_1_3 = true;
                if (Units1_4 != DBNull.Value)
                    r.has_1_4 = true;
                if (Units1_5 != DBNull.Value)
                    r.has_1_5 = true;
                if (Units1_6 != DBNull.Value)
                    r.has_1_6 = true;


                billingMatrixItem.Units_1_1 = Units1_1 != DBNull.Value ? Convert.ToDecimal(Units1_1) : 0M;
                billingMatrixItem.Units_1_2 = Units1_2 != DBNull.Value ? Convert.ToDecimal(Units1_2) : 0M;
                billingMatrixItem.Units_1_3 = Units1_3 != DBNull.Value ? Convert.ToDecimal(Units1_3) : 0M;
                billingMatrixItem.Units_1_4 = Units1_4 != DBNull.Value ? Convert.ToDecimal(Units1_4) : 0M;
                billingMatrixItem.Units_1_5 = Units1_5 != DBNull.Value ? Convert.ToDecimal(Units1_5) : 0M;
                billingMatrixItem.Units_1_6 = Units1_6 != DBNull.Value ? Convert.ToDecimal(Units1_6) : 0M;

                billingMatrixItem.Units_Total = billingMatrixItem.Units_1_1 + billingMatrixItem.Units_1_2 + billingMatrixItem.Units_1_3 +
                             billingMatrixItem.Units_1_4 + billingMatrixItem.Units_1_5 + billingMatrixItem.Units_1_6;
                r.matrixItems.Add(billingMatrixItem);
                r.Units_1_1_Total += billingMatrixItem.Units_1_1;
                r.Units_1_2_Total += billingMatrixItem.Units_1_2;
                r.Units_1_3_Total += billingMatrixItem.Units_1_3;
                r.Units_1_4_Total += billingMatrixItem.Units_1_4;
                r.Units_1_5_Total += billingMatrixItem.Units_1_5;
                r.Units_1_6_Total += billingMatrixItem.Units_1_6;

            }
            r.Units_Total_All = r.Units_1_1_Total + r.Units_1_2_Total + r.Units_1_3_Total + r.Units_1_4_Total + r.Units_1_5_Total + r.Units_1_6_Total;
            ServiceItemsDistinct.Dispose();
            ServiceItemsWithUnitsRatio.Dispose();
        }


        private void SetProviderBillingMatrix(ref DataSet ds, int tableIdx, ref ProviderHours r)
        {
            DataView ProviderHoursData = new DataView(ds.Tables[tableIdx]);

            // Filter out those items Ryan says are non billable
            ProviderHoursData.RowFilter = "IsEvv=0 OR " +
                  "(completed<>0 AND noShow=0 AND designeeUnableToSign=0 AND designeeRefusedToSign=0 AND clientRefusedService=0 AND unsafeToWork=0 AND (designeeId<>0 OR guardianId<>0) )";


            DataTable ServiceItemsDistinct = ProviderHoursData.ToTable(true, "svc");
            DataTable ServiceItemsWithUnitsRatio = ProviderHoursData.ToTable(true, "svc", "utcIn", "utcOut", "units", "ratio");
            // need to be careful here because ProviderHours Data has 1:2 1:3
            foreach (DataRow dr in ServiceItemsDistinct.Rows)
            {
                BillableMatrixItem billingMatrixItem = new BillableMatrixItem();
                billingMatrixItem.svc = (string)dr["svc"];
                object Units1_1 = ServiceItemsWithUnitsRatio.Compute("SUM(Units)", "svc='" + dr["svc"] + "' AND ratio=1");
                object Units1_2 = ServiceItemsWithUnitsRatio.Compute("SUM(Units)", "svc='" + dr["svc"] + "' AND ratio=2");
                object Units1_3 = ServiceItemsWithUnitsRatio.Compute("SUM(Units)", "svc='" + dr["svc"] + "' AND ratio=3");
                object Units1_4 = ServiceItemsWithUnitsRatio.Compute("SUM(Units)", "svc='" + dr["svc"] + "' AND ratio=4");
                object Units1_5 = ServiceItemsWithUnitsRatio.Compute("SUM(Units)", "svc='" + dr["svc"] + "' AND ratio=5");
                object Units1_6 = ServiceItemsWithUnitsRatio.Compute("SUM(Units)", "svc='" + dr["svc"] + "' AND ratio=6");


                if (Units1_1 != DBNull.Value)
                    r.has_1_1 = true;
                if (Units1_2 != DBNull.Value)
                    r.has_1_2 = true;
                if (Units1_3 != DBNull.Value)
                    r.has_1_3 = true;
                if (Units1_4 != DBNull.Value)
                    r.has_1_4 = true;
                if (Units1_5 != DBNull.Value)
                    r.has_1_5 = true;
                if (Units1_6 != DBNull.Value)
                    r.has_1_6 = true;

                billingMatrixItem.Units_1_1 = Units1_1 != DBNull.Value ? Convert.ToDecimal(Units1_1) : 0M;
                billingMatrixItem.Units_1_2 = Units1_2 != DBNull.Value ? Convert.ToDecimal(Units1_2) : 0M;
                billingMatrixItem.Units_1_3 = Units1_3 != DBNull.Value ? Convert.ToDecimal(Units1_3) : 0M;
                billingMatrixItem.Units_1_4 = Units1_4 != DBNull.Value ? Convert.ToDecimal(Units1_4) : 0M;
                billingMatrixItem.Units_1_5 = Units1_5 != DBNull.Value ? Convert.ToDecimal(Units1_5) : 0M;
                billingMatrixItem.Units_1_6 = Units1_6 != DBNull.Value ? Convert.ToDecimal(Units1_6) : 0M;
                billingMatrixItem.Units_Total = billingMatrixItem.Units_1_1 + billingMatrixItem.Units_1_2 + billingMatrixItem.Units_1_3 + billingMatrixItem.Units_1_4 + billingMatrixItem.Units_1_5 + billingMatrixItem.Units_1_6;
                r.matrixItems.Add(billingMatrixItem);
                r.Units_1_1_Total += billingMatrixItem.Units_1_1;
                r.Units_1_2_Total += billingMatrixItem.Units_1_2;
                r.Units_1_3_Total += billingMatrixItem.Units_1_3;
                r.Units_1_4_Total += billingMatrixItem.Units_1_4;
                r.Units_1_5_Total += billingMatrixItem.Units_1_5;
                r.Units_1_6_Total += billingMatrixItem.Units_1_6;

            }
            r.Units_Total_All = r.Units_1_1_Total + r.Units_1_2_Total + r.Units_1_3_Total + r.Units_1_4_Total + r.Units_1_5_Total + r.Units_1_6_Total;
            ServiceItemsDistinct.Dispose();
            ServiceItemsWithUnitsRatio.Dispose();
        }
        private List<Visit> SetProviderSessionData(ref DataSet ds, int tableIdx)
        {
            List<Visit> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new Visit()
            {
                SessionType = spR["sessionType"].ToString(),
                StaffSessionId = Convert.ToInt32(spR["staffSessionId"]),
                ClientSessionId = Convert.ToInt32(spR["clientSessionId"]),
                Service = (string)spR["svc"],

                Date = ((DateTime)spR["dt"]).ToShortDateString(),
                StartAt = (DateTimeLocal((DateTime)spR["utcIn"])).ToShortTimeString(),
                EndAt = spR["utcOut"] != DBNull.Value ? (DateTimeLocal((DateTime)spR["utcOut"])).ToShortTimeString() : "",
                Units = spR["utcOut"] != DBNull.Value ? @String.Format("{0:F2}", Convert.ToDecimal(spR["units"])) : "",
                Ratio = spR["ratio"] != DBNull.Value ? "1:" + Convert.ToInt32(spR["ratio"]) : "",
                IsEVV = (bool)spR["isEVV"],
                ClientName = (string)spR["ln"] + " " + (string)spR["fn"],
                StartLocationType =
                          (spR["StartBillingLocationType"] != DBNull.Value ? (string)spR["StartBillingLocationType"] : ""),
                StartLocationAddress =
                          (spR["StartClientAddress"] != DBNull.Value ? " " + (string)spR["StartClientAddress"] : "") +
                          (spR["StartClientCity"] != DBNull.Value ? " " + (string)spR["StartClientCity"] : ""),

                StartLat = (decimal)spR["startLat"],
                StartLon = (decimal)spR["startLon"],

                EndLocationType =
                          (spR["EndBillingLocationType"] != DBNull.Value ? (string)spR["EndBillingLocationType"] : ""),
                EndLocationAddress =
                          (spR["EndClientAddress"] != DBNull.Value ? " " + (string)spR["EndClientAddress"] : "") +
                          (spR["EndClientCity"] != DBNull.Value ? " " + (string)spR["EndClientCity"] : ""),

                EndLat = spR["endLat"] != DBNull.Value ? (decimal)spR["endLat"] : 0M,
                EndLon = spR["endLon"] != DBNull.Value ? (decimal)spR["endLon"] : 0M,


                ClientLocationType =
                       (spR["ClientLocationType"] != DBNull.Value ? (string)spR["ClientLocationType"] : ""),


                Status = spR["utcOut"] == DBNull.Value ? "No Sign Out/End Time" :
                      ((int)spR["guardianId"] == 0 && (int)spR["designeeId"] == 0 ? "Designee Approval Missing " : "") +
                      ((bool)spR["noShow"] ? "Client No Show " : "") +
                      ((bool)spR["IsEvv"] && (bool)spR["designeeUnableToSign"] ? "Designee Unable To Sign " : "") +
                      ((bool)spR["IsEvv"] && (bool)spR["designeerefusedToSign"] ? "Designee Refused To Sign " : "") +
                      ((bool)spR["IsEvv"] && (bool)spR["clientRefusedService"] ? "Client Refused To Sign " : "") +
                      ((bool)spR["IsEvv"] && (bool)spR["unsafeToWork"] ? "Unsafe To Work " : "") +
                      (!(bool)spR["completed"] ? "Session Note Incomplete " : "") +
                      (spR["sessionType"].ToString() == "Therapy" && (spR["clientLocationId"] == DBNull.Value || (int)spR["clientLocationId"] == 0) ? "Locations Requires Verification" : ""),

                NotPayable = spR["utcOut"] == DBNull.Value ? true :

                      ((bool)spR["IsEvv"] && (int)spR["guardianId"] == 0 && (int)spR["designeeId"] == 0) ||
                      ((bool)spR["noShow"]) ||
                      ((bool)spR["IsEvv"] && (bool)spR["designeeUnableToSign"]) ||
                      ((bool)spR["IsEvv"] && (bool)spR["designeerefusedToSign"]) ||
                      ((bool)spR["IsEvv"] && (bool)spR["clientRefusedService"]) ||
                      ((bool)spR["IsEvv"] && (bool)spR["unsafeToWork"]) ||
                      (!(bool)spR["completed"]) ||
                      (spR["sessionType"].ToString() == "Therapy" && (spR["clientLocationId"] == DBNull.Value || (int)spR["clientLocationId"] == 0))


            }).ToList();

            return r;
        }


        private List<ScheduleChangeRequest> SetChangeRequests(ref DataSet ds, int tableIdx)
        {
            List<ScheduleChangeRequest> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new ScheduleChangeRequest()
            {
                scheduleId= (int)spR["scheduleId"],
                requestor = spR["requestorLastName"] + " " + spR["requestorFirstName"],
                requestedTime = DateTimeLocal((DateTime)spR["createdAt"]).ToShortDateString() + " " + DateTimeLocal((DateTime)spR["createdAt"]).ToShortDateString(),
                provider = spR["staffFirstName"] + " " + spR["staffLastName"],
                client = spR["clientLastName"] + " " + spR["clientFirstName"],
                svc = spR["svc"].ToString(),
                changeRequest = spR["changeRequest"].ToString()


            }).ToList();

            return r;
        }



        private List<Credential> setStaffCredentials(ref DataSet ds, int prId, int tableIdx)
        {
            List<Credential> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new Credential()
            {
                credTypeId = (int)spR["credTypeId"],
                credName = (string)spR["credName"],
                credId = spR["credId"] != DBNull.Value ? (int)spR["credId"] : 0,
                docId = spR["docId"] != DBNull.Value ? (string)spR["docId"] : "",
                validFrom = spR["validFrom"] != DBNull.Value ? ((DateTime)spR["validFrom"]).ToShortDateString() : "",
                validTo = spR["validTo"] != DBNull.Value ? ((DateTime)spR["validTo"]).ToShortDateString() : "",
                verificationDate = spR["verified"] != DBNull.Value && (bool)spR["verified"] ? ((DateTime)spR["verificationDate"]).ToShortDateString() : "",
                verifiedBy = spR["verified"] != DBNull.Value && (bool)spR["verified"] ? spR["fn"] + " " + spR["ln"] : "",
                status = (string)spR["status"],
                statusColor = ((string)spR["status"] != "Superseded" && (string)spR["status"] != "Verified") ? "red" : "green",
                btnView = (string)spR["status"] == "Missing" ? false : true,
                //btnVerify = ((string)spR["status"] == "Not Verified" && prId != UserClaim.prid) ? true : false,
                btnVerify = (spR["verified"] != DBNull.Value && !(bool)spR["verified"] && prId != UserClaim.prid) ? true : false,

                btnMail = ((string)spR["status"] != "Superseded" && (string)spR["status"] != "Verified" && prId != UserClaim.prid) ? true : false,
                //  btnEdit = (spR["verified"] != DBNull.Value && (bool)spR["verified"])  ? false : true
                btnEdit = (spR["verified"] == DBNull.Value || !(bool)spR["verified"] || UserClaim.userLevel == "SuperAdmin") ? true : false
            }).ToList();
            return r;
        }

        private List<PendingDocumentation> setPendingDocumentation(ref DataSet ds, int tableIdx)
        {
            List<PendingDocumentation> r = null;

            r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new PendingDocumentation()
            {

                docId = (int)spR["docId"],
                docType = (string)spR["docType"],
                clientName = (string)spR["cfn"] + " " + (string)spR["cln"],
                providerName = (string)spR["sfn"] + " " + (string)spR["sln"],
                providerName2 = (string)spR["s2fn"] !=  "" ? (string)spR["s2fn"] + ' ' + (string)spR["s2ln"] : null,
                completed = (bool)spR["completed"],
                approved = Convert.ToBoolean(spR["verified"]),
                dueDt = ((DateTime)spR["dueDt"]).ToShortDateString(),
                status = (string)spR["status"],
                noteType = (string)spR["noteType"],
                svc = (string)spR["svc"],
                lostSession = Convert.ToBoolean(spR["lostSession"]),
                requiresLocation = (bool)spR["requiresLocation"],
                clientLocationId = (int)spR["clientLocationId"]
            }).ToList();

            return r;
        }

        private List<RequiredCredential> setStaffRequiredCredentials(ref DataSet ds, int tableIdx)
        {
            DataView dv = new DataView(ds.Tables[tableIdx]);
            List<RequiredCredential> r = dv.ToTable(true, "credTypeId", "credName").Rows.Cast<DataRow>().Select(spR => new RequiredCredential()
            {
                credTypeId = (int)spR["credTypeId"],
                credName = (string)spR["credName"]
            }).ToList();

            dv.Dispose();
            return r;
        }

        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> GetTaskClientPage(string clsvId)
        {
            var s = clsvId.Split('-');
            if (s[1] != UserClaim.coid.ToString())
            {
                Response.Write("You are currently logged into " + UserClaim.companyName);
                Response.StatusCode = 403;
                return null;
            }
            else
                clsvId = s[0];

            ClientPageData r = new ClientPageData();
            r.userLevel = UserClaim.userLevel;
            r.userPrid = UserClaim.prid;
            r.documentationStart = DateTimeLocal().AddDays(-14).ToString("yyyy-MM-dd");
            r.documentationEnd = DateTimeLocal().ToString("yyyy-MM-dd");
            r.documentationSelNotes = true;
            r.documentationSelReports = true;



            /*
            var clientAlerts = getClientAlerts();
            if (UserClaim.userLevel == "BillingAdmin")
            {
                clientAlerts.AddRange(GetPreAuthAlerts());
            }
            r.clientAlerts = clientAlerts;
            */

            r.clientAlerts = getClientAlerts();
            r.staffAlerts = getStaffAlerts();

            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskGetClientPage", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@clsvId", clsvId);
                        cmd.Parameters.AddWithValue("@TimeZone", UserClaim.timeZone);
                        cmd.Parameters.AddWithValue("@docStartDate", r.documentationStart);
                        cmd.Parameters.AddWithValue("@docEndDate", r.documentationEnd);
                        cmd.Parameters.AddWithValue("@docSelectNotes", r.documentationSelNotes);
                        cmd.Parameters.AddWithValue("@docSelectReports", r.documentationSelReports);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                setClientProfile(ref r, ref ds, false);
                r.services = setClientServices(ref ds, 1);
                setClientServiceDetails(ref ds, 2, 3, 4, 16, ref r);
                r.geoLocations = setGeoLocations(ref ds, 5);
                r.charts = setClientCharts(ref ds, 6);
                r.commentHistory = setCommentHistory(ref ds, 7);
                r.serviceObjectives = setObjectives(ref ds, 8);
                r.careAreas = setCareAreas(ref ds, 9);
                r.documentation = setClientDocumentation(ref ds, 10);
                r.periods = setPeriods(ref ds, 11);

                r.assignedProviders = ds.Tables[12].Rows.Cast<DataRow>().Select(spR => new AssignedProvider()
                {
                    providerId = (int)spR["prId"],
                    providerName = (string)spR["nm"]
                }).ToList();

                r.clientHours.Periods = setPeriods(ref ds, 13);
                r.clientHours.PeriodId = Convert.ToInt32(ds.Tables[14].Rows[0].ItemArray[0]);
                SetClientBillingMatrix(ref ds, 15, ref r.clientHours);
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

            ds.Dispose();
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 500;
                return null;
            }

            else
            {
                /* Figure out which screen to use */
                if (r.userLevel == "Provider" || r.userLevel == "TherapyAssistant")
                    return PartialView("Task_Provider_Client", r);
                else if (r.userLevel == "TherapySupervisor")
                    return PartialView("Task_Therapist_Client", r);
                else
                    return PartialView("Task_Director_Client", r);
            }
        }

        private List<ClientAlert> getClientAlerts()
        {
            bool er = false;
            DataSet ds = new DataSet();
            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_TaskGetClientAlerts", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
            }
            List<ClientAlert> clientAlerts = setClientAlerts(ref ds, 0);

            ds.Dispose();

            return clientAlerts;
        }
        private List<ClientAlert> setClientAlerts(ref DataSet ds, int tableIdx)
        {
            DataView dv = new DataView(ds.Tables[tableIdx]);
            dv.Sort = "cln ASC, cfn ASC";
            List<ClientAlert> clientAlerts;
            if (UserClaim.userLevel != "Provider" && UserClaim.userLevel != "Supervisor" && UserClaim.userLevel != "TherapyAssistant" && UserClaim.userLevel != "TherapySupervisor")
            {
                clientAlerts = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new ClientAlert()
                {
                    priority = (int)spR["priority"],
                    id = spR["clsvId"] + "-" + UserClaim.coid,
                    alert = (string)spR["alert"],
                    name = spR["cln"] + " " + spR["cfn"],
                    clwEm = spR["clwEm"] != DBNull.Value ? (string)spR["clwEm"] : "",
                    clwNm = spR["clwNm"] != DBNull.Value ? (string)spR["clwNm"] : "",
                    clwPh = spR["clwPh"] != DBNull.Value ? (string)spR["clwPh"] : ""
                }).ToList();
            }
            else
            {
                clientAlerts = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new ClientAlert()
                {
                    priority = (int)spR["priority"],
                    id = spR["clsvId"] + "-" + UserClaim.coid,
                    alert = (string)spR["alert"],
                    name = spR["cln"] + " " + spR["cfn"],
                    clwEm = "",
                    clwNm = "",
                    clwPh = ""
                }).ToList();
            }
            dv.Dispose();
            return clientAlerts;
        }


        private List<StaffAlert> getStaffAlerts()
        {

            DataSet ds = new DataSet();

            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_TaskGetStaffAlerts", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                cmd.Parameters.AddWithValue("@otherRole", UserClaim.dcwRole);
                cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
            }
            List<StaffAlert> staffAlerts = setStaffAlerts(ref ds, 0);

            ds.Dispose();

            return staffAlerts;


        }

        private List<StaffAlert> setStaffAlerts(ref DataSet ds, int tableIdx)
        {

            DataView dv = new DataView(ds.Tables[tableIdx]);
            dv.Sort = "sln ASC, sfn ASC";


            List<StaffAlert> staffAlerts = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new StaffAlert()
            {
                priority = (int)spR["priority"],
                id = spR["prId"] + "-" + UserClaim.coid,
                alert = (string)spR["alert"],
                name = spR["sln"] + " " + spR["sfn"],
                sNm = spR["sfn"] + " " + spR["sln"]
            }).ToList();
            dv.Dispose();
            return staffAlerts;
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
            r.clientProfile.responsiblePersonRelationship = dr1["relationship"]== DBNull.Value ? "" : dr1["relationship"].ToString();
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
                billingType = spR["billingType"] == DBNull.Value ? 0 : (int)spR["billingType"],
                assignedRateId = spR["assignedRateId"] == DBNull.Value ? 0 : (int)spR["assignedRateId"],
                assignedRateName= spR["rateName"] == DBNull.Value ? "" :  spR["rateName"] + " / $" + spR["rate"],

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
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 500;
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
            Windows w = new Windows();
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
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@fn", c.fn);
                        cmd.Parameters.AddWithValue("@ln", c.ln);
                        cmd.Parameters.AddWithValue("@clId", c.clId == null ? "" : c.clId);
                        cmd.Parameters.AddWithValue("@medicaidId", c.medicaidId == null ? "" : c.medicaidId);
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
                        cmd.Parameters.AddWithValue("@getClientAlerts", true);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                setClientProfile(ref r, ref ds, false);
                w.clientProfile = RenderRazorViewToString("Client_Profile", r);
                r.clientAlerts = setClientAlerts(ref ds, 1);
                w.alertTgt = "cAlert" + c.clsvId;
                w.clientAlerts = RenderRazorViewToString("ClientAlertsBlock", r.clientAlerts);



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
                Response.StatusCode = 500;
                return null;
            }
            else
                return Json(w);
        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenActivateModal(int clsvidId)
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
                Response.StatusCode = 500;
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
            Windows w = new Windows();
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
                        cmd.Parameters.AddWithValue("@getClientAlerts", true);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@userlevel", UserClaim.userLevel);

                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.services = setClientServices(ref ds, 0);
                setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);
                r.clientAlerts = setClientAlerts(ref ds, 5);
                w.clientServices = RenderRazorViewToString("Client_Services", r);
                w.alertTgt = "cAlert" + t.clsvId;
                w.clientAlerts = RenderRazorViewToString("ClientAlertsBlock", r.clientAlerts);

            }
            catch (Exception ex)
            {
                w.er.code = 1;
                w.er.msg = ex.Message;
            }

            ds.Dispose();
            return Json(w);
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
                Response.StatusCode = 500;
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
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();
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
                        cmd.Parameters.AddWithValue("@getClientAlerts", true);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@userlevel", UserClaim.userLevel);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.services = setClientServices(ref ds, 0);
                setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);
                r.clientAlerts = setClientAlerts(ref ds, 5);
                w.clientServices = RenderRazorViewToString("Client_Services", r);
                w.alertTgt = "cAlert" + n.clsvId;
                w.clientAlerts = RenderRazorViewToString("ClientAlertsBlock", r.clientAlerts);

            }
            catch (Exception ex)
            {
                w.er.code = 1;
                w.er.msg = ex.Message;
            }

            ds.Dispose();
            return Json(w);
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
                Response.StatusCode = 500;
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
                Response.StatusCode = 500;
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
                    r.reportPeriodEndDateOption.Add(new SelectOption() { name = date, value = date });
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
                Response.StatusCode = 500;
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
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();
            if (n.nextReportDueDate != null)
            {
                if (Convert.ToDateTime(n.nextReportDueDate) < DateTime.Now.AddMonths(-4))
                {
                    w.er.code = 1;
                    w.er.msg = "Cannot backdate end date more than 4 month!";
                }
            }
            if (w.er.code == 0)
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
                            cmd.Parameters.AddWithValue("@getClientAlerts", true);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@userlevel", UserClaim.userLevel);

                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    });
                    r.services = setClientServices(ref ds, 0);
                    setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);
                    r.clientAlerts = setClientAlerts(ref ds, 5);
                    w.clientServices = RenderRazorViewToString("Client_Services", r);
                    w.alertTgt = "cAlert" + n.clsvId;
                    w.clientAlerts = RenderRazorViewToString("ClientAlertsBlock", r.clientAlerts);

                }
                catch (Exception ex)
                {
                    w.er.code = 1;
                    w.er.msg = ex.Message;
                }

                ds.Dispose();
            }


      
            return Json(w);

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
                Response.StatusCode = 500;
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
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();
            if (n.nextATCMonitoringVisit != null)
            {
                if (Convert.ToDateTime(n.nextATCMonitoringVisit) < DateTime.Now.AddMonths(-1))
                {
                    w.er.code = 1;
                    w.er.msg = "Cannot backdate end date more than one month!";
                }
            }
            if (w.er.code == 0)
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
                            cmd.Parameters.AddWithValue("@getClientAlerts", true);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@userlevel", UserClaim.userLevel);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    });
                    r.services = setClientServices(ref ds, 0);
                    setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);
                    r.clientAlerts = setClientAlerts(ref ds, 5);
                    w.clientServices = RenderRazorViewToString("Client_Services", r);
                    w.alertTgt = "cAlert" + n.clsvId;
                    w.clientAlerts = RenderRazorViewToString("ClientAlertsBlock", r.clientAlerts);

                }
                catch (Exception ex)
                {
                    w.er.code = 1;
                    w.er.msg = ex.Message;
                }

                ds.Dispose();
            }

            return Json(w);

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
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();

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
                        cmd.Parameters.AddWithValue("@getClientAlerts", true);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@userlevel", UserClaim.userLevel);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                if (ds.Tables.Count != 0)
                {
                    r.services = setClientServices(ref ds, 0);
                    setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);
                    r.clientAlerts = setClientAlerts(ref ds, 5);
                    w.clientServices = RenderRazorViewToString("Client_Services", r);
                    w.alertTgt = "cAlert" + n.clsvId;
                    w.clientAlerts = RenderRazorViewToString("ClientAlertsBlock", r.clientAlerts);
                }
                else
                {
                    w.er.code = 1;
                    w.er.msg = "Authorizations dates would overlap with an existing auth";

                }

            }
            catch (Exception ex)
            {
                w.er.code = 1;
                w.er.msg = ex.Message;
            }


            return Json(w);
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
                Response.StatusCode = 500;
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
                Response.StatusCode = 500;
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
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();
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
                        cmd.Parameters.AddWithValue("@getClientAlerts", true);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@userlevel", UserClaim.userLevel);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.services = setClientServices(ref ds, 0);
                setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);
                r.clientAlerts = setClientAlerts(ref ds, 5);
                w.clientServices = RenderRazorViewToString("Client_Services", r);
                w.alertTgt = "cAlert" + clsvId;
                w.clientAlerts = RenderRazorViewToString("ClientAlertsBlock", r.clientAlerts);
            }
            catch (Exception ex)
            {
                w.er.code = 1;
                w.er.msg = ex.Message;
            }
            return Json(w);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> UpdateAuth(Auth n)
        {
            ClientPageData r = new ClientPageData();
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();

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
                        cmd.Parameters.AddWithValue("@getClientAlerts", true);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@userlevel", UserClaim.userLevel);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                if (ds.Tables.Count != 0)
                {
                    r.services = setClientServices(ref ds, 0);
                    setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);
                    r.clientAlerts = setClientAlerts(ref ds, 5);
                    w.clientServices = RenderRazorViewToString("Client_Services", r);
                    w.alertTgt = "cAlert" + n.clsvId;
                    w.clientAlerts = RenderRazorViewToString("ClientAlertsBlock", r.clientAlerts);
                }
                else
                {
                    w.er.code = 1;
                    w.er.msg = "Authorizations dates would overlap with an existing auth";
                }

            }
            catch (Exception ex)
            {
                w.er.code = 1;
                w.er.msg = ex.Message;
            }
            ds.Dispose();
            return Json(w);

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

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public ActionResult OpenDocumentDeleteModal(PendingDocumentation r)
        {
                return PartialView("ModalDeleteDocument", r);

        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> DeleteDocument(int providerId, string docType, int docId)
        {
            HomeStaffPage r = new HomeStaffPage();
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_DeleteDocument", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userPrId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@prId", providerId);
                        cmd.Parameters.AddWithValue("@docType", docType);
                        cmd.Parameters.AddWithValue("@docId", docId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.pendingDocumentation = setPendingDocumentation(ref ds, 0);
                r.staffAlerts = setStaffAlerts(ref ds, 1);
                w.pendingDocumentation = RenderRazorViewToString("PendingDocumentation", r);
                w.staffAlerts = RenderRazorViewToString("StaffAlerts", r.staffAlerts);
            }
            catch (Exception ex)
            {
                w.er.code = 1;
                w.er.msg = ex.Message;
            }
            ds.Dispose();
            var jsonResult = Json(w);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;


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
                Response.StatusCode = 500;
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
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();

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
                        cmd.Parameters.AddWithValue("@getClientAlerts", true);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@userlevel", UserClaim.userLevel);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.services = setClientServices(ref ds, 0);
                setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);
                r.clientAlerts = setClientAlerts(ref ds, 5);
                w.clientServices = RenderRazorViewToString("Client_Services", r);
                w.alertTgt = "cAlert" + n.clsvId;
                w.clientAlerts = RenderRazorViewToString("ClientAlertsBlock", r.clientAlerts);
            }
            catch (Exception ex)
            {
                w.er.code = 1;
                w.er.msg = ex.Message;
            }

            return Json(w);

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
                Response.StatusCode = 500;
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
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();

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
                        cmd.Parameters.AddWithValue("@getClientAlerts", true);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@userlevel", UserClaim.userLevel);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.services = setClientServices(ref ds, 0);
                setClientServiceDetails(ref ds, 1, 2, 3, 4, ref r);
                r.clientAlerts = setClientAlerts(ref ds, 5);
                w.clientServices = RenderRazorViewToString("Client_Services", r);
                w.alertTgt = "cAlert" + n.clsvId;
                w.clientAlerts = RenderRazorViewToString("ClientAlertsBlock", r.clientAlerts);
            }
            catch (Exception ex)
            {
                w.er.code = 1;
                w.er.msg = ex.Message;
            }

            return Json(w);

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
                Response.StatusCode = 500;
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
                Response.StatusCode = 500;
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
                Response.StatusCode = 500;
                return null;
            }
            else
                return PartialView("Client_Services", r);
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
                Response.StatusCode = 500;
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
                Response.StatusCode = 500;
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

            if (g.st == "AZ" && (g.locationTypeId== 1 || g.locationTypeId==2))
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
                            cmd.Parameters.AddWithValue("@zip", g.zip.Substring(0,5));

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
                    }
                    catch (Exception ex)
                    {
                        r.er.code = 1;
                        r.er.msg = ex.Message;
                    }
                    if (r.er.code == 0)
                    {
                        r.geoLocations = setGeoLocations(ref ds, 0);
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
                Response.StatusCode = 500;
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
                Response.StatusCode = 500;
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
                Response.StatusCode = 500;
                return null;
            }
            else
                return PartialView("Client_Locations", r);
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
                Response.StatusCode = 500;
                return null;
            }
            else
                return PartialView("Client_Comments", r);
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
                Response.StatusCode = 500;
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
            Windows w = new Windows();
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


                        if (r.er.code == 0)
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
                                        cmd.Parameters.AddWithValue("@getClientAlerts", true);
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
                                    r.clientAlerts = setClientAlerts(ref ds, 6);
                                    w.clientServices = RenderRazorViewToString("Client_Services", r);
                                    w.clientCharts = RenderRazorViewToString("Client_Charts", r);
                                    w.alertTgt = "cAlert" + clsvId;
                                    w.clientAlerts = RenderRazorViewToString("ClientAlertsBlock", r.clientAlerts);
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
            Windows w = new Windows();
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
                        cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                        cmd.Parameters.AddWithValue("@getClientAlerts", true);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        return ds;
                    }
                });
                if (ds.Tables[0].Rows.Count != 0)
                {

                    FileData f = new FileData("charts", UserClaim.blobStorage);
                    f.DeleteFile(s.chartId + ds.Tables[0].Rows[0].ItemArray[0].ToString());

                    r.charts = setClientCharts(ref ds, 1);

                    r.services = setClientServices(ref ds, 2);
                    setClientServiceDetails(ref ds, 3, 4, 5, 6, ref r);
                    r.clientAlerts = setClientAlerts(ref ds, 7);
                    w.clientServices = RenderRazorViewToString("Client_Services", r);
                    w.clientCharts = RenderRazorViewToString("Client_Charts", r);
                    w.alertTgt = "cAlert" + s.clsvId;
                    w.clientAlerts = RenderRazorViewToString("ClientAlertsBlock", r.clientAlerts);
                }
            }
            catch (Exception ex)
            {
                w.er.code = 1;
                w.er.msg = ex.Message;

            }

            ds.Dispose();
            return Json(w);
        }
        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> openEditCredentialModal(CredentialModal r)
        {
            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_StaffGetCredentialsForEdit", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@credId", r.credId);
                        cmd.Parameters.AddWithValue("@prId", r.prId);

                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.requiredCredentials = setStaffRequiredCredentials(ref ds, 0);
                DataRow dr = ds.Tables[1].Rows[0];
                r.credName = (string)dr["credName"];
                r.credId = (int)dr["credId"];
                r.credTypeId = (int)dr["credTypeId"];
                r.docId = (string)dr["docId"];
                r.validFrom = ((DateTime)dr["validFrom"]).ToString("yyyy-MM-dd");
                r.validTo = ((DateTime)dr["validTo"]).ToString("yyyy-MM-dd");

            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 500;
                return null;
            }
            else
                return PartialView("ModalEditStaffCredential", r);

        }


        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenAddCredentialModal(CredentialModal r)
        {

            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_StaffGetCredentialsForEdit", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@prId", r.prId);
                        cmd.Parameters.AddWithValue("@credId", 0);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.requiredCredentials = setStaffRequiredCredentials(ref ds, 0);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 500;
                return null;
            }
            else
                return PartialView("ModalAddStaffCredential", r);

        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> UpdateCredential(IEnumerable<HttpPostedFileBase> files, int prId, int credId, int credTypeId, string docId, string validFrom, string validTo)
        {

            if (credId == 0 && files.Count() == 0) 
            {
                Response.Write("No file was uploaded");
                Response.StatusCode = 500;
                return null;
            }

            HomeStaffPage r = new HomeStaffPage();
            r.userLevel = UserClaim.userLevel;
            r.userPrId = UserClaim.prid;
            r.prId = prId;
            string fileName;
            int newCredId = 0;
            string fileExtension = null;
            string contentType = null;
            DataSet ds = new DataSet();
            // insert or update credential info
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_StaffAddCredential", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@prId", prId);
                        cmd.Parameters.AddWithValue("@credId", credId);
                        cmd.Parameters.AddWithValue("@credTypeId", credTypeId);
                        cmd.Parameters.AddWithValue("@docId", docId);
                        cmd.Parameters.AddWithValue("@validFrom", validFrom);
                        cmd.Parameters.AddWithValue("@validTo", validTo);
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
                if (ds.Tables.Count != 0 && ds.Tables[0].Rows.Count != 0)
                {
                    newCredId = Convert.ToInt32(ds.Tables[0].Rows[0].ItemArray[0]);
                    fileExtension = Convert.ToString(ds.Tables[0].Rows[0].ItemArray[1]);
                    contentType = Convert.ToString(ds.Tables[0].Rows[0].ItemArray[2]);
                }
                else
                {
                    r.er.code = 1;
                    r.er.msg = "Failed to get file data!";
                }
            }
            ds.Dispose();
            if (r.er.code == 0)
            {
                if (files != null && files.Count() != 0)
                {
                    var file = files.FirstOrDefault();
                    // Verify that the user selected a file
                    if (file != null && file.ContentLength > 0)
                    {
                        fileExtension = Path.GetExtension(file.FileName);
                        fileName = newCredId + fileExtension;
                        contentType = file.ContentType;
                        try
                        {
                            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                            {
                                SqlCommand cmd = new SqlCommand("INSERT INTO APIFile (credId,fileName)VALUES(" + newCredId +",'" + file.FileName + " - " + Request.Browser.Browser + "')", cn)
                                {
                                   
                                };
                                cn.Open();
                                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                                cn.Close();
                            }

                            if (fileExtension.ToLower() == ".jpg" || fileExtension.ToLower() == ".jpeg" || fileExtension.ToLower() == ".png" || fileExtension.ToLower() == ".bmp")
                            {
                                // resize the image and convert to jpeg
                                fileExtension = ".jpg";
                                fileName = newCredId + fileExtension;
                                contentType = "image/jpeg";
                                MemoryStream ms = new MemoryStream();
                                ImageUtility.ResizeImage(file.InputStream, ref ms);
                                byte[] data = new byte[ms.Length];
                                ms.Seek(0, SeekOrigin.Begin);
                                ms.Read(data, 0, data.Length);
                                ms.Dispose();
                                FileData f = new FileData("credentials", UserClaim.blobStorage);
                                f.StoreFile(data, fileName);
                            }
                            else
                            {
                               
                                byte[] data = new byte[file.InputStream.Length];
                                file.InputStream.Read(data, 0, data.Length);
                                FileData f = new FileData("credentials", UserClaim.blobStorage);
                                f.StoreFile(data, fileName);
                            }

                        }
                        catch (Exception ex)
                        {
                            r.er.code = 1;
                            r.er.msg = ex.Message;
                            // delete cred
                            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                            {
                                SqlCommand cmd = new SqlCommand("DELETE FROM StaffCredentials WHERE credId=" + newCredId, cn)
                                {
                                };
                                cn.Open();
                                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                                cn.Close();
                            }
                        }

                    }
                    else
                    {
                        r.er.code = 1;
                        if (file == null)
                            r.er.msg = "Bad File upload  - null file";
                        else
                            r.er.msg = "Bad File Upload - No Content";

                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("INSERT INTO APIFile (credId,fileName)VALUES(" + newCredId + ",'" + r.er.msg + " - " + Request.Browser.Browser + "')", cn)
                            {

                            };
                            cn.Open();
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                            cn.Close();
                        }

                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("DELETE FROM StaffCredentials WHERE credId=" + newCredId, cn)
                            {
                            };
                            cn.Open();
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                            cn.Close();
                        }
                    }
                }
                if (r.er.code == 0)
                {
                    if (newCredId != 0 || fileExtension != null)
                    {
                        DataSet ds2 = new DataSet();
                        try
                        {
                            await Task.Run(() =>
                            {
                                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                                {
                                    SqlCommand cmd = new SqlCommand("sp_StaffUpdateCredentialFiltered", cn)
                                    {
                                        CommandType = CommandType.StoredProcedure
                                    };
                                    cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                                    cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                                    cmd.Parameters.AddWithValue("@prId", prId);
                                    cmd.Parameters.AddWithValue("@credId", newCredId);
                                    cmd.Parameters.AddWithValue("@fileExtension", fileExtension);
                                    cmd.Parameters.AddWithValue("@contentType", contentType);
                                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds2);
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
                                r.credentials = setStaffCredentials(ref ds2, prId, 0);
                            }
                            catch (Exception ex)
                            {
                                r.er.code = 1;
                                r.er.msg = ex.Message;
                            }
                        }
                        ds2.Dispose();
                    }
                }
            }
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 500;
                return null;
            }
            else
                return PartialView("Staff_Credentials", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenVerifyCredentialModal(CredentialModal r)
        {

            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_StaffGetCredentialsForEdit", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@prId", r.prId);
                        cmd.Parameters.AddWithValue("@credId", r.credId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                DataRow dr = ds.Tables[1].Rows[0];
                r.credName = (string)dr["credName"];
                r.credId = (int)dr["credId"];
                r.credTypeId = (int)dr["credTypeId"];
                r.docId = (string)dr["docId"];
                r.validFrom = ((DateTime)dr["validFrom"]).ToShortDateString();
                r.validTo = ((DateTime)dr["validTo"]).ToShortDateString();
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 500;
                return null;
            }
            else
                return PartialView("ModalVerifyStaffCredential", r);

        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> VerifyCredential(CredentialVerificationUpdate c)
        {
            HomeStaffPage r = new HomeStaffPage();
            r.userLevel = UserClaim.userLevel;
            r.userPrId = UserClaim.prid;
            r.prId = c.prId;
            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_StaffVerifyCredentialFiltered", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@prId", c.prId);
                        cmd.Parameters.AddWithValue("@credId", c.credId);
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
                    r.credentials = setStaffCredentials(ref ds, c.prId, 0);
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
                Response.StatusCode = 500;
                return null;
            }
            else
                return PartialView("Staff_Credentials", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenDeleteCredentialModal(CredentialModal r)
        {

            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_StaffGetCredentialsForEdit", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@prId", r.prId);
                        cmd.Parameters.AddWithValue("@credId", r.credId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                DataRow dr = ds.Tables[1].Rows[0];
                r.credName = (string)dr["credName"];
                r.credId = (int)dr["credId"];
                r.credTypeId = (int)dr["credTypeId"];
                r.docId = (string)dr["docId"];
                r.validFrom = ((DateTime)dr["validFrom"]).ToShortDateString();
                r.validTo = ((DateTime)dr["validTo"]).ToShortDateString();
                r.docName = (int)dr["credId"] + (string)dr["fileExtension"];
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
                return PartialView("ModalDeleteStaffCredential", r);

        }


        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> DeleteCredential(CredentialModal c)
        {
            HomeStaffPage r = new HomeStaffPage();
            r.userLevel = UserClaim.userLevel;
            r.userPrId = UserClaim.prid;
            r.prId = c.prId;
            DataSet ds = new DataSet();

            try
            {
                FileData f = new FileData("credentials", UserClaim.blobStorage);
                f.DeleteFile(c.docName);

                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_StaffDeleteCredentialFiltered", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@prId", c.prId);
                        cmd.Parameters.AddWithValue("@credId", c.credId);
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
                    r.credentials = setStaffCredentials(ref ds, c.prId, 0);
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
                return PartialView("Staff_Credentials", r);
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
                    Response.AddHeader("Content-Disposition", "attachment;filename=\"" + fileName + "\"");

                    return new FileContentResult(data, ds.Tables[0].Rows[0].ItemArray[1].ToString());
                }
            }

            return View(er);
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
                Response.StatusCode = 500;
                return null;
            }
            else
                //   
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
                Response.StatusCode = 500;
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
                Response.StatusCode = 500;
                return null;
            }
            else
                return PartialView("Client_CareAreas", r);
        }

        [HttpGet]
        [AJAXAuthorize]
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
                Response.StatusCode = 500;
                return null;
            }
            else
                return PartialView("Client_CareAreas", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> GetProviderTimeSheet(int providerId, int clientId, string startEndDates)
        {
            ProviderTimeSheetData r = null;
            string[] dates = startEndDates.Split('-');
            try
            {
                r = await getTimeSheet(clientId, providerId, dates);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 500;
                return null;
            }
            else
            {
                r.userLevel = UserClaim.userLevel;

            }
            return PartialView("Task_ProviderTimeSheet", r);
        }
        [AJAXAuthorize]
        public async Task<ActionResult> InsertProviderTimeSheetRecord(int clientSessionTherapyId, int providerId, int clientId, string svc, string serviceDate, int inOffsetMinutes, int outOffSetMinutes, string serviceInfo, string locationInfo, string startEndDates)
        {
            ProviderTimeSheetData r = new ProviderTimeSheetData();


            string[] locationInfoParsed = locationInfo.Split('|');
            string[] serviceInfoParsed = serviceInfo.Split('-');
            string[] dates = startEndDates.Split('-');

            DateTime x = Convert.ToDateTime(serviceDate);

            DateTime y = Convert.ToDateTime(dates[0]);
            DateTime z = Convert.ToDateTime(dates[1]);


            if (x < Convert.ToDateTime(dates[0]) || x > Convert.ToDateTime(dates[1]))
            {
                Response.Write("Date is incorrect for time period try MM/DD/YYYY or just enter the day of month.");
                Response.StatusCode = 500;
                return null;
            }


            InOutProcessor t = new InOutProcessor(UserClaim.conStr, UserClaim.timeZone);
            if (ConvertToUTC(Convert.ToDateTime(serviceDate).AddMinutes(outOffSetMinutes)) < DateTime.UtcNow)
            {
                t.HCBSEmpHrsId = 0;

                t.utcIn = ConvertToUTC((Convert.ToDateTime(serviceDate)).AddMinutes(inOffsetMinutes));

                if (outOffSetMinutes < inOffsetMinutes)
                    t.utcOut = ConvertToUTC((Convert.ToDateTime(serviceDate)).AddDays(1).AddMinutes(outOffSetMinutes));
                else
                    t.utcOut = ConvertToUTC((Convert.ToDateTime(serviceDate)).AddMinutes(outOffSetMinutes));
                t.adjutcIn = null;
                t.adjutcOut = null;
                t.prId = providerId;
                t.clsvId = clientId;
                t.clsvidId = Convert.ToInt32(serviceInfoParsed[1]);
                t.serviceId = Convert.ToInt32(serviceInfoParsed[0]);
                t.svc = svc;
                t.inCallType = 0;
                t.outCallType = 0;
                t.callType = 0;
                t.startLocationTypeId = Convert.ToInt32(locationInfoParsed[1]);
                t.startClientLocationId = Convert.ToInt32(locationInfoParsed[0]);
                t.endLocationTypeId = Convert.ToInt32(locationInfoParsed[1]);
                t.endClientLocationId = Convert.ToInt32(locationInfoParsed[0]);
                t.startLat = Convert.ToDecimal(locationInfoParsed[2]);
                t.startLon = Convert.ToDecimal(locationInfoParsed[3]);
                t.endLat = Convert.ToDecimal(locationInfoParsed[2]);
                t.endLon = Convert.ToDecimal(locationInfoParsed[3]);
                t.isEVV = false;


                // The stuff here allow HCBS to deal with sessions through midnight AZ time
                var TimeZone = TimeZoneInfo.FindSystemTimeZoneById(UserClaim.timeZone);
                DateTime AZInTime = TimeZoneInfo.ConvertTimeFromUtc((DateTime)t.utcIn, TimeZone);
                var startDayLocal = new DateTime(AZInTime.Year, AZInTime.Month, AZInTime.Day, 0, 0, 0, DateTimeKind.Unspecified);
                var startDayUTC = TimeZoneInfo.ConvertTimeToUtc(startDayLocal, TimeZone);
                t.date1 = startDayLocal.ToShortDateString();
                t.utcPartition1Start = startDayUTC;
                t.utcPartition1End = t.utcPartition1Start.AddDays(1);
                DateTime AZOutTime = TimeZoneInfo.ConvertTimeFromUtc((DateTime)t.utcOut, TimeZone);

                if (AZInTime.Day != AZOutTime.Day)
                {  // if this session crosses midnight need to times when day starts and ends locally for next day in our time zone
                    t.date2 = startDayLocal.AddDays(1).ToShortDateString();
                    t.utcPartition2Start = t.utcPartition1End;
                    t.utcPartition2End = t.utcPartition1End.AddDays(1);
                }


                try
                {
                    if (serviceInfoParsed[2] == "Therapy")
                    {
                        t.checkTherapyRecordIsValid(ref r.er);
                        if (r.er.code == 0)
                        {
                            t.checkTherapyRecordAgainstAuths(ref r.er);
                            if (r.er.code == 0)
                            {
                                t.updateTherapyRecord(ref r.er);
                            }
                        }
                    }
                    else if (serviceInfoParsed[2] == "HCBS")
                    {
                        t.checkHCBSRecordIsValid(ref r.er);
                        if (r.er.code == 0)
                        {
                           if (UserClaim.userLevel == "Provider")
                                t.checkHCBSRecordAgainstAuths(ref r.er);
                            if (r.er.code == 0)
                            {
                                t.updateHCBSRecord(ref r.er, UserClaim.prid);
                            }
                        }
                    }
                    if (r.er.code == 0)
                    {
                        r = await getTimeSheet(clientId, providerId, dates);
                        if (r.er.code != 0)
                        {
                            r.er.code = 1;
                            r.er.msg = r.er.msg;
                        }
                    }

                }
                catch (Exception ex)
                {
                    r.er.code = 1;
                    r.er.msg = ex.Message;
                }

            }

            else
            {
                r.er.code = 1;
                r.er.msg = "You cannot enter sessions in advance";
            }

            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
            {
                r.userLevel = UserClaim.userLevel;

            }
            return PartialView("Task_ProviderTimeSheet", r);
        }


        [AJAXAuthorize]
        public async Task<ActionResult> DeleteProviderTimeSheetRecord(int clientSessionTherapyId, string date, int clientId, int providerId, string startEndDates, string svcType)
        {

            string[] dates = startEndDates.Split('-');
            Er er = new Er();
            ProviderTimeSheetData r = null;
            try
            {
                InOutProcessor InOutProcessor = new InOutProcessor(UserClaim.conStr,UserClaim.timeZone);
                if (svcType == "Therapy")
                {
                    InOutProcessor.deleteTherapyRecord(clientSessionTherapyId, ref er);
                }

                else if (svcType == "HCBS")
                {
                    InOutProcessor.deleteHCBSRecord(clientSessionTherapyId, date, ref er);
                }
                if (er.code == 0)
                {
                    r = await getTimeSheet(clientId, providerId, dates);
                    if (r.er.code != 0)
                    {
                        er.code = 1;
                        er.msg = r.er.msg;
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
            {
                r.userLevel = UserClaim.userLevel;

            }
            return PartialView("Task_ProviderTimeSheet", r);
        }



        public async Task<ActionResult> UpdateProviderLocation(int sessionId, string locationId, string noteType)
        {
            Er er = new Er();
            string[] locationInfoParsed = locationId.Split('|');

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_UpdateProviderLocation", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@sessionId", sessionId);
                        cmd.Parameters.AddWithValue("@clientLocationId", locationInfoParsed[0]);
                        cmd.Parameters.AddWithValue("@locationTypeId", locationInfoParsed[1]);
                        cmd.Parameters.AddWithValue("@noteType", noteType);
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

            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 500;
                return null;
            }
            return Json(er);

        }


        private async Task<ProviderTimeSheetData> getTimeSheet(int clientId, int providerId, string[] dates) 
        {
            ProviderTimeSheetData r = new ProviderTimeSheetData();

            DataSet ds = new DataSet();
           try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskGetProviderTimeSheet", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@providerId", providerId);
                        cmd.Parameters.AddWithValue("@clientId", clientId);
                        cmd.Parameters.AddWithValue("@LocalPeriodStart", dates[0]);
                        cmd.Parameters.AddWithValue("@LocalPeriodEnd", dates[1]);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.providerId = providerId;
                r.periodStartDate =Convert.ToDateTime(dates[0]).ToString("yyyy-MM-dd");
                r.periodEndDate = Convert.ToDateTime(dates[1]).ToString("yyyy-MM-dd"); ;
                r.sessions = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new TherapySession()
                {
                    clientSessionTherapyId = Convert.ToInt32(spR["clientSessionTherapyId"]),

                    serviceName = (string)spR["svc"],
                    serviceId = spR["serviceId"] + "-" + spR["clientServiceId"],
                    svcType = (string)spR["svcType"],
                    allowManualInOut = (bool)spR["allowManualInOut"],

                    locationName = (string)spR["Location"],
                    locationId = spR["clientLocationId"] + "|" + spR["locationTypeId"] + "|" + spR["lat"] + "|" + spR["lon"],

                    // YYYY for therapies only or need to check HCBS
                    svcDate = ((DateTime)spR["dt"]).ToShortDateString(),

                    inDate = DateTimeLocal((DateTime)spR["startAt"]).ToShortDateString(),
                    outDate = DateTimeLocal((DateTime)spR["endAt"]).ToShortDateString(),
                    inTime = DateTimeLocal((DateTime)spR["startAt"]).ToShortTimeString(),
                    outTime = DateTimeLocal((DateTime)spR["endAt"]).ToShortTimeString(),

                    svcDateAdj = spR["dtAdj"] == DBNull.Value ? null : ((DateTime)spR["dtAdj"]).ToShortDateString(),
                    inDateAdj = spR["adjStartAt"] == DBNull.Value ? null : DateTimeLocal((DateTime)spR["adjStartAt"]).ToShortDateString(),
                    inTimeAdj = spR["adjStartAt"] == DBNull.Value ? null : DateTimeLocal((DateTime)spR["adjStartAt"]).ToShortTimeString(),

                    outDateAdj = spR["adjEndAt"] == DBNull.Value ? null : DateTimeLocal((DateTime)spR["adjEndAt"]).ToShortDateString(),
                    outTimeAdj = spR["adjEndAt"] == DBNull.Value ? null : DateTimeLocal((DateTime)spR["adjEndAt"]).ToShortTimeString(),
                    units = calulateUnits15MinRule(
                            spR["adjEndAt"] == DBNull.Value ? (DateTime)spR["endAt"] : (DateTime)spR["adjEndAt"],
                          spR["adjStartAt"] == DBNull.Value ? (DateTime)spR["startAt"] : (DateTime)spR["adjStartAt"]),
                    // YYYY end
                    completedNote = (bool)spR["completed"],
                    approvedNote = (string)spR["noteType"] == "TherapyNote" ? (bool)spR["verified"] : true,
                    designeeApproved = (bool)spR["designeeApproved"],
                    isEvaluation = (bool)spR["isEvaluation"],
                    noteType = (string)spR["noteType"],
                    isEVV = (bool)spR[ "isEVV"],
                    inCallTYpe = Convert.ToInt32(spR["inCallType"]),
                    outCallType = Convert.ToInt32(spR["outcallType"]),
                }).ToList();

                r.locations = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {

                    name = (string)spR["Location"],
                    value = spR["clLocId"] + "|" + spR["billinglocationTypeId"] + "|" + spR["lat"] + "|" + spR["lon"]
                }).ToList();

                r.services = ds.Tables[2].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {

                    name = (string)spR["svc"],
                    value = spR["serviceId"] + "-" + spR["clientServiceId"] + "-" + spR["svcType"] + "-" + ((bool)spR["allowManualInOut"] == true ? 1 : 0)
                }).ToList();


                // for day entry checks within specified dtea
                DateTime startDate = Convert.ToDateTime(dates[0]);
                DateTime endDate = Convert.ToDateTime(dates[1]);


                r.periodStartDateISO = startDate.ToString("yyyy-MM-dd");
                r.periodEndDateISO = endDate.ToString("yyyy-MM-dd");



                int DateCount = 0;
                if (startDate.Month == endDate.Month)
                    DateCount = 1;
                else DateCount = 2;
                if (DateCount == 1)
                {
                    // dates lie within same month
                    ValidDate validDate = new ValidDate();
                    validDate.yr = startDate.Year;
                    validDate.mn = startDate.Month;
                    validDate.sdy = startDate.Day;
                    validDate.edy = endDate.Day;
                    r.validDates.Add(validDate);
                }
                else
                {
                    // dates span over 2 months
                    ValidDate validDate = new ValidDate();
                    validDate.yr = startDate.Year;
                    validDate.mn = startDate.Month;
                    validDate.sdy = startDate.Day;
                    validDate.edy = new DateTime(endDate.Year, endDate.Month, 1).AddDays(-1).Day;
                    r.validDates.Add(validDate);


                    validDate = new ValidDate();
                    validDate.yr = endDate.Year;
                    validDate.mn = endDate.Month;
                    validDate.sdy = 1;
                    validDate.edy = endDate.Day;


                    r.validDates.Add(validDate);
                }


            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            ds.Dispose();
            return r;
        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> GetProviderTimeSheetPDF(int providerId, string clientId, string startEndDates)
        {
            Er er = new Er();

            string[] dates = startEndDates.Split('-');

            try
            {
                DataSet CompanyData = new DataSet();
                DataSet TimeSheetData = new DataSet();
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetCompanyById", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@coId", UserClaim.coid);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, CompanyData);
                    }

                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskGetProviderTimeSheetForPrinter", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@providerId", providerId);
                        cmd.Parameters.AddWithValue("@clientId", clientId);
                        cmd.Parameters.AddWithValue("@LocalPeriodStart", dates[0]);
                        cmd.Parameters.AddWithValue("@LocalPeriodEnd", dates[1]);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, TimeSheetData);

                    }
                });
          

                // create Pdf


                XGraphics gfx;
                //   protected XGraphics gfx2;
                XPen pen10 = new XPen(XColors.DimGray, 1.0);
                XPen pen05 = new XPen(XColors.DimGray, 0.5);
                XPen pen05B = new XPen(XColors.Black, 0.5);

                PdfDocument document = new PdfDocument();
                XFont A12B = new XFont("Arial", 12, XFontStyle.Bold);
                XFont A11 = new XFont("Arial", 11);
                XFont A10 = new XFont("Arial", 10);
                XFont A10B = new XFont("Arial", 10, XFontStyle.Bold);

                XFont A9 = new XFont("Arial", 9);
                XFont A10I = new XFont("Arial", 10, XFontStyle.Italic);
                XFont A10U = new XFont("Arial", 10, XFontStyle.Underline);
                XFont A10UI = new XFont("Arial", 10, XFontStyle.BoldItalic);
                XFont A11B = new XFont("Arial", 11, XFontStyle.Bold);
                XFont A8I = new XFont("Arial", 8, XFontStyle.Italic);
                
                DataRow clientRow = TimeSheetData.Tables[1].Rows[0];
                string clientName = clientRow["ln"] + ", " + clientRow["fn"];
                DataRow providerRow = TimeSheetData.Tables[0].Rows[0];
                string providerName = providerRow["ln"] + ", " + providerRow["fn"];

                string fileName = clientName.Replace(",", "").Replace("'", "") + "-" + providerName.Replace(",", "").Replace("'", "") + "-" + dates[0].Replace("/", "_") + "-" + dates[1].Replace("/", "_") + ".pdf";

                int pgCnt = 0;
                if (TimeSheetData.Tables[2].Rows.Count <= 50) pgCnt = 1;
                else if (TimeSheetData.Tables[2].Rows.Count <= 100) pgCnt = 2;
                else if (TimeSheetData.Tables[2].Rows.Count <= 150) pgCnt = 3;
                else pgCnt = 4;
                for (int pg = 1; pg < pgCnt + 1; pg++)
                {
                    PdfPage page = document.AddPage();
                    page.Size = PageSize.A4;
                    page.Orientation = PageOrientation.Portrait;
                    gfx = XGraphics.FromPdfPage(page);
                    XTextFormatter tf = new XTextFormatter(gfx);
                    XRect rect = new XRect(20, 30, 200, 13);

                    gfx.DrawRectangle(XBrushes.Transparent, rect);
                    tf.DrawString(((bool)providerRow["basicProvider"] ? "HCBS" : "Therapy") + " Timesheet - CONFIDENTIAL", A12B, XBrushes.DimGray, rect, XStringFormats.TopLeft);


                    rect = new XRect(20, 43, 300, 13);
                    DataRow dr = CompanyData.Tables[0].Rows[0];
                    gfx.DrawRectangle(XBrushes.Transparent, rect);
                    tf.DrawString((string)dr["name"], A11B, XBrushes.DimGray, rect, XStringFormats.TopLeft);


                    rect = new XRect(300, 43, 250, 13);
                    gfx.DrawRectangle(XBrushes.Transparent, rect);
                    tf.DrawString("Provider: " + providerName, A11B, XBrushes.DimGray, rect, XStringFormats.TopLeft);

                    rect = new XRect(300, 55, 250, 13);
                    gfx.DrawRectangle(XBrushes.Transparent, rect);
                    dr = TimeSheetData.Tables[1].Rows[0];
                    tf.DrawString("Client: " + clientName, A11B, XBrushes.DimGray, rect, XStringFormats.TopLeft);

                    rect = new XRect(300, 67, 250, 13);
                    gfx.DrawRectangle(XBrushes.Transparent, rect);
                    tf.DrawString("Dates: " + dates[0] + " - " + dates[1], A11B, XBrushes.DimGray, rect, XStringFormats.TopLeft);

                    rect = new XRect(300, 79, 250, 13);
                    gfx.DrawRectangle(XBrushes.Transparent, rect);
                    tf.DrawString("Page " + pg + " of " + pgCnt, A11B, XBrushes.DimGray, rect, XStringFormats.TopLeft);


                    gfx.DrawLine(pen05, 40, 130, 40, 742); // Date
                    gfx.DrawLine(pen05, 170, 130, 170, 742); // Service
                    gfx.DrawLine(pen05, 210, 130, 210, 742); // In
                    gfx.DrawLine(pen05, 270, 130, 270, 742); // Out
                    gfx.DrawLine(pen05, 330, 130, 330, 742); // Un
                    gfx.DrawLine(pen05, 390, 130, 390, 742);




                
                    // draw horizontal lines
                    for (int i = 130; i < 750; i += 12)//14
                    {
                        gfx.DrawLine(pen05, 40, i, 390, i);//552
                    }

                    rect = new XRect(43, 131, 250, 13);
                    gfx.DrawRectangle(XBrushes.Transparent, rect);
                    tf.DrawString("Date of Service", A11B, XBrushes.DimGray, rect, XStringFormats.TopLeft);

                    rect = new XRect(173, 131, 250, 13);
                    gfx.DrawRectangle(XBrushes.Transparent, rect);
                    tf.DrawString("Svc", A11B, XBrushes.DimGray, rect, XStringFormats.TopLeft);

                    rect = new XRect(213, 131, 250, 13);
                    gfx.DrawRectangle(XBrushes.Transparent, rect);
                    tf.DrawString("Start", A11B, XBrushes.DimGray, rect, XStringFormats.TopLeft);

                    rect = new XRect(273, 131, 250, 13);
                    gfx.DrawRectangle(XBrushes.Transparent, rect);
                    tf.DrawString("End", A11B, XBrushes.DimGray, rect, XStringFormats.TopLeft);

                    rect = new XRect(333, 131, 250, 13);
                    gfx.DrawRectangle(XBrushes.Transparent, rect);
                    tf.DrawString("Units", A11B, XBrushes.DimGray, rect, XStringFormats.TopLeft);

                    int rowHeight = 143;

                    int sIdx = 0;
                    int eIdx = 0;
                    if (pg == 1)
                    {
                        sIdx = 0;
                        eIdx = 50;
                    }
                    else if (pg == 2)
                    {
                        sIdx = 50;
                        eIdx = 100;
                    }
                    else if (pg == 3)
                    {
                        sIdx = 100;
                        eIdx = 150;
                    }
                    else if (pg == 4)
                    {
                        sIdx = 150;
                        eIdx = 200;
                    }
                    if (TimeSheetData.Tables[2].Rows.Count < eIdx) eIdx = TimeSheetData.Tables[2].Rows.Count;
                    for (int y = sIdx; y < eIdx; y++)
                    {
                        DataRow TimeSheetItem = TimeSheetData.Tables[2].Rows[y];

                        rect = new XRect(43, rowHeight, 250, 13);
                        gfx.DrawRectangle(XBrushes.Transparent, rect);
                        tf.DrawString(((DateTime)TimeSheetItem["dt"]).ToShortDateString(), A10, XBrushes.DimGray, rect, XStringFormats.TopLeft);

                        rect = new XRect(173, rowHeight, 250, 13);
                        gfx.DrawRectangle(XBrushes.Transparent, rect);
                        tf.DrawString((string)TimeSheetItem["svc"], A10, XBrushes.DimGray, rect, XStringFormats.TopLeft);

                        rect = new XRect(213, rowHeight, 250, 13);
                        gfx.DrawRectangle(XBrushes.Transparent, rect);
                        tf.DrawString(DateTimeLocal((DateTime)TimeSheetItem["startAt"]).ToShortTimeString(), A10, XBrushes.DimGray, rect, XStringFormats.TopLeft);

                        rect = new XRect(273, rowHeight, 250, 13);
                        gfx.DrawRectangle(XBrushes.Transparent, rect);
                        tf.DrawString(DateTimeLocal((DateTime)TimeSheetItem["endAt"]).ToShortTimeString(), A10, XBrushes.DimGray, rect, XStringFormats.TopLeft);

                        rect = new XRect(333, rowHeight, 250, 13);
                        gfx.DrawRectangle(XBrushes.Transparent, rect);
                        tf.DrawString(Convert.ToDecimal(TimeSheetItem["units"]).ToString("N2"), A10, XBrushes.DimGray, rect, XStringFormats.TopLeft);

                        rowHeight += 12;
                    }



                    gfx.DrawLine(pen05, 150, 765, 250, 765);

                    rect = new XRect(150, 770, 250, 13);
                    gfx.DrawRectangle(XBrushes.Transparent, rect);
                    tf.DrawString("Date", A10, XBrushes.DimGray, rect, XStringFormats.TopLeft);

                    gfx.DrawLine(pen05, 300, 765, 525, 765);
                    rect = new XRect(300, 770, 250, 13);
                    gfx.DrawRectangle(XBrushes.Transparent, rect);
                    tf.DrawString("Guardian/Responsible Person", A10, XBrushes.DimGray, rect, XStringFormats.TopLeft);
                   
                }

              
                CompanyData.Dispose();
                TimeSheetData.Dispose();


                MemoryStream ms = new MemoryStream();

                document.Save((Stream)ms, false);

                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, "application/pdf", fileName);



            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }

          
         //   if (er.code != 0)
        //    {
                Response.Write(er.msg);
                Response.StatusCode = 500;
                return null;
       //     }






      
        }
    

        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> GetProviderHours(int providerId, int periodId)
        {
            ProviderHours r = new ProviderHours();
            Er er = new Er();

            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetProviderBillingHours", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@providerId", providerId);
                        cmd.Parameters.AddWithValue("@ppId", periodId);
                        cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });


                r.Periods = setPeriods(ref ds, 0);
                r.PeriodId = Convert.ToInt32(ds.Tables[1].Rows[0].ItemArray[0]);
                SetProviderBillingMatrix(ref ds, 2, ref r);
                r.Visits = SetProviderSessionData(ref ds, 2);

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
                Response.StatusCode = 500;
                return null;
            }
             
            return PartialView("Staff_Hours", r);
        }


        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> GetClientHours(int clientId, int periodId)
        {
            ClientHours r = new ClientHours();
            Er er = new Er();

            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetClientBillingHours", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@clientId", clientId);
                        cmd.Parameters.AddWithValue("@ppId", periodId);
                        cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });


                r.Periods = setPeriods(ref ds, 0);
                r.PeriodId = Convert.ToInt32(ds.Tables[1].Rows[0].ItemArray[0]);
                SetClientBillingMatrix(ref ds, 2, ref r);
                

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
                Response.StatusCode = 500;
                return null;
            }

            return PartialView("Client_Hours", r);
        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> ViewSessionLocations(string SessionType, int SessionId)
        {
            Er er = new Er();
            SessionLocation r = new SessionLocation();
            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetSessionLocations", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@SessionType", SessionType);
                        cmd.Parameters.AddWithValue("@SessionId", SessionId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                DataRow dr = ds.Tables[0].Rows[0];
                r.StartLat = (decimal)dr["startLat"];
                r.StartLon = (decimal)dr["startLon"];
                r.EndLat = (decimal)dr["endLat"];
                r.EndLon = (decimal)dr["endLon"];
                var sCoord = new GeoCoordinate((double)r.StartLat, (double)r.StartLon);
                var eCoord = new GeoCoordinate((double)r.EndLat, (double)r.EndLon);

               var meters = sCoord.GetDistanceTo(eCoord);
               if (meters > 33) // approx 100 ft
                {
                    r.StartLocation = DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString() + " " + (string)dr["startLocation"];
                    r.EndLocation = DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString() + " " + (string)dr["endLocation"];
                }
                else
                {
                    r.StartLocation = DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString() + "-" + DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString() +  " "  + (string)dr["startLocation"];
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
                Response.StatusCode = 500;
                return null;
            }

            return PartialView("Modal_SessionLocations", r);


        }




      



        [AJAXAuthorize]
        public async Task<ActionResult> ViewSessionLocationsRaw(string SessionType, int SessionId)
        {
            Er er = new Er();
            SessionLocation r = new SessionLocation();
            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetSessionLocationsRaw", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@SessionType", SessionType);
                        cmd.Parameters.AddWithValue("@SessionId", SessionId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                DataRow dr = ds.Tables[0].Rows[0];
                r.StartLat = (decimal)dr["startLat"];
                r.StartLon = (decimal)dr["startLon"];
                r.EndLat = (decimal)dr["endLat"];
                r.EndLon = (decimal)dr["endLon"];
                var sCoord = new GeoCoordinate((double)r.StartLat, (double)r.StartLon);
                var eCoord = new GeoCoordinate((double)r.EndLat, (double)r.EndLon);

                var meters = sCoord.GetDistanceTo(eCoord);
                if (meters > 33) // approx 100 ft
                {
                    r.StartLocation = DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString() + " " + (string)dr["startLocation"];
                    r.EndLocation = DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString() + " " + (string)dr["endLocation"];
                }
                else
                {
                    r.StartLocation = DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString() + "-" + DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString() + " " + (string)dr["startLocation"];
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
                Response.StatusCode = 500;
                return null;
            }

            return PartialView("Modal_SessionLocations", r);


        }
        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> GetClientBillingData(string clientId, string startEndDates)
        {
            ProviderTimeSheetData r = new ProviderTimeSheetData();
            Er er = new Er();

            string[] dates = startEndDates.Split('-');

            DateTime UTCPeriodStart = ConvertToUTC(Convert.ToDateTime(dates[0]));
            DateTime UTCPeriodEnd = ConvertToUTC(Convert.ToDateTime(dates[1]).AddDays(1));
            
            return PartialView("Task_ProviderTimeSheet", r);
        }



        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenEditObjectivesModal(int clientId, int serviceId, int isTherapy)
        {
            ObjectivesModal r = new ObjectivesModal();
            Er er = new Er();
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
                er.code = 1;
                er.msg = ex.Message;
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
                Response.StatusCode = 500;
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
      //          if (l.objectiveStatus == "Completed")
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
                 //       if (s.goalStatus == "Completed")
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
                Response.StatusCode = 500;
                return null;
            }
            else
                return PartialView("Client_Objectives", r);

        }
        private async Task<Er> FinalizeHCBSRecord(int sessionId, int providerId, int clientId, int serviceId, int clientServiceId, string svc, AdjustmentInfo adjustmentInfo)
        {
            Er er = new Er();
            var TimeZone = TimeZoneInfo.FindSystemTimeZoneById(UserClaim.timeZone);

            if (adjustmentInfo.adjUtcIn == "")
                adjustmentInfo.adjUtcIn = adjustmentInfo.utcIn;
            if (adjustmentInfo.adjUtcOut == "")
                adjustmentInfo.adjUtcOut = adjustmentInfo.utcOut;
            if (adjustmentInfo.adjDt == "")
                adjustmentInfo.adjDt = adjustmentInfo.dt;



            DateTime recordedDate;
            DateTime recordedIn;
            DateTime recordedOut;
            DateTime adjustedDate;
            DateTime adjustedIn;
            DateTime adjustedOut;



            string[] sPDt = adjustmentInfo.dt.Split('/');
            recordedDate = new DateTime(Convert.ToInt32(sPDt[2]), Convert.ToInt32(sPDt[0]), Convert.ToInt32(sPDt[1]), 0, 0, 0, DateTimeKind.Unspecified);

            string[] sPIn1 = adjustmentInfo.utcIn.Trim().Split(':');
            string[] sPIn2 = sPIn1[1].Split(' ');

            int HourIn;
            if (sPIn2[1].Trim() == "PM" && Convert.ToInt32(sPIn1[0]) != 12)
                HourIn = Convert.ToInt32(sPIn1[0]) + 12;
            else if (sPIn2[1].Trim() == "AM" && Convert.ToInt32(sPIn1[0]) == 12)
                HourIn = 0;
            else
                HourIn = Convert.ToInt32(sPIn1[0]);
            int MinuteIn = Convert.ToInt32(sPIn2[0]);
            recordedIn = new DateTime(Convert.ToInt32(sPDt[2]), Convert.ToInt32(sPDt[0]), Convert.ToInt32(sPDt[1]), HourIn, MinuteIn, 0, DateTimeKind.Unspecified);

            string[] sPOut1 = adjustmentInfo.utcOut.Trim().Split(':');
            string[] sPOut2 = sPOut1[1].Split(' ');
            int HourOut;
            if (sPOut2[1].Trim() == "PM" && Convert.ToInt32(sPOut1[0]) != 12)
                HourOut = Convert.ToInt32(sPOut1[0]) + 12;
            else if (sPOut2[1].Trim() == "AM" && Convert.ToInt32(sPOut1[0]) == 12)
                HourOut = 0;
            else
                HourOut = Convert.ToInt32(sPOut1[0]);
            int MinuteOut = Convert.ToInt32(sPOut2[0]);
            recordedOut = new DateTime(Convert.ToInt32(sPDt[2]), Convert.ToInt32(sPDt[0]), Convert.ToInt32(sPDt[1]), HourOut, MinuteOut, 0, DateTimeKind.Unspecified);
            if (recordedOut < recordedIn)
                recordedOut = recordedOut.AddDays(1);

            sPDt = adjustmentInfo.adjDt.Split('/');
            adjustedDate = new DateTime(Convert.ToInt32(sPDt[2]), Convert.ToInt32(sPDt[0]), Convert.ToInt32(sPDt[1]), 0, 0, 0, DateTimeKind.Unspecified);

            sPIn1 = adjustmentInfo.adjUtcIn.Trim().Split(':');
            sPIn2 = sPIn1[1].Split(' ');
            if (sPIn2[1].Trim() == "PM" && Convert.ToInt32(sPIn1[0]) != 12)
                HourIn = Convert.ToInt32(sPIn1[0]) + 12;
            else if (sPIn2[1].Trim() == "AM" && Convert.ToInt32(sPIn1[0]) == 12)
                HourIn = 0;
            else
                HourIn = Convert.ToInt32(sPIn1[0]);
            MinuteIn = Convert.ToInt32(sPIn2[0]);
            adjustedIn = new DateTime(Convert.ToInt32(sPDt[2]), Convert.ToInt32(sPDt[0]), Convert.ToInt32(sPDt[1]), HourIn, MinuteIn, 0, DateTimeKind.Unspecified);

            sPOut1 = adjustmentInfo.adjUtcOut.Trim().Split(':');
            sPOut2 = sPOut1[1].Split(' ');
            if (sPOut2[1].Trim() == "PM" && Convert.ToInt32(sPOut1[0]) != 12)
                HourOut = Convert.ToInt32(sPOut1[0]) + 12;
            else if (sPOut2[1].Trim() == "AM" && Convert.ToInt32(sPOut1[0]) == 12)
                HourOut = 0;
            else
                HourOut = Convert.ToInt32(sPOut1[0]);
            MinuteOut = Convert.ToInt32(sPOut2[0]);

            adjustedOut = new DateTime(Convert.ToInt32(sPDt[2]), Convert.ToInt32(sPDt[0]), Convert.ToInt32(sPDt[1]), HourOut, MinuteOut, 0, DateTimeKind.Unspecified);

            if (adjustedOut < adjustedIn)
                adjustedOut = ((DateTime)adjustedOut).AddDays(1);


            DateTime utcrecordedIn = TimeZoneInfo.ConvertTimeToUtc(recordedIn, TimeZone);
            DateTime utcadjustedIn = TimeZoneInfo.ConvertTimeToUtc(adjustedIn, TimeZone);
            DateTime utcrecordedOut = TimeZoneInfo.ConvertTimeToUtc(recordedOut, TimeZone);
            DateTime utcadjustedOut = TimeZoneInfo.ConvertTimeToUtc(adjustedOut, TimeZone);



            double inTimeChange = (utcadjustedIn - utcrecordedIn).TotalMinutes;
            double outTimeChange = (utcadjustedOut - utcrecordedOut).TotalMinutes;
            double maxAdjustmentInMinutes = 6 * 60;
            if (outTimeChange > 0)
            {
                er.code = 1;
                er.msg = "You cannot adjust your out time forward!";
            }

            else if (inTimeChange > maxAdjustmentInMinutes || inTimeChange < -maxAdjustmentInMinutes)
            {
                er.code = 1;
                er.msg = "The In time cannot be adjusted more than 6 hours!";
            }
            else
            {
                string[] sP = adjustmentInfo.clientLocationValue.Split('|');

                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_APISetHCBSAdjustments", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userPrid", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@HCBSHrsEmpId", sessionId);
                        cmd.Parameters.AddWithValue("@dtAdj", (adjustedDate != recordedDate) ? (object)adjustedDate.ToShortDateString() : DBNull.Value);
                        cmd.Parameters.AddWithValue("@adjUtcIn", (utcadjustedIn != utcrecordedIn) ? (object)utcadjustedIn : DBNull.Value);
                        cmd.Parameters.AddWithValue("@adjutcOut", (utcadjustedOut != utcrecordedOut) ? (object)utcadjustedOut : DBNull.Value);
                        cmd.Parameters.AddWithValue("@clientLocationId", sP[0]);
                        cmd.Parameters.AddWithValue("@locationTypeId", sP[1]);
                        cn.Open();
                        cmd.ExecuteNonQuery();
                        cn.Close();
                    }
                });
                InOutProcessor t = new InOutProcessor(UserClaim.conStr, UserClaim.timeZone);


                t.HCBSEmpHrsId = sessionId;



                if (recordedDate < adjustedDate)
                {
                    t.utcPartition1Start = TimeZoneInfo.ConvertTimeToUtc(recordedDate, TimeZone);
                    t.utcPartition1End = t.utcPartition1Start.AddDays(1);
                    t.utcPartition2Start = t.utcPartition1End;
                    t.utcPartition2End = t.utcPartition1Start.AddDays(2);
                    t.date1 = Convert.ToDateTime(recordedDate).ToShortDateString();
                    t.date2 = Convert.ToDateTime(recordedDate).AddDays(1).ToShortDateString();
                }
                else
                {
                    t.utcPartition1Start = TimeZoneInfo.ConvertTimeToUtc(adjustedDate, TimeZone);
                    t.utcPartition1End = t.utcPartition1Start.AddDays(1);
                    t.utcPartition2Start = t.utcPartition1End;
                    t.utcPartition2End = t.utcPartition1Start.AddDays(2);
                    t.date1 = Convert.ToDateTime(adjustedDate).ToShortDateString();
                    t.date2 = Convert.ToDateTime(adjustedDate).AddDays(1).ToShortDateString();

                }

                t.utcIn = utcrecordedIn;
                t.utcOut = utcrecordedOut;
                t.adjutcIn = (utcrecordedIn == utcadjustedIn) ? null : (DateTime?)utcadjustedIn;
                t.adjutcOut = (utcrecordedOut == utcadjustedOut) ? null : (DateTime?)utcadjustedOut;
                t.prId = providerId;
                t.clsvId = clientId;
                t.clsvidId = clientServiceId;
                t.serviceId = serviceId;
                t.svc = svc;



                t.checkHCBSRecordIsValid(ref er);
                if (er.code == 0)
                {
                    if (er.code == 0)
                    {
                       if (UserClaim.userLevel == "Provider")
                            t.checkHCBSRecordAgainstAuths(ref er);
                        if (er.code == 0)
                        {
                            t.updateHCBSRecord2(ref er);
                        }
                    }
                }
            }
            return er;
        }


        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> GetRspNote(int docId)
        {
            ClientNote r = new ClientNote();


            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskGetSessionRSPNote", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@staffSessionHcbsId", docId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                DataRow dr = ds.Tables[0].Rows[0];
                r.signee = UserClaim.staffname;
                r.signeeCredentials = UserClaim.stafftitle + (UserClaim.staffnpi != "" ? " (NPI: " + UserClaim.staffnpi + ")" : "");

                r.clientName = (string)dr["cnm"];
                r.svc = (string)dr["svc"];
                r.note = dr["note"] == DBNull.Value ? "" : (string)dr["note"];
                r.dt = ((DateTime)dr["dt"]).ToShortDateString();
              
                r.docId = (int)dr["staffSessionHcbsId"];
                r.noShow = (bool)dr["noShow"];
                r.hasAttachment = (bool)dr["hasAttachment"];

                r.attachmentName = (string)dr["attachmentName"].ToString();
                r.extension = (string)dr["fileExtension"];
                r.noShow = (bool)dr["noShow"];
                r.attachmentName = dr["attachmentName"].ToString();

                r.providerId = (int)dr["providerId"];
                r.clientId = (int)dr["clsvId"];
                r.serviceId = (int)dr["serviceId"];
                r.clientServiceId = (int)dr["clientServiceId"];

                // added for EVV notes completed on web
                r.isEVV = (bool)dr["isEVV"];
                r.clientLocationValue = dr["clientLocationId"] + "|" + dr["LocationTypeId"] + "|" + dr["lat"] + "|" + dr["lon"];
                r.inTime = DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString();
                r.outTime = DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString();
                r.adjDt = dr["dtAdj"] == DBNull.Value ? ((DateTime)dr["dt"]).ToShortDateString() : ((DateTime)dr["dtAdj"]).ToShortDateString();

                r.adjInTime = dr["adjUtcIn"] != DBNull.Value ? DateTimeLocal((DateTime)dr["adjUtcIn"]).ToShortTimeString() : DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString();
                r.adjOutTime = dr["adjUtcOut"] != DBNull.Value ? DateTimeLocal((DateTime)dr["adjUtcOut"]).ToShortTimeString() : DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString();
                r.isoDate = ((DateTime)dr["dt"]).ToString("yyyy-MM-dd");
                r.location = dr["location"] != DBNull.Value ? dr["location"].ToString() : "";
                r.locations = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {

                    name = (string)spR["Location"],
                    value = spR["clientLocationId"] + "|" + spR["locationTypeId"] + "|" + spR["lat"] + "|" + spR["lon"]
                }).ToList();

                if ((int)dr["startClientLocationId"] == 0 || (int)dr["endClientLocationId"] == 0)
                    // no firm start or end location
                    r.locationDetermined = false;
                   
                else
                    r.locationDetermined = true;
                // end added for EVV notes completed on web


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
                Response.StatusCode = 500;
                return null;
            }

            return PartialView("ModalSessionRspNote", r);
        }

   

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SetRspNote(IEnumerable<HttpPostedFileBase> files, string _sessionNote)
        {
            HomeStaffPage r = new HomeStaffPage();
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();
            SessioNote sNote = JsonConvert.DeserializeObject<SessioNote>(_sessionNote);

            w.er = await FinalizeHCBSRecord(sNote.docId, sNote.providerId, sNote.clientId, sNote.serviceId, sNote.clientServiceId, sNote.svc, sNote.adjustmentInfo);

            sNote.providerId = UserClaim.prid;
            DataSet ds = new DataSet();
            bool hasAttachment = false;
            string fileExtension = "";
            string fileName = null;

            if (w.er.code == 0)
            {


                if (files != null)
                {
                    var file = files.FirstOrDefault();
                    // Verify that the user selected a file
                    if (file != null && file.ContentLength > 0)
                    {
                        fileExtension = Path.GetExtension(file.FileName);
                        fileName = sNote.attachmentName;

                        byte[] data = new byte[file.InputStream.Length];
                        file.InputStream.Read(data, 0, data.Length);
                        try
                        {
                            FileData f = new FileData("attachments", UserClaim.blobStorage);
                            f.StoreFile(data, fileName + fileExtension);
                            hasAttachment = true;
                        }
                        catch (Exception ex)
                        {
                            w.er.code = 1;
                            w.er.msg = ex.Message;
                        }
                    }
                }
            }

            if (w.er.code == 0)
            {
               
                try
                {
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskSetSessionRspNote", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@prId", sNote.providerId);
                            cmd.Parameters.AddWithValue("@staffSessionHcbsId", sNote.docId);
                            cmd.Parameters.AddWithValue("@noShow", sNote.noShow);
                            cmd.Parameters.AddWithValue("@note", sNote.note);
                            cmd.Parameters.AddWithValue("@hasAttachment", hasAttachment);
                            cmd.Parameters.AddWithValue("@fileExtension", fileExtension);
                            cn.Open();
                            cmd.ExecuteNonQuery();
                            cn.Close();
                        }
                    });

                    string fileName2 = await saveRspNoteAsFile(sNote.docId);
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskSetSessionRspNoteApprove", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@prId", sNote.providerId);
                            cmd.Parameters.AddWithValue("@staffSessionHcbsId", sNote.docId);
                            cmd.Parameters.AddWithValue("@fileName", fileName2);
                           cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    });
                    r.pendingDocumentation = setPendingDocumentation(ref ds, 0);
                    r.staffAlerts = setStaffAlerts(ref ds, 1);
                    w.pendingDocumentation = RenderRazorViewToString("PendingDocumentation", r);
                    w.staffAlerts = RenderRazorViewToString("StaffAlerts", r.staffAlerts);

                }
                catch (Exception ex)
                {
                    w.er.code = 1;
                    w.er.msg = ex.Message;
                }
            }
            ds.Dispose();

            var jsonResult = Json(w);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;


        }
        private async Task<string> saveRspNoteAsFile(int docId)
        {
            string fileName = "";
            ClientNotePdf rpr = new ClientNotePdf();

            await Task.Run(() =>
            {
                getRSPNotePdf(Convert.ToString(docId), ref rpr);
            });

            var docStr = docId.ToString();
            fileName = "HCBS_" + ("0000000000000000".Substring(0, 16 - docStr.Length) + docStr) + ".pdf";

            PdfGenerateConfig config = new PdfGenerateConfig();
            config.PageSize = PageSize.Letter;
            config.MarginBottom = 90;
            config.MarginTop = 40;
            config.MarginRight = 30;
            config.MarginLeft = 30;
            config.PageOrientation = PageOrientation.Portrait;
            PdfDocument pdf = PdfGenerator.GeneratePdf(RenderRazorViewToString("Document_NoteRsp", rpr), config);
            XFont A8 = new XFont("Arial", 8);
            XRect rect = new XRect(20, 31, 100, 13);
            for (int i = 0; i < pdf.PageCount; i++)
            {
                string footer = rpr.clientName + " (" + rpr.dt + ") Page " + (i + 1) + " of " + pdf.PageCount;
                PdfPage page = pdf.Pages[i];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XTextFormatter tf = new XTextFormatter(gfx);
                rect.X = 200;
                rect.Y = 750;
                rect.Width = 300;
                rect.Height = 10;
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(footer, A8, XBrushes.DarkSlateGray, rect, XStringFormats.TopLeft);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                pdf.Save(ms, false);
                ms.Seek(0, SeekOrigin.Begin);

                FileData f = new FileData("sessionnotes", UserClaim.blobStorage);
                f.StoreFile(ms.ToArray(), fileName);
            }
            return fileName;
        }

        private void getRSPNotePdf(string docId, ref ClientNotePdf r)
        {
            DataSet ds = new DataSet();

            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_TaskGetSessionRspNotePdf", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@StaffSessionHcbsId", docId);
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
            }
            DataRow dr = ds.Tables[0].Rows[0];
            r.completedBy = dr["completedByFn"] + " " + dr["completedByLn"];
            r.completedByCredentials = dr["completedByTitle"] + ((string)dr["completedByNpi"] != "" ? " (NPI: " + dr["completedByNpi"] + ")" : "");

            r.timeOfService = DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString() + " - " + DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString();
            r.agency = UserClaim.companyName;
            r.npi = UserClaim.npi;
            r.clientName = (string)dr["cnm"];
            r.svc = (string)dr["svc"];
            r.serviceName = (string)dr["serviceName"];
            r.dt = ((DateTime)dr["dt"]).ToShortDateString();

            r.note = dr["note"] == DBNull.Value ? "" : ConvertToHtml((string)dr["note"]);
            r.noShow = (bool)dr["noShow"];
            // XXXX New statuses to be used with note
            r.designeeUnableToSign = (bool)dr["designeeUnableToSign"];
            r.designeeRefusedToSign = (bool)dr["designeeRefusedToSign"];
            r.clientRefusedService = (bool)dr["clientRefusedService"];
            r.unsafeToWork = (bool)dr["unsafeToWork"];
            r.guardianId = (int)dr["guardianId"];
            r.designeeId = (int)dr["designeeId"];
            r.IsEVV = (bool)dr["IsEvv"];
            // XXXX end new Stuff
            r.dob = dr["dob"] == DBNull.Value ? "" : ((DateTime)dr["dob"]).ToShortDateString();
            r.clId = dr["clId"] == DBNull.Value ? "" : (string)dr["clId"];
            r.clientWorker = dr["clwNm"] == DBNull.Value ? "" : (string)dr["clwNm"];

            ds.Dispose();
        }

        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> GetHahNote(string docId)
        {
            ClientNote r = new ClientNote();
            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskGetSessionHabilitationNote", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@staffSessionHcbsId", docId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                DataRow dr = ds.Tables[0].Rows[0];

                r.signee = UserClaim.staffname;
                r.signeeCredentials = UserClaim.stafftitle + (UserClaim.staffnpi != "" ? " (NPI: " + UserClaim.staffnpi + ")" : "");

                r.clientName = (string)dr["cnm"];
                r.svc = (string)dr["svc"];
                r.note = dr["note"] == DBNull.Value ? "" : (string)dr["note"];
                r.dt = ((DateTime)dr["dt"]).ToShortDateString();

                r.docId = (int)dr["staffSessionHcbsId"];
                r.noShow = (bool)dr["noShow"];
                r.hasAttachment = (bool)dr["hasAttachment"];

                r.attachmentName = (string)dr["attachmentName"].ToString();
                r.extension = (string)dr["fileExtension"];

                DataView dv = new DataView(ds.Tables[1]);

                r.longTermObjectives = dv.ToTable(true, "objectiveId", "longTermVision", "longTermGoal").Rows.Cast<DataRow>().Select(spR => new LongTermObjective()
                {
                    objectiveId = (int)spR["objectiveId"],
                    longTermVision = (string)spR["longTermVision"],
                    longTermGoal = (string)spR["longTermGoal"],
                }).ToList();

                foreach (LongTermObjective o in r.longTermObjectives)
                {
                    dv.RowFilter = "objectiveId=" + o.objectiveId;
                    o.shortTermGoals = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new ShortTermGoal()
                    {
                        goalId = (int)spR["goalId"],
                        shortTermGoal = (string)spR["shortTermGoal"],
                        teachingMethod = (string)spR["teachingMethod"],
                        score = (string)spR["score"],
                        trialPct = spR["trialPct"] == DBNull.Value || (string)spR["trialPct"] == "" ? "0" : (string)spR["trialPct"]
                    }).ToList();
                }

                r.scoring = ds.Tables[2].Rows.Cast<DataRow>().Select(spR => new Scoring()
                {
                    value = (string)spR["scoreValue"],
                    name = (string)spR["scoreName"],

                }).ToList();

                r.providerId = (int)dr["providerId"];
                r.clientId = (int)dr["clsvId"];
                r.serviceId = (int)dr["serviceId"];
                r.clientServiceId = (int)dr["clientServiceId"];
                // added for EVV notes completed on web
                r.isEVV = (bool)dr["isEVV"];
                r.clientLocationValue = dr["clientLocationId"] + "|" + dr["LocationTypeId"] + "|" + dr["lat"] + "|" + dr["lon"];
                r.inTime = DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString();
                r.outTime = DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString();
                r.adjDt = dr["dtAdj"] == DBNull.Value ? ((DateTime)dr["dt"]).ToShortDateString() : ((DateTime)dr["dtAdj"]).ToShortDateString();
                r.adjInTime = dr["adjUtcIn"] != DBNull.Value ? DateTimeLocal((DateTime)dr["adjUtcIn"]).ToShortTimeString() : DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString();
                r.adjOutTime = dr["adjUtcOut"] != DBNull.Value ? DateTimeLocal((DateTime)dr["adjUtcOut"]).ToShortTimeString() : DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString();
                r.isoDate = ((DateTime)dr["dt"]).ToString("yyyy-MM-dd");
                r.location = dr["location"] != DBNull.Value ? dr["location"].ToString() : "";
                r.locations = ds.Tables[3].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {

                    name = (string)spR["Location"],
                    value = spR["clientLocationId"] + "|" + spR["locationTypeId"] + "|" + spR["lat"] + "|" + spR["lon"]
                }).ToList();

                if ((int)dr["startClientLocationId"] == 0 || (int)dr["endClientLocationId"] == 0)
                    // no firm start or end location
                    r.locationDetermined = false;

                else
                    r.locationDetermined = true;
                // end added for EVV notes completed on web



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
                Response.StatusCode = 500;
                return null;
            }
            return PartialView("ModalSessionHabilitationNote", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SetHahNote(IEnumerable<HttpPostedFileBase> files, string _sessionNote)
        {
            HomeStaffPage r = new HomeStaffPage();
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();
            SessioNote sNote = JsonConvert.DeserializeObject<SessioNote>(_sessionNote);

            w.er = await FinalizeHCBSRecord(sNote.docId, sNote.providerId, sNote.clientId, sNote.serviceId, sNote.clientServiceId, sNote.svc, sNote.adjustmentInfo);

            sNote.providerId = UserClaim.prid;
            DataSet ds = new DataSet();
            bool hasAttachment = false;
            string fileExtension = "";
            string fileName = null;

            if (w.er.code == 0)
            {

                if (files != null)
                {
                    var file = files.FirstOrDefault();
                    // Verify that the user selected a file
                    if (file != null && file.ContentLength > 0)
                    {
                        fileExtension = Path.GetExtension(file.FileName);
                        fileName = Convert.ToString(sNote.attachmentName);
                        byte[] data = new byte[file.InputStream.Length];
                        file.InputStream.Read(data, 0, data.Length);
                        try
                        {
                            FileData f = new FileData("attachments", UserClaim.blobStorage);
                            f.StoreFile(data, fileName + fileExtension);
                            hasAttachment = true;
                        }
                        catch (Exception ex)
                        {
                            w.er.code = 1;
                            w.er.msg = ex.Message;
                        }
                    }
                }
            }
            if (w.er.code == 0)
            {
                try
                {
                    DataTable dt = new DataTable();
                    dt.Clear();
                    dt.Columns.Add("goalId");
                    dt.Columns.Add("score");
                    dt.Columns.Add("trialPct");

                    for (int i = 0; i < sNote.sessionScores.Count; i++)
                    {
                        if (sNote.sessionScores[i].score != "NA")
                        {
                            DataRow nRow = dt.NewRow();
                            nRow["goalId"] = sNote.sessionScores[i].goalId;
                            nRow["score"] = sNote.sessionScores[i].score;
                            nRow["trialPct"] = sNote.sessionScores[i].trialPct;
                            dt.Rows.Add(nRow);
                        }
                    }

                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskSetSessionHabilitationNote", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@prId", sNote.providerId);
                            cmd.Parameters.AddWithValue("@staffSessionHcbsId", sNote.docId);
                            cmd.Parameters.AddWithValue("@goalScores", dt);
                            cmd.Parameters.AddWithValue("@note", sNote.note);
                            cmd.Parameters.AddWithValue("@noShow", sNote.noShow);
                            cmd.Parameters.AddWithValue("@hasAttachment", hasAttachment);
                            cmd.Parameters.AddWithValue("@fileExtension", fileExtension);
                            cn.Open();
                            cmd.ExecuteNonQuery();
                            cn.Close();
                        }
                    });

                    string fileName2 = await saveHahNoteAsFile(sNote.docId);
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskSetSessionHabilitationNoteApprove", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@prId", sNote.providerId);
                            cmd.Parameters.AddWithValue("@staffSessionHcbsId", sNote.docId);
                            cmd.Parameters.AddWithValue("@fileName", fileName2);
                            cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    });
                    r.pendingDocumentation = setPendingDocumentation(ref ds, 0);
                    r.staffAlerts = setStaffAlerts(ref ds, 1);
                    w.pendingDocumentation = RenderRazorViewToString("PendingDocumentation", r);
                    w.staffAlerts = RenderRazorViewToString("StaffAlerts", r.staffAlerts);
                }

                catch (Exception ex)
                {
                    w.er.code = 1;
                    w.er.msg = ex.Message;
                }
            }
            ds.Dispose();

            var jsonResult = Json(w);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        private async Task<string> saveHahNoteAsFile(int docId)
        {
            string fileName = "";
            ClientNotePdf rpr = new ClientNotePdf();

            await Task.Run(() =>
            {
                getHahNotePdf(Convert.ToString(docId), ref rpr);
            });

            var docStr = docId.ToString();
            fileName = "HCBS_" + ("0000000000000000".Substring(0, 16 - docStr.Length) + docStr) + ".pdf";

            PdfGenerateConfig config = new PdfGenerateConfig();
            config.PageSize = PageSize.Letter;
            config.MarginBottom = 90;
            config.MarginTop = 40;
            config.MarginRight = 30;
            config.MarginLeft = 30;
            config.PageOrientation = PageOrientation.Portrait;
            PdfDocument pdf = PdfGenerator.GeneratePdf(RenderRazorViewToString("Document_NoteHabilitation", rpr), config);
            XFont A8 = new XFont("Arial", 8);
            XRect rect = new XRect(20, 31, 100, 13);
            for (int i = 0; i < pdf.PageCount; i++)
            {
                string footer = rpr.clientName + " (" + rpr.dt + ") Page " + (i + 1) + " of " + pdf.PageCount;
                PdfPage page = pdf.Pages[i];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XTextFormatter tf = new XTextFormatter(gfx);
                rect.X = 200;
                rect.Y = 750;
                rect.Width = 300;
                rect.Height = 10;
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(footer, A8, XBrushes.DarkSlateGray, rect, XStringFormats.TopLeft);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                pdf.Save(ms, false);
                ms.Seek(0, SeekOrigin.Begin);

                FileData f = new FileData("sessionnotes", UserClaim.blobStorage);
                f.StoreFile(ms.ToArray(), fileName);
            }
            return fileName;
        }

        private void getHahNotePdf(string docId, ref ClientNotePdf r)
        {
            DataSet ds = new DataSet();

            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_TaskGetSessionHabilitationNotePdf", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@StaffSessionHcbsId", docId);
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
            }
            DataRow dr = ds.Tables[0].Rows[0];
            r.completedBy = dr["completedByFn"] + " " + dr["completedByLn"];
            r.completedByCredentials = dr["completedByTitle"] + ((string)dr["completedByNpi"] != "" ? " (NPI: " + dr["completedByNpi"] + ")" : "");

            r.timeOfService = DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString() + " - " + DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString();
            r.agency = UserClaim.companyName;
            r.npi = UserClaim.npi;
            r.clientName = (string)dr["cnm"];
            r.svc = (string)dr["svc"];
            r.serviceName = (string)dr["serviceName"];
            r.dt = ((DateTime)dr["dt"]).ToShortDateString();

            r.note = dr["note"] == DBNull.Value ? "" : ConvertToHtml((string)dr["note"]);
            r.noShow = (bool)dr["noShow"];
            // XXXX New statuses to be used with note
            r.designeeUnableToSign = (bool)dr["designeeUnableToSign"];
            r.designeeRefusedToSign = (bool)dr["designeeRefusedToSign"];
            r.clientRefusedService = (bool)dr["clientRefusedService"];
            r.unsafeToWork = (bool)dr["unsafeToWork"];
            r.guardianId = (int)dr["guardianId"];
            r.designeeId = (int)dr["designeeId"];
            r.IsEVV = (bool)dr["IsEvv"];
            // XXXX end new Stuff
            r.dob = dr["dob"] == DBNull.Value ? "" : ((DateTime)dr["dob"]).ToShortDateString();
            r.clId = dr["clId"] == DBNull.Value ? "" : (string)dr["clId"];
            r.clientWorker = dr["clwNm"] == DBNull.Value ? "" : (string)dr["clwNm"];
            DataView dv = new DataView(ds.Tables[1]);

            r.longTermObjectives = dv.ToTable(true, "objectiveId", "longTermVision", "longTermGoal").Rows.Cast<DataRow>().Select(spR => new LongTermObjective()
            {
                objectiveId = (int)spR["objectiveId"],
                longTermVision = ConvertToHtml((string)spR["longTermVision"]),
                longTermGoal = ConvertToHtml((string)spR["longTermGoal"]),
            }).ToList();

            foreach (LongTermObjective o in r.longTermObjectives)
            {
                dv.RowFilter = "objectiveId=" + o.objectiveId;
                o.shortTermGoals = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new ShortTermGoal()
                {
                    goalId = (int)spR["goalId"],
                    shortTermGoal = ConvertToHtml((string)spR["shortTermGoal"]),
                    teachingMethod = ConvertToHtml((string)spR["teachingMethod"]),
                    score = (string)spR["score"],
                    trialPct = spR["trialPct"] == DBNull.Value || (string)spR["trialPct"] == "" ? "0" : (string)spR["trialPct"]

                }).ToList();
            }

            ds.Dispose();
        }

        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> GetAtcNote(string docId)
        {
            ClientNote r = new ClientNote();
            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskGetSessionAtcNote", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@staffSessionHcbsId", docId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                DataRow dr = ds.Tables[0].Rows[0];

                r.signee = UserClaim.staffname;
                r.signeeCredentials = UserClaim.stafftitle + (UserClaim.staffnpi != "" ? " (NPI: " + UserClaim.staffnpi + ")" : "");

                r.clientName = (string)dr["cnm"];
                r.svc = (string)dr["svc"];
                r.note = dr["note"] == DBNull.Value ? "" : (string)dr["note"];
                r.dt = ((DateTime)dr["dt"]).ToShortDateString();

                r.docId = (int)dr["staffSessionHcbsId"];
                r.noShow = (bool)dr["noShow"];
                r.hasAttachment = (bool)dr["hasAttachment"];

                r.attachmentName = (string)dr["attachmentName"].ToString();
                r.extension = (string)dr["fileExtension"];
                r.careAreas = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new CareArea()
                {
                    careId = (int)spR["careId"],
                    careArea = (string)spR["careArea"],
                    score = spR["score"] == DBNull.Value ? "" : (string)spR["score"],
                    lastDate = spR["lastDate"] == DBNull.Value ? "Never" : ((DateTime)spR["lastDate"]).ToShortDateString()

                }).ToList();

                r.scoring = ds.Tables[2].Rows.Cast<DataRow>().Select(spR => new Scoring()
                {
                    value = (string)spR["scoreValue"],
                    name = (string)spR["scoreName"],

                }).ToList();

                r.providerId = (int)dr["providerId"];
                r.clientId = (int)dr["clsvId"];
                r.serviceId = (int)dr["serviceId"];
                r.clientServiceId = (int)dr["clientServiceId"];
                // added for EVV notes completed on web
                r.isEVV = (bool)dr["isEVV"];
                r.clientLocationValue = dr["clientLocationId"] + "|" + dr["LocationTypeId"] + "|" + dr["lat"] + "|" + dr["lon"];
                r.inTime = DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString();
                r.outTime = DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString();
                r.adjDt = dr["dtAdj"] == DBNull.Value ? ((DateTime)dr["dt"]).ToShortDateString() : ((DateTime)dr["dtAdj"]).ToShortDateString();

                r.adjInTime = dr["adjUtcIn"] != DBNull.Value ? DateTimeLocal((DateTime)dr["adjUtcIn"]).ToShortTimeString() : DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString();
                r.adjOutTime = dr["adjUtcOut"] != DBNull.Value ? DateTimeLocal((DateTime)dr["adjUtcOut"]).ToShortTimeString() : DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString();
                r.isoDate = ((DateTime)dr["dt"]).ToString("yyyy-MM-dd");
                r.location = dr["location"] != DBNull.Value ? dr["location"].ToString() : "";
                r.locations = ds.Tables[3].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {

                    name = (string)spR["Location"],
                    value = spR["clientLocationId"] + "|" + spR["locationTypeId"] + "|" + spR["lat"] + "|" + spR["lon"]
                }).ToList();

                if ((int)dr["startClientLocationId"] == 0 || (int)dr["endClientLocationId"] == 0)
                    // no firm start or end location
                    r.locationDetermined = false;

                else
                    r.locationDetermined = true;
                // end added for EVV notes completed on web
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 500;
                return null;
            }

            return PartialView("ModalSessionAtcNote", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SetAtcNote(IEnumerable<HttpPostedFileBase> files, string _sessionNote)
        {
            HomeStaffPage r = new HomeStaffPage();
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();
            SessioNote sNote = JsonConvert.DeserializeObject<SessioNote>(_sessionNote);

            w.er = await FinalizeHCBSRecord(sNote.docId, sNote.providerId, sNote.clientId, sNote.serviceId, sNote.clientServiceId, sNote.svc, sNote.adjustmentInfo);

            sNote.providerId = UserClaim.prid;
            DataSet ds = new DataSet();
            bool hasAttachment = false;
            string fileExtension = "";
            string fileName = null;

            if (w.er.code == 0)
            {
                if (files != null)
                {
                    var file = files.FirstOrDefault();
                    // Verify that the user selected a file
                    if (file != null && file.ContentLength > 0)
                    {
                        fileExtension = Path.GetExtension(file.FileName);
                        fileName = sNote.attachmentName;
                        byte[] data = new byte[file.InputStream.Length];
                        file.InputStream.Read(data, 0, data.Length);
                        try
                        {
                            FileData f = new FileData("attachments", UserClaim.blobStorage);
                            f.StoreFile(data, fileName + fileExtension);
                            hasAttachment = true;
                        }
                        catch (Exception ex)
                        {
                            w.er.code = 1;
                            w.er.msg = ex.Message;
                        }
                    }
                }
            }
       
            if (w.er.code == 0)
            {
                try
                {
                    DataTable dt = new DataTable();
                    dt.Clear();
                    dt.Columns.Add("goalId");
                    dt.Columns.Add("score");

                    for (int i = 0; i < sNote.sessionScores.Count; i++)
                    {
                        if (sNote.sessionScores[i].score != "NA")
                        {
                            DataRow nRow = dt.NewRow();
                            nRow["goalId"] = sNote.sessionScores[i].goalId;
                            nRow["score"] = sNote.sessionScores[i].score;
                            dt.Rows.Add(nRow);

                        }


                    }
                    
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskSetSessionAtcNote", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@prId", sNote.providerId);
                            cmd.Parameters.AddWithValue("@note", sNote.note);
                            cmd.Parameters.AddWithValue("@supervisorPresent", sNote.supervisorPresent);
                            cmd.Parameters.AddWithValue("@noShow", sNote.noShow);
                            cmd.Parameters.AddWithValue("@staffSessionHcbsId", sNote.docId);
                            cmd.Parameters.AddWithValue("@careAreas", dt);
                            cmd.Parameters.AddWithValue("@hasAttachment", hasAttachment);
                            cmd.Parameters.AddWithValue("@fileExtension", fileExtension);
                            cn.Open();
                            cmd.ExecuteNonQuery();
                            cn.Close();
                        }
                    });
                   
                    string fileName2 = await saveAtcNoteAsFile(sNote.docId);

                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskSetSessionAtcNoteApprove", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@prId", sNote.providerId);
                            cmd.Parameters.AddWithValue("@staffSessionHcbsId", sNote.docId);
                            cmd.Parameters.AddWithValue("@fileName", fileName2);
                            cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    });

                    r.pendingDocumentation = setPendingDocumentation(ref ds, 0);
                    r.staffAlerts = setStaffAlerts(ref ds, 1);
                    w.pendingDocumentation = RenderRazorViewToString("PendingDocumentation", r);
                    w.staffAlerts = RenderRazorViewToString("StaffAlerts", r.staffAlerts);
                    
                }
                catch (Exception ex)
                {
                    w.er.code = 1;
                    w.er.msg = ex.Message;
                }

            }
            ds.Dispose();

            var jsonResult = Json(w);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        private async Task<string> saveAtcNoteAsFile(int docId)
        {
            string fileName = "";
            ClientNotePdf rpr = new ClientNotePdf();

            await Task.Run(() =>
            {
                getAtcNotePdf(Convert.ToString(docId), ref rpr);
            });

            var docStr = docId.ToString();
            fileName = "HCBS_" + ("0000000000000000".Substring(0, 16 - docStr.Length) + docStr) + ".pdf";

            PdfGenerateConfig config = new PdfGenerateConfig();
            config.PageSize = PageSize.Letter;
            config.MarginBottom = 90;
            config.MarginTop = 40;
            config.MarginRight = 30;
            config.MarginLeft = 30;
            config.PageOrientation = PageOrientation.Portrait;
            PdfDocument pdf = PdfGenerator.GeneratePdf(RenderRazorViewToString("Document_NoteAtc", rpr), config);
            XFont A8 = new XFont("Arial", 8);
            XRect rect = new XRect(20, 31, 100, 13);
            for (int i = 0; i < pdf.PageCount; i++)
            {
                string footer = rpr.clientName + " (" + rpr.dt + ") Page " + (i + 1) + " of " + pdf.PageCount;
                PdfPage page = pdf.Pages[i];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XTextFormatter tf = new XTextFormatter(gfx);
                rect.X = 200;
                rect.Y = 750;
                rect.Width = 300;
                rect.Height = 10;
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(footer, A8, XBrushes.DarkSlateGray, rect, XStringFormats.TopLeft);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                pdf.Save(ms, false);
                ms.Seek(0, SeekOrigin.Begin);

                FileData f = new FileData("sessionnotes", UserClaim.blobStorage);
                f.StoreFile(ms.ToArray(), fileName);
            }

            return fileName;
        }

        private void getAtcNotePdf(string docId, ref ClientNotePdf r)
        {
            DataSet ds = new DataSet();

            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_TaskGetSessionAtcNotePdf", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@staffSessionHcbsId", docId);
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
            }
            DataRow dr = ds.Tables[0].Rows[0];
            r.completedBy = dr["completedByFn"] + " " + dr["completedByLn"];
            r.completedByCredentials = dr["completedByTitle"] + ((string)dr["completedByNpi"] != "" ? " (NPI: " + dr["completedByNpi"] + ")" : "");

            r.timeOfService = DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString() + " - " + DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString();
            r.agency = UserClaim.companyName;
            r.npi = UserClaim.npi;
            r.clientName = (string)dr["cnm"];
            r.svc = (string)dr["svc"];
            r.serviceName = (string)dr["serviceName"];
            r.dt = ((DateTime)dr["dt"]).ToShortDateString();

            r.note = ConvertToHtml((string)dr["note"]);
            r.noShow = (bool)dr["noShow"];
            // XXXX New statuses to be used with note
            r.designeeUnableToSign = (bool)dr["designeeUnableToSign"];
            r.designeeRefusedToSign = (bool)dr["designeeRefusedToSign"];
            r.clientRefusedService = (bool)dr["clientRefusedService"];
            r.unsafeToWork = (bool)dr["unsafeToWork"];
            r.guardianId = (int)dr["guardianId"];
            r.designeeId = (int)dr["designeeId"];
            r.IsEVV = (bool)dr["IsEvv"];
            // XXXX end new Stuff
            r.supervisorPresent = (bool)dr["supervisorPresent"];
            r.dob = dr["dob"] == DBNull.Value ? "" : ((DateTime)dr["dob"]).ToShortDateString();
            r.clId = dr["clId"] == DBNull.Value ? "" : (string)dr["clId"];
            r.clientWorker = dr["clwNm"] == DBNull.Value ? "" : (string)dr["clwNm"];


            r.careAreas = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new CareArea()
            {
                careId = (int)spR["careId"],
                careArea = (string)spR["careArea"],
                score = (string)spR["scoreName"]
            }).ToList();



            ds.Dispose();
        }



      
        private Er FinalizeTherapyRecord(int sessionId, int providerId, int clientId, int serviceId, int clientServiceId, string svc, AdjustmentInfo adjustmentInfo)
        {
            Er er = new Er();
            var TimeZone = TimeZoneInfo.FindSystemTimeZoneById(UserClaim.timeZone);

            if (adjustmentInfo.adjUtcIn == "")
                adjustmentInfo.adjUtcIn = adjustmentInfo.utcIn;
            if (adjustmentInfo.adjUtcOut == "")
                adjustmentInfo.adjUtcOut = adjustmentInfo.utcOut;
            if (adjustmentInfo.adjDt == "")
                adjustmentInfo.adjDt = adjustmentInfo.dt;



            DateTime recordedDate;
            DateTime recordedIn;
            DateTime recordedOut;
            DateTime adjustedDate;
            DateTime adjustedIn;
            DateTime adjustedOut;



            string[] sPDt = adjustmentInfo.dt.Split('/');
            recordedDate = new DateTime(Convert.ToInt32(sPDt[2]), Convert.ToInt32(sPDt[0]), Convert.ToInt32(sPDt[1]), 0, 0, 0, DateTimeKind.Unspecified);

            string[] sPIn1 = adjustmentInfo.utcIn.Trim().Split(':');
            string[] sPIn2 = sPIn1[1].Split(' ');

            int HourIn;
            if (sPIn2[1].Trim() == "PM" && Convert.ToInt32(sPIn1[0]) != 12)
                HourIn = Convert.ToInt32(sPIn1[0]) + 12;
            else if (sPIn2[1].Trim() == "AM" && Convert.ToInt32(sPIn1[0]) == 12)
                HourIn = 0;
            else
                HourIn = Convert.ToInt32(sPIn1[0]);
            int MinuteIn = Convert.ToInt32(sPIn2[0]);
            recordedIn = new DateTime(Convert.ToInt32(sPDt[2]), Convert.ToInt32(sPDt[0]), Convert.ToInt32(sPDt[1]), HourIn, MinuteIn, 0, DateTimeKind.Unspecified);

            string[] sPOut1 = adjustmentInfo.utcOut.Trim().Split(':');
            string[] sPOut2 = sPOut1[1].Split(' ');
            int HourOut;
            if (sPOut2[1].Trim() == "PM" && Convert.ToInt32(sPOut1[0]) != 12)
                HourOut = Convert.ToInt32(sPOut1[0]) + 12;
            else if (sPOut2[1].Trim() == "AM" && Convert.ToInt32(sPOut1[0]) == 12)
                HourOut = 0;
            else
                HourOut = Convert.ToInt32(sPOut1[0]);
            int MinuteOut = Convert.ToInt32(sPOut2[0]);
            recordedOut = new DateTime(Convert.ToInt32(sPDt[2]), Convert.ToInt32(sPDt[0]), Convert.ToInt32(sPDt[1]), HourOut, MinuteOut, 0, DateTimeKind.Unspecified);
            if (recordedOut < recordedIn)
                recordedOut = recordedOut.AddDays(1);

            sPDt = adjustmentInfo.adjDt.Split('/');
            adjustedDate = new DateTime(Convert.ToInt32(sPDt[2]), Convert.ToInt32(sPDt[0]), Convert.ToInt32(sPDt[1]), 0, 0, 0, DateTimeKind.Unspecified);

            sPIn1 = adjustmentInfo.adjUtcIn.Trim().Split(':');
            sPIn2 = sPIn1[1].Split(' ');
            if (sPIn2[1].Trim() == "PM" && Convert.ToInt32(sPIn1[0]) != 12)
                HourIn = Convert.ToInt32(sPIn1[0]) + 12;
            else if (sPIn2[1].Trim() == "AM" && Convert.ToInt32(sPIn1[0]) == 12)
                HourIn = 0;
            else
                HourIn = Convert.ToInt32(sPIn1[0]);
            MinuteIn = Convert.ToInt32(sPIn2[0]);
            adjustedIn = new DateTime(Convert.ToInt32(sPDt[2]), Convert.ToInt32(sPDt[0]), Convert.ToInt32(sPDt[1]), HourIn, MinuteIn, 0, DateTimeKind.Unspecified);

            sPOut1 = adjustmentInfo.adjUtcOut.Trim().Split(':');
            sPOut2 = sPOut1[1].Split(' ');
            if (sPOut2[1].Trim() == "PM" && Convert.ToInt32(sPOut1[0]) != 12)
                HourOut = Convert.ToInt32(sPOut1[0]) + 12;
            else if (sPOut2[1].Trim() == "AM" && Convert.ToInt32(sPOut1[0]) == 12)
                HourOut = 0;
            else
                HourOut = Convert.ToInt32(sPOut1[0]);
            MinuteOut = Convert.ToInt32(sPOut2[0]);

            adjustedOut = new DateTime(Convert.ToInt32(sPDt[2]), Convert.ToInt32(sPDt[0]), Convert.ToInt32(sPDt[1]), HourOut, MinuteOut, 0, DateTimeKind.Unspecified);

            if (adjustedOut < adjustedIn)
                adjustedOut = ((DateTime)adjustedOut).AddDays(1);


            DateTime utcrecordedIn = TimeZoneInfo.ConvertTimeToUtc(recordedIn, TimeZone);
            DateTime utcadjustedIn = TimeZoneInfo.ConvertTimeToUtc(adjustedIn, TimeZone);
            DateTime utcrecordedOut = TimeZoneInfo.ConvertTimeToUtc(recordedOut, TimeZone);
            DateTime utcadjustedOut = TimeZoneInfo.ConvertTimeToUtc(adjustedOut, TimeZone);



            double inTimeChange = (utcadjustedIn - utcrecordedIn).TotalMinutes;
            double outTimeChange = (utcadjustedOut - utcrecordedOut).TotalMinutes;
            double maxAdjustmentInMinutes = 6 * 60;
            if (outTimeChange > 0)
            {
                er.code = 1;
                er.msg = "You cannot adjust your out time forward!";
            }

            else if (inTimeChange > maxAdjustmentInMinutes || inTimeChange < -maxAdjustmentInMinutes)
            {
                er.code = 1;
                er.msg = "The In time cannot be adjusted more than 6 hours!";
            }
            else
            {
                string[] sP = adjustmentInfo.clientLocationValue.Split('|');
                // update therapy record here??
             
                if (er.code == 0)
                {
                    InOutProcessor t = new InOutProcessor(UserClaim.conStr, UserClaim.timeZone);
                    t.HCBSEmpHrsId = sessionId;

                    t.utcIn = utcrecordedIn;
                    t.utcOut = utcrecordedOut;
                    t.adjutcIn = (utcrecordedIn == utcadjustedIn) ? null : (DateTime?)utcadjustedIn;
                    t.adjutcOut = (utcrecordedOut == utcadjustedOut) ? null : (DateTime?)utcadjustedOut;

                    t.date1 = Convert.ToDateTime(recordedDate).ToShortDateString();
                    if (recordedDate != adjustedDate)
                        t.adjdate1 = Convert.ToDateTime(adjustedDate).ToShortDateString();
                    t.clientLocationId = Convert.ToInt32(sP[0]);
                    t.locationTypeId = Convert.ToInt32(sP[1]);


                    t.checkTherapyRecordAgainstAuths(ref er);
                    if (er.code == 0)
                        t.updateTherapyRecordAdjusted(ref er);

                }
            }
            return er;
        }





        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> GetTherapyNote(string docId)
        {
            ClientNote r = new ClientNote();
            DataSet ds = new DataSet();

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskGetSessionTherapyNote", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@clientSessionTherapyId", docId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                DataRow dr = ds.Tables[0].Rows[0];

                if ((bool)dr["completed"] && !(bool)dr["verified"])
                    r.verification = true;
                else
                    r.verification = false;

                r.signee = UserClaim.staffname;
                r.signeeCredentials = UserClaim.stafftitle + (UserClaim.staffnpi != "" ? " (NPI: " + UserClaim.staffnpi + ")" : "");
                r.noShow = (bool)dr["noShow"];
                r.teletherapy = (bool)dr["teletherapy"];
                r.clientName = (string)dr["cnm"];
                r.providerName = (string)dr["snm"];
                r.svc = (string)dr["svc"];
                r.note = dr["note"] == DBNull.Value ? "" : (string)dr["note"];
                r.dt = ((DateTime)dr["dt"]).ToShortDateString();

                r.docId = (int)dr["clientSessionTherapyID"];
                // XXXX New statuses to be used with note
                r.designeeUnableToSign = (bool)dr["designeeUnableToSign"];
                r.designeeRefusedToSign = (bool)dr["designeeRefusedToSign"];
                r.clientRefusedService = (bool)dr["clientRefusedService"];
                r.unsafeToWork = (bool)dr["unsafeToWork"];
                r.guardianId = (int)dr["guardianId"];
                r.designeeId = (int)dr["designeeId"];
               
                // XXXX end new Stuff

                r.hasAttachment = (bool)dr["hasAttachment"];

                r.attachmentName = (string)dr["attachmentName"].ToString();
                r.extension = (string)dr["fileExtension"];

                r.rejected = (bool)dr["rejected"];
                r.rejectedReason = dr["rejectedReason"].ToString();

                DataView dv = new DataView(ds.Tables[1]);

                r.longTermObjectives = dv.ToTable(true, "objectiveId", "longTermVision", "longTermGoal").Rows.Cast<DataRow>().Select(spR => new LongTermObjective()
                {
                    objectiveId = (int)spR["objectiveId"],
                    longTermVision = (string)spR["longTermVision"],
                    longTermGoal = (string)spR["longTermGoal"],
                }).ToList();

                foreach (LongTermObjective o in r.longTermObjectives)
                {
                    dv.RowFilter = "objectiveId=" + o.objectiveId;
                    o.shortTermGoals = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new ShortTermGoal()
                    {
                        goalId = (int)spR["goalId"],
                        shortTermGoal = (string)spR["shortTermGoal"],
                        teachingMethod = (string)spR["teachingMethod"],
                        frequency = (string)spR["frequency"],
                        score = (string)spR["score"],
                        trialPct = (string)spR["trialPct"] == "" ? "0" : (string)spR["trialPct"]
                    }).ToList();
                }

                r.scoring = ds.Tables[2].Rows.Cast<DataRow>().Select(spR => new Scoring()
                {
                    value = (string)spR["scoreValue"],
                    name = (string)spR["scoreName"],

                }).ToList();

                r.providerId = (int)dr["prId"];
                r.clientId = (int)dr["clsvId"];
                r.serviceId = (int)dr["serviceId"];
                r.clientServiceId = (int)dr["clsvidId"];

                // added for EVV notes completed on web
                r.isEVV = (bool)dr["isEVV"];
                r.clientLocationValue = dr["clientLocationId"] + "|" + dr["LocationTypeId"] + "|" + dr["lat"] + "|" + dr["lon"];
                r.inTime = DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString();
                r.outTime = DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString();
                r.adjDt = dr["dtAdj"] == DBNull.Value ? ((DateTime)dr["dt"]).ToShortDateString() : ((DateTime)dr["dtAdj"]).ToShortDateString();
                r.adjInTime = dr["adjUtcIn"] != DBNull.Value ? DateTimeLocal((DateTime)dr["adjUtcIn"]).ToShortTimeString() : DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString();
                r.adjOutTime = dr["adjUtcOut"] != DBNull.Value ? DateTimeLocal((DateTime)dr["adjUtcOut"]).ToShortTimeString() : DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString();
                r.isoDate = ((DateTime)dr["dt"]).ToString("yyyy-MM-dd");
                r.location = dr["location"] != DBNull.Value ? dr["location"].ToString() : "";
                r.locations = ds.Tables[3].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {

                    name = (string)spR["Location"],
                    value = spR["clientLocationId"] + "|" + spR["locationTypeId"] + "|" + spR["lat"] + "|" + spR["lon"]
                }).ToList();

                if ((int)dr["startClientLocationId"] == 0 || (int)dr["endClientLocationId"] == 0)
                    // no firm start or end location
                    r.locationDetermined = false;

                else
                    r.locationDetermined = true;
                // end added for EVV notes completed on web



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
                Response.StatusCode = 500;
                return null;
            }

            return PartialView("ModalSessionTherapyNote", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SetTherapyNote(IEnumerable<HttpPostedFileBase> files, string _sessionNote)
        {
            HomeStaffPage r = new HomeStaffPage();
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();

            SessioNote sNote = JsonConvert.DeserializeObject<SessioNote>(_sessionNote);

            w.er = FinalizeTherapyRecord(sNote.docId, sNote.providerId, sNote.clientId, sNote.serviceId, sNote.clientServiceId, sNote.svc, sNote.adjustmentInfo);


            sNote.providerId = UserClaim.prid;
            bool autoVerify = false;
            if (((UserClaim.userLevel == "TherapySupervisor" || UserClaim.dcwRole == "TherapySupervisor") && sNote.complete) || sNote.noShow)
            {
                sNote.complete = true;
                autoVerify = true;
            }
            DataSet ds = new DataSet();
            bool hasAttachment = false;
            string fileExtension = "";
            string fileName = null;
            if (files != null)
            {
                var file = files.FirstOrDefault();
                // Verify that the user selected a file
                if (file != null && file.ContentLength > 0)
                {
                    fileExtension = Path.GetExtension(file.FileName);
                    fileName = Convert.ToString(sNote.attachmentName);
                    byte[] data = new byte[file.InputStream.Length];
                    file.InputStream.Read(data, 0, data.Length);
                    try
                    {
                        FileData f = new FileData("attachments", UserClaim.blobStorage);
                        f.StoreFile(data, fileName + fileExtension);
                        hasAttachment = true;
                    }
                    catch (Exception ex)
                    {
                        w.er.code = 1;
                        w.er.msg = ex.Message;
                    }
                }
            }

            if (w.er.code == 0)
            {
                try
                {
                    DataTable dt = new DataTable();
                    dt.Clear();
                    dt.Columns.Add("goalId");
                    dt.Columns.Add("score");
                    dt.Columns.Add("trialPct");

                    for (int i = 0; i < sNote.sessionScores.Count; i++)
                    {

                        DataRow nRow = dt.NewRow();
                        nRow["goalId"] = sNote.sessionScores[i].goalId;
                        nRow["score"] = sNote.sessionScores[i].score;
                        nRow["trialPct"] = sNote.sessionScores[i].trialPct;
                        dt.Rows.Add(nRow);
                    }
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskSetSessionTherapyNote", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@prId", sNote.providerId != null ? sNote.providerId : UserClaim.prid);
                            cmd.Parameters.AddWithValue("@clientSessionTherapyId", sNote.docId);
                            cmd.Parameters.AddWithValue("@goalScores", dt);
                            cmd.Parameters.AddWithValue("@note", sNote.note);
                            cmd.Parameters.AddWithValue("@teletherapy", sNote.teletherapy);
                            cmd.Parameters.AddWithValue("@noShow", sNote.noShow);
                            cmd.Parameters.AddWithValue("@hasAttachment", hasAttachment);
                            cmd.Parameters.AddWithValue("@fileExtension", fileExtension);
                            cmd.Parameters.AddWithValue("@Timezone", UserClaim.timeZone);
                            cmd.Parameters.AddWithValue("@complete", sNote.complete);
                            cmd.Parameters.AddWithValue("@getPendingDocumentation", autoVerify ? false : true);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    });

                    if (autoVerify)
                    {
                        string fileName2 = await saveTherapyNoteAsFile(sNote.docId);

                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskSetSessionTherapyNoteApprove", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@clientSessionTherapyId", sNote.docId);
                            cmd.Parameters.AddWithValue("@prId", sNote.providerId != null ? sNote.providerId : UserClaim.prid);
                            cmd.Parameters.AddWithValue("@rejected", 0);
                            cmd.Parameters.AddWithValue("@rejectedreason", "");
                            cmd.Parameters.AddWithValue("@filename", fileName2);
                            cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }


                    }
                    r.pendingDocumentation = setPendingDocumentation(ref ds, 0);
                    r.staffAlerts = setStaffAlerts(ref ds, 1);
                    w.pendingDocumentation = RenderRazorViewToString("PendingDocumentation", r);
                    w.staffAlerts = RenderRazorViewToString("StaffAlerts", r.staffAlerts);
                }
                catch (Exception ex)
                {
                    w.er.code = 1;
                    w.er.msg = ex.Message;
                }

            }
            ds.Dispose();

            var jsonResult = Json(w);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }


        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SetTherapyNoteApproval(NoteAcceptance c)
        {
            HomeStaffPage r = new HomeStaffPage();
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();
            DataSet ds = new DataSet();
            try
            {
                string fileName = "";
                if (!c.rejected)
                {
                    fileName = await saveTherapyNoteAsFile(c.docId);
                }
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskSetSessionTherapyNoteApprove", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@clientSessionTherapyId", c.docId);
                        cmd.Parameters.AddWithValue("@prId", c.providerId != null ? c.providerId : UserClaim.prid.ToString());
                        cmd.Parameters.AddWithValue("@rejected", c.rejected);
                        cmd.Parameters.AddWithValue("@rejectedreason", c.rejectedReason == null ? "" : c.rejectedReason);
                        cmd.Parameters.AddWithValue("@fileName", fileName);
                        cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.pendingDocumentation = setPendingDocumentation(ref ds, 0);
                r.staffAlerts = setStaffAlerts(ref ds, 1);
                w.pendingDocumentation = RenderRazorViewToString("PendingDocumentation", r);
                w.staffAlerts = RenderRazorViewToString("StaffAlerts", r.staffAlerts);
            }
            catch (Exception ex)
            {
                w.er.code = 1;
                w.er.msg = ex.Message;
            }

            ds.Dispose();

            var jsonResult = Json(w);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        private async Task<string> saveTherapyNoteAsFile(int docId)
        {
            string fileName = "";
            ClientNotePdf rpr = new ClientNotePdf();

            await Task.Run(() =>
            {
                getTherapyNotePdf(Convert.ToString(docId), ref rpr);
            });

            var docStr = docId.ToString();
            fileName = "Therapy_" + ("0000000000000000".Substring(0, 16 - docStr.Length) + docStr) + ".pdf";

            PdfGenerateConfig config = new PdfGenerateConfig();
            config.PageSize = PageSize.Letter;
            config.MarginBottom = 90;
            config.MarginTop = 40;
            config.MarginRight = 30;
            config.MarginLeft = 30;
            config.PageOrientation = PageOrientation.Portrait;
            PdfDocument pdf = PdfGenerator.GeneratePdf(RenderRazorViewToString("Document_NoteTherapy", rpr), config);
            XFont A8 = new XFont("Arial", 8);
            XRect rect = new XRect(20, 31, 100, 13);
            for (int i = 0; i < pdf.PageCount; i++)
            {
                string footer = rpr.clientName + " (" + rpr.dt + ") Page " + (i + 1) + " of " + pdf.PageCount;
                PdfPage page = pdf.Pages[i];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XTextFormatter tf = new XTextFormatter(gfx);
                rect.X = 200;
                rect.Y = 750;
                rect.Width = 300;
                rect.Height = 10;
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(footer, A8, XBrushes.DarkSlateGray, rect, XStringFormats.TopLeft);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                pdf.Save(ms, false);
                ms.Seek(0, SeekOrigin.Begin);

                FileData f = new FileData("sessionnotes", UserClaim.blobStorage);
                f.StoreFile(ms.ToArray(), fileName);
            }

            return fileName;
        }

        private void getTherapyNotePdf(string docId, ref ClientNotePdf r)
        {
            DataSet ds = new DataSet();

            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_TaskGetSessionTherapyNotePdf", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ClientSessionTherapyId", docId);
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
            }
            DataRow dr = ds.Tables[0].Rows[0];
            r.completedBy = dr["completedByFn"] + " " + dr["completedByLn"];
            r.completedByCredentials = dr["completedByTitle"] + ((string)dr["completedByNpi"] != "" ? " (NPI: " + dr["completedByNpi"] + ")" : "");
            r.approvedBy = dr["approvedByFn"] + " " + dr["approvedByLn"];
            r.approvedByCredentials = dr["approvedByTitle"] + ((string)dr["approvedByNpi"] != "" ? " (NPI: " + dr["approvedByNpi"] + ")" : "");
            r.teletherapy = (bool)dr["teletherapy"];
            r.timeOfService = DateTimeLocal((DateTime)dr["startAt"]).ToShortTimeString() + " - " + DateTimeLocal((DateTime)dr["endAt"]).ToShortTimeString();
            r.agency = UserClaim.companyName;
            r.npi = UserClaim.npi;
            r.clientName = (string)dr["cnm"];
            r.svc = (string)dr["svc"];
            r.serviceName = (string)dr["serviceName"];
            r.dt = ((DateTime)dr["dt"]).ToShortDateString();

            r.note = ConvertToHtml((string)dr["note"]);
            r.noShow = (bool)dr["noShow"];
            // XXXX New statuses to be used with note
            r.designeeUnableToSign = (bool)dr["designeeUnableToSign"];
            r.designeeRefusedToSign = (bool)dr["designeeRefusedToSign"];
            r.clientRefusedService = (bool)dr["clientRefusedService"];
            r.unsafeToWork = (bool)dr["unsafeToWork"];
            r.guardianId = (int)dr["guardianId"];
            r.designeeId = (int)dr["designeeId"];
            // XXXX end new Stuff
            r.dob = dr["dob"] == DBNull.Value ? "" : ((DateTime)dr["dob"]).ToShortDateString();
            r.clId = dr["clId"] == DBNull.Value ? "" : (string)dr["clId"];
            r.clientWorker = dr["clwNm"] == DBNull.Value ? "" : (string)dr["clwNm"];
            DataView dv = new DataView(ds.Tables[1]);

            r.longTermObjectives = dv.ToTable(true, "objectiveId", "longTermVision", "longTermGoal").Rows.Cast<DataRow>().Select(spR => new LongTermObjective()
            {
                objectiveId = (int)spR["objectiveId"],
                longTermVision = ConvertToHtml((string)spR["longTermVision"]),
                longTermGoal = ConvertToHtml((string)spR["longTermGoal"]),
            }).ToList();

            foreach (LongTermObjective o in r.longTermObjectives)
            {
                dv.RowFilter = "objectiveId=" + o.objectiveId;
                o.shortTermGoals = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new ShortTermGoal()
                {
                    goalId = (int)spR["goalId"],
                    shortTermGoal = ConvertToHtml((string)spR["shortTermGoal"]),
                    teachingMethod = ConvertToHtml((string)spR["teachingMethod"]),
                    score = (string)spR["score"],
                    trialPct = (string)spR["trialPct"]

                }).ToList();
            }

            ds.Dispose();
        }

        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> GetAtcMonitoringReport(ProviderNoteRequest c)
        {
            AtcMonitoringResponse r = new AtcMonitoringResponse();
            DataSet ds = new DataSet();

            try
            {
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
                        cmd.Parameters.AddWithValue("@clsvId", c.clsvId);
                        cmd.Parameters.AddWithValue("@ATCMonitorId", c.docId);
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

                    if (ds.Tables[0].Rows.Count != 0)
                    {
                        DataRow dr = ds.Tables[0].Rows[0];
                        r.cnm = (string)dr["cnm"];
                        r.svc = (string)dr["svc"];

                        r.clsvId = (int)dr["clsvId"];

                        r.clsvidId = (int)dr["clsvidId"];
                        r.clwNm = (string)dr["clwNm"];
                        r.guardian = (string)dr["guardian"];
                        r.serviceStartDate = dr["serviceStartDate"] == DBNull.Value ? "" : ((DateTime)dr["serviceStartDate"]).ToShortDateString();
                        r.atcMonitorId = (int)dr["atcMonitorId"];
                        r.visitDt = dr["visitDt"] == DBNull.Value ? "" : ((DateTime)dr["visitDt"]).ToShortDateString();
                        r.days5 = dr["days5"] == DBNull.Value ? false : (bool)dr["days5"];
                        r.days30 = dr["days30"] == DBNull.Value ? false : (bool)dr["days30"];
                        r.days60 = dr["days60"] == DBNull.Value ? false : (bool)dr["days60"];
                        r.days90 = dr["days90"] == DBNull.Value ? false : (bool)dr["days90"];
                        r.anc = dr["anc"] == DBNull.Value ? false : (bool)dr["anc"];
                        r.afc = dr["afc"] == DBNull.Value ? false : (bool)dr["afc"];
                        r.hsk = dr["hsk"] == DBNull.Value ? false : (bool)dr["hsk"];
                        if (ds.Tables[1].Rows.Count != 0)
                        {

                            r.atcQuestions = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new AtcQuestion()
                            {
                                atcQuestId = (int)spR["atcQuestId"],
                                qNum = (string)spR["qNum"],
                                question = (string)spR["question"],
                                yes = spR["yes"] == DBNull.Value ? false : (bool)spR["yes"],
                                no = spR["no"] == DBNull.Value ? false : (bool)spR["no"],
                                na = spR["na"] == DBNull.Value ? false : (bool)spR["na"],
                                cmt = spR["cmt"] == DBNull.Value ? "" : (string)spR["cmt"]

                            }).ToList();
                        }
                        if (ds.Tables[2].Rows.Count != 0)
                        {
                            r.providers = ds.Tables[2].Rows.Cast<DataRow>().Select(spR => new Option()
                            {
                                name = spR["fn"] + " " + spR["ln"],
                                value = spR["prid"].ToString()
                            }).ToList();

                        }
                    }
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
                Response.StatusCode = 500;
                return null;
            }
            else
                return PartialView("ModalATCMonitoringReport", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> SetAtcMonitoringReport(AtcMonitoringResponse c)
        {
            HomeStaffPage r = new HomeStaffPage();
            r.userLevel = UserClaim.userLevel;

            DataSet ds = new DataSet();

            DataTable dt = new DataTable();
            dt.Clear();
            dt.Columns.Add("atcQuestId");
            dt.Columns.Add("yes");
            dt.Columns.Add("no");
            dt.Columns.Add("na");
            dt.Columns.Add("cmt");

            for (int i = 0; i < c.atcQuestions.Count; i++)
            {
                DataRow nRow = dt.NewRow();
                nRow["atcQuestId"] = c.atcQuestions[i].atcQuestId;
                nRow["yes"] = c.atcQuestions[i].yes;
                nRow["no"] = c.atcQuestions[i].no;
                nRow["na"] = c.atcQuestions[i].na;
                nRow["cmt"] = c.atcQuestions[i].cmt == null ? "" : c.atcQuestions[i].cmt;

                dt.Rows.Add(nRow);
            }

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskSetATCMonitoringReport", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@clsvId", c.clsvId);
                        cmd.Parameters.AddWithValue("@clsvidId", c.clsvidId);
                        cmd.Parameters.AddWithValue("@prId", c.prId);
                        cmd.Parameters.AddWithValue("@providerId", c.providerId);
                        cmd.Parameters.AddWithValue("@atcMonitorId", c.atcMonitorId);
                        cmd.Parameters.AddWithValue("@guardian", c.guardian);
                        cmd.Parameters.AddWithValue("@clwNm", c.clwNm);
                        cmd.Parameters.AddWithValue("@days5", c.days5);
                        cmd.Parameters.AddWithValue("@days30", c.days30);
                        cmd.Parameters.AddWithValue("@days60", c.days60);
                        cmd.Parameters.AddWithValue("@days90", c.days90);
                        cmd.Parameters.AddWithValue("@anc", c.anc);
                        cmd.Parameters.AddWithValue("@afc", c.afc);
                        cmd.Parameters.AddWithValue("@hsk", c.hsk);

                        cmd.Parameters.AddWithValue("@visitDt", c.visitDt);
                        cmd.Parameters.AddWithValue("@nextVisitDt", Convert.ToDateTime(c.visitDt).AddDays(c.nextVisit).ToShortDateString());
                        cmd.Parameters.AddWithValue("@serviceStartDate", c.serviceStartDate);
                        cmd.Parameters.AddWithValue("@questions", dt);
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
                    if (ds.Tables[0].Rows.Count != 0)
                        r.pendingDocumentation = setPendingDocumentation(ref ds, 0);
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
                Response.StatusCode = 500;
                return null;
            }
            else
                return PartialView("PendingDocumentation", r);

        }

        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> GetEvalNote(string docId)
        {
            ProgressReport r = new ProgressReport();
            DataSet ds = new DataSet();
            if (UserClaim.userLevel == "TherapySupervisor" || UserClaim.dcwRole == "TherapySupervisor")
                r.isTherapistSupervisor = true;
            else
                r.isTherapistSupervisor = false;

            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskGetTherapyEvalNote", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@clientSessionTherapyId", docId);
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
                    DataRow dr = ds.Tables[0].Rows[0];
                    r.signee = UserClaim.staffname;
                    r.signeeCredentials = UserClaim.stafftitle + (UserClaim.staffnpi != "" ? " (NPI: " + UserClaim.staffnpi + ")" : "");

                    r.providerId= (int)dr["prId"];
                    r.clientName = (string)dr["cnm"];
                    r.svc = (string)dr["svc"];
                    r.serviceName = (string)dr["serviceName"];
                    r.dt = ((DateTime)dr["dt"]).ToShortDateString();
                    r.clientId = (int)dr["clsvId"];
                    r.serviceId = (int)dr["evaluationserviceId"];
                    r.clientServiceId = (int)dr["clientServiceId"];
                    r.docId = (int)dr["clientSessionTherapyId"];
                    r.formType = "TherapyEvaluation";
                    r.hasAttachment = (bool)dr["hasAttachment"];
                    r.attachmentName = dr["attachmentName"].ToString();
                    r.treatmentFrequencyId = Convert.ToString(dr["treatmentFrequencyId"]);
                    r.treatmentFrequency = (string)dr["treatmentFrequency"];
                    r.numberOfVisits = Convert.ToInt32(dr["numberOfVisits"]);
                    r.treatmentDurationId = Convert.ToString(dr["treatmentDurationId"]);
                    r.teletherapy = (bool)dr["teletherapy"];
                    // r.treatmentDurationId = Convert.ToString(dr["treatmentDurationId"]);
                    // r.treatmentFrequencyId = Convert.ToString(dr["treatmentFrequencyId"]);
                    //  r.treatmentFrequency = (string)dr["treatmentFrequency"];


                    // added for EVV notes completed on web
                    r.isEVV = (bool)dr["isEVV"];


                    r.clientLocationValue = dr["clientLocationId"] + "|" + dr["LocationTypeId"] + "|" + dr["lat"] + "|" + dr["lon"];
                    r.inTime = DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString();
                    r.adjDt = dr["dtAdj"] == DBNull.Value ? ((DateTime)dr["dt"]).ToShortDateString() : ((DateTime)dr["dtAdj"]).ToShortDateString();
                    r.outTime = DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString();
                    r.adjInTime = dr["adjUtcIn"] != DBNull.Value ? DateTimeLocal((DateTime)dr["adjUtcIn"]).ToShortTimeString() : DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString();
                    r.adjOutTime = dr["adjUtcOut"] != DBNull.Value ? DateTimeLocal((DateTime)dr["adjUtcOut"]).ToShortTimeString() : DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString();
                
                    
                    
                    r.isoDate = ((DateTime)dr["dt"]).ToString("yyyy-MM-dd");
                    r.location = dr["location"] != DBNull.Value ? dr["location"].ToString() : ""; 
                    r.locations = ds.Tables[6].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                    {

                        name = (string)spR["Location"],
                        value = spR["clientLocationId"] + "|" + spR["locationTypeId"] + "|" + spR["lat"] + "|" + spR["lon"]
                    }).ToList();

                    if ((int)dr["startClientLocationId"] == 0 || (int)dr["endClientLocationId"] == 0)
                        // no firm start or end location
                        r.locationDetermined = false;

                    else
                        r.locationDetermined = true;
                    // end added for EVV notes completed on web
                    r.extension = dr["fileExtension"] == DBNull.Value ? "" : (string)dr["fileExtension"];

                    r.questions = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new Question()
                    {
                        questionId = (int)spR["questionId"],
                        title = (string)spR["title"],
                        answer = (string)spR["answer"],
                        isRequired = (bool)spR["isRequired"]
                    }).ToList();


                    DataView dv = new DataView(ds.Tables[2]);

                    r.longTermObjectives = dv.ToTable(true, "objectiveId", "longTermVision", "longTermGoal", "goalAreaId", "goalAreaName", "goalAreaActive","objectiveStatus", "changes").Rows.Cast<DataRow>().Select(spR => new LongTermObjective()
                    {
                        objectiveId = (int)spR["objectiveId"],
                        longTermVision = (string)spR["longTermVision"],
                        longTermGoal = (string)spR["longTermGoal"],
                        goalAreaId = (int)spR["goalAreaId"],
                        goalAreaName = (string)spR["goalAreaName"],
                        goalAreaActive = (bool)spR["goalAreaActive"],
                        objectiveStatus = (string)spR["objectiveStatus"],
                        changes = (string)spR["changes"]
                    }).ToList();

                    foreach (LongTermObjective o in r.longTermObjectives)
                    {
                        dv.RowFilter = "objectiveId=" + o.objectiveId + " AND goalId IS NOT NULL";
                        o.shortTermGoals = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new ShortTermGoal()
                        {
                            goalId = (int)spR["goalId"],
                            step = (short)spR["step"],
                            shortTermGoal = (string)spR["shortTermGoal"],
                            teachingMethod = (string)spR["teachingMethod"],
                            goalStatus = (string)spR["goalStatus"],
                            frequencyId = Convert.ToString(spR["frequencyId"]),
                            frequency = (string)spR["frequency"],
                         //   progress = (string)spR["goalprogress"],
                        //    recommendation = (string)spR["goalrecommendations"]
                        }).ToList();
                    }

                    r.goalAreas = ds.Tables[3].Rows.Cast<DataRow>().Select(spR => new Option()
                    {
                        value = spR["goalAreaId"].ToString(),
                        name = (string)spR["name"]
                    }).ToList();

                    r.frequencies = ds.Tables[4].Rows.Cast<DataRow>().Select(spR => new Option()
                    {
                        value = spR["frequencyId"].ToString(),
                        name = (string)spR["name"]
                    }).ToList();

                    r.statuses = ds.Tables[5].Rows.Cast<DataRow>().Select(spR => new Option()
                    {
                        value = (string)spR["name"],
                        name = (string)spR["name"]
                    }).ToList();

                    r.durations = new List<Option>();
                    for (int i = 12; i > 0; i--)
                    {
                        Option o = new Option
                        {
                            value = i.ToString(),
                            name = i + " Months"
                        };
                        r.durations.Add(o);
                    }
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
                Response.StatusCode = 500;
                return null;
            }
            return PartialView("ModalEvaluationTherapy", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SetEvalNote(IEnumerable<HttpPostedFileBase> files, string _progressReport, bool complete)
        {
            HomeStaffPage r = new HomeStaffPage();
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();
            ProgressReport pr = null;
            bool hasAttachment = false;
            string fileExtension = "";
            string fileName = null;
            pr = JsonConvert.DeserializeObject<ProgressReport>(_progressReport);

            w.er = FinalizeTherapyRecord(pr.docId, (int)pr.providerId, pr.clientId, pr.serviceId, pr.clientServiceId, pr.svc, pr.adjustmentInfo);


            pr.providerId = UserClaim.prid;

            if (files != null)
            {

                var file = files.FirstOrDefault();
                // if the user selected a file
                if (file != null && file.ContentLength > 0)
                {
                    fileExtension = Path.GetExtension(file.FileName);
                    fileName = pr.attachmentName;
                    byte[] data = new byte[file.InputStream.Length];
                    file.InputStream.Read(data, 0, data.Length);
                    try
                    {
                        FileData f = new FileData("attachments", UserClaim.blobStorage);
                        f.StoreFile(data, fileName + fileExtension);
                        hasAttachment = true;
                    }
                    catch (Exception ex)
                    {
                        w.er.code = 1;
                        w.er.msg = ex.Message;
                    }
                }
            }
            if (r.er.code == 0)
            {
                try
                {
                    DataTable answers = new DataTable();
                    answers.Clear();
                    answers.Columns.Add("questionId");
                    answers.Columns.Add("answer");
                    foreach (Question q in pr.questions)
                    {
                        DataRow nRow = answers.NewRow();
                        nRow["questionId"] = q.questionId;
                        nRow["answer"] = q.answer;
                        answers.Rows.Add(nRow);
                    }

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

                    foreach (LongTermObjective l in pr.longTermObjectives)
                    {
                        DataRow nRow1 = objectives.NewRow();
                        nRow1["objectiveId"] = l.objectiveId;
                        nRow1["goalAreaId"] = l.goalAreaId;
                        nRow1["longTermVision"] = l.longTermVision == null ? "" : l.longTermVision;
                        nRow1["longTermGoal"] = l.longTermGoal == null ? "" : l.longTermGoal;
                        nRow1["objectiveStatus"] = l.objectiveStatus;
                        if (l.objectiveStatus == "Completed")
                            nRow1["completedDt"] = DateTimeLocal().ToShortDateString();
                        nRow1["changes"] = l.changes == null ? "" : l.changes;

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
                                if (s.goalStatus == "Completed")
                                    nRow1["completedDt"] = DateTimeLocal().ToShortDateString();

                                nRow2["frequencyId"] = s.frequencyId;
                               /*nRow2["progress"] = s.progress =="";
                                nRow2["recommendation"] = s.recommendation == null ? "" : s.recommendation;
                               */
                                shortTermGoals.Rows.Add(nRow2);
                            }

                        }

                    }
                    DataSet ds = new DataSet();

                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskSetTherapyEvalNote", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@complete", complete);
                       //     cmd.Parameters.AddWithValue("@completedDt", DateTimeLocal().ToShortDateString());
                            cmd.Parameters.AddWithValue("@prId", pr.providerId);
                            cmd.Parameters.AddWithValue("@clientSessionTherapyId", pr.docId);
                            cmd.Parameters.AddWithValue("@teletherapy", pr.teletherapy);
                            cmd.Parameters.AddWithValue("@numberOfVisits", pr.numberOfVisits);
                            cmd.Parameters.AddWithValue("@treatmentFrequencyId", pr.treatmentFrequencyId);
                            cmd.Parameters.AddWithValue("@treatmentDurationId", pr.treatmentDurationId);
                            cmd.Parameters.AddWithValue("@hasAttachment", hasAttachment);
                            cmd.Parameters.AddWithValue("@fileExtension", fileExtension);
                            cmd.Parameters.AddWithValue("@answers", answers);
                            cmd.Parameters.AddWithValue("@shortTermGoals", shortTermGoals);
                            cmd.Parameters.AddWithValue("@objectives", objectives);
                            cmd.Parameters.AddWithValue("@getPendingDocumentation", !complete);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    });

                    if (complete)
                    {
                        string fileName2 = await saveTherapyEvaluationAsFile(pr.docId);

                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskSetTherapyEvalNoteComplete", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                            cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@clientSessionTherapyId", pr.docId);
                            cmd.Parameters.AddWithValue("@prId", pr.providerId);
                            cmd.Parameters.AddWithValue("@fileName", fileName2);
                            cmd.Parameters.AddWithValue("@timeZone", UserClaim.timeZone);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    }
                    ds.Dispose();
                    r.pendingDocumentation = setPendingDocumentation(ref ds, 0);
                    r.staffAlerts = setStaffAlerts(ref ds, 1);
                    w.pendingDocumentation = RenderRazorViewToString("PendingDocumentation", r);
                    w.staffAlerts = RenderRazorViewToString("StaffAlerts", r.staffAlerts);
                }
                catch (Exception ex)
                {
                    w.er.code = 1;
                    w.er.msg = ex.Message;
                }
            }
            var jsonResult = Json(w);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
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
                Response.StatusCode = 500;
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

        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> GetTherapyPlanOfCare(string docId)
        {
            ProgressReport r = new ProgressReport();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskGetTherapyPlanOfCare", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@planOfCareId", docId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                r.signee = UserClaim.staffname;
                r.signeeCredentials = UserClaim.stafftitle + (UserClaim.staffnpi != "" ? " (NPI: " + UserClaim.staffnpi + ")" : "");

                DataRow dr = ds.Tables[0].Rows[0];
                r.clientName = (string)dr["cnm"];
                r.svc = (string)dr["svc"];
                r.serviceName = (string)dr["serviceName"];
                r.dt = ((DateTime)dr["duedt"]).ToShortDateString();
                r.clientId = (int)dr["clsvId"];
                r.serviceId = (int)dr["evaluationserviceId"];
                r.docId = (int)dr["PlanOfCareId"];
                r.formType = "PlanOfCare";

                r.treatmentStart = ((DateTime)dr["treatmentStart"]).ToString("yyyy-MM-dd");
                r.treatmentEnd = ((DateTime)dr["treatmentEnd"]).ToString("yyyy-MM-dd");
                r.treatmentFrequencyId = Convert.ToString(dr["treatmentFrequencyId"]);
                r.treatmentFrequency = (string)dr["treatmentFrequency"];
                r.numberOfVisits = Convert.ToInt32(dr["numberOfVisits"]);
                r.treatmentDurationId = Convert.ToString(dr["treatmentDurationId"]);


                r.questions = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new Question()
                {
                    questionId = (int)spR["questionId"],
                    title = (string)spR["title"],
                    answer = (string)spR["answer"],
                    isRequired = (bool)spR["isRequired"]
                }).ToList();


                DataView dv = new DataView(ds.Tables[2]);
                r.longTermObjectives = dv.ToTable(true, "objectiveId", "longTermVision", "longTermGoal", "goalAreaId", "goalAreaName", "goalAreaActive", "objectiveStatus").Rows.Cast<DataRow>().Select(spR => new LongTermObjective()
                {
                    objectiveId = (int)spR["objectiveId"],
                    longTermVision = (string)spR["longTermVision"],
                    longTermGoal = (string)spR["longTermGoal"],
                    goalAreaId = (int)spR["goalAreaId"],
                    goalAreaName = (string)spR["goalAreaName"],
                    goalAreaActive = (bool)spR["goalAreaActive"],
                    objectiveStatus = (string)spR["objectiveStatus"],
                  //  changes = (string)spR["changes"]
                }).ToList();
            

                foreach (LongTermObjective o in r.longTermObjectives)
                {
                    dv.RowFilter = "objectiveId=" + o.objectiveId + " AND goalId IS NOT NULL";
                    o.shortTermGoals = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new ShortTermGoal()
                    {
                        goalId = (int)spR["goalId"],
                        step = (short)spR["step"],
                        shortTermGoal = (string)spR["shortTermGoal"],
                        teachingMethod = (string)spR["teachingMethod"],
                        goalStatus = (string)spR["goalStatus"],
                        frequencyId = Convert.ToString(spR["frequencyId"]),
                        frequency = (string)spR["frequency"]
                    }).ToList();
                }

                r.goalAreas = ds.Tables[3].Rows.Cast<DataRow>().Select(spR => new Option()
                {
                    value = spR["goalAreaId"].ToString(),
                    name = (string)spR["name"]
                }).ToList();

                r.frequencies = ds.Tables[4].Rows.Cast<DataRow>().Select(spR => new Option()
                {
                    value = spR["frequencyId"].ToString(),
                    name = (string)spR["name"]
                }).ToList();

                r.statuses = ds.Tables[5].Rows.Cast<DataRow>().Select(spR => new Option()
                {
                    value = (string)spR["name"],
                    name = (string)spR["name"]
                }).ToList();

            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 500;
                return null;
            }
            return PartialView("ModalPlanOfCareTherapy", r);
        }


        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SetTherapyPlanOfCare(IEnumerable<HttpPostedFileBase> files, string _progressReport, bool complete)
        {
            HomeStaffPage r = new HomeStaffPage();
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();
            ProgressReport progressReport = null;

            progressReport = JsonConvert.DeserializeObject<ProgressReport>(_progressReport);

            if (r.er.code == 0)
            {
                try
                {
                    DataTable answers = new DataTable();
                    answers.Clear();
                    answers.Columns.Add("questionId");
                    answers.Columns.Add("answer");
                    foreach (Question q in progressReport.questions)
                    {
                        DataRow nRow = answers.NewRow();
                        nRow["questionId"] = q.questionId;
                        nRow["answer"] = q.answer;
                        answers.Rows.Add(nRow);
                    }

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

                    foreach (LongTermObjective l in progressReport.longTermObjectives)
                    {
                        DataRow nRow1 = objectives.NewRow();
                        nRow1["objectiveId"] = l.objectiveId;
                        nRow1["goalAreaId"] = l.goalAreaId;
                        nRow1["longTermVision"] = l.longTermVision == null ? "" : l.longTermVision;
                        nRow1["longTermGoal"] = l.longTermGoal == null ? "" : l.longTermGoal;
                        nRow1["objectiveStatus"] = l.objectiveStatus;
                        if (l.objectiveStatus == "Completed")
                            nRow1["completedDt"] = DateTimeLocal().ToShortDateString();
                        nRow1["changes"] = "";
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
                                if (s.goalStatus == "Completed")
                                    nRow1["completedDt"] = DateTimeLocal().ToShortDateString();

                                nRow2["frequencyId"] = s.frequencyId;
                                nRow2["progress"] = "";
                                nRow2["recommendation"] = "";
                                shortTermGoals.Rows.Add(nRow2);
                            }
                        }
                    }
                    DataSet ds = new DataSet();

                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskSetTherapyPlanOfCare", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@completedDt", DateTimeLocal().ToShortDateString());
                            cmd.Parameters.AddWithValue("@prId", progressReport.providerId);
                            cmd.Parameters.AddWithValue("@planOfCareId", progressReport.docId);
                            cmd.Parameters.AddWithValue("@treatmentStart", progressReport.treatmentStart);
                            cmd.Parameters.AddWithValue("@treatmentEnd", progressReport.treatmentEnd);
                            cmd.Parameters.AddWithValue("@numberOfVisits", progressReport.numberOfVisits);
                            cmd.Parameters.AddWithValue("@clientId", progressReport.clientId);
                            cmd.Parameters.AddWithValue("@serviceId", progressReport.serviceId);
                            cmd.Parameters.AddWithValue("@treatmentFrequencyId", progressReport.treatmentFrequencyId);
                            cmd.Parameters.AddWithValue("@treatmentDurationId", progressReport.treatmentDurationId);
                            cmd.Parameters.AddWithValue("@answers", answers);
                            cmd.Parameters.AddWithValue("@shortTermGoals", shortTermGoals);
                            cmd.Parameters.AddWithValue("@objectives", objectives);
                            cmd.Parameters.AddWithValue("@getPendingDocumentation", !complete ? true : false);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    });

                    if (complete)
                    {
                        string fileName2 = await savePlanOfCareAsFile(progressReport.docId);

                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskSetTherapyPlanOfCareComplete", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@planOfCareId", progressReport.docId);
                            cmd.Parameters.AddWithValue("@prId", progressReport.providerId);
                            cmd.Parameters.AddWithValue("@fileName", fileName2);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    }
                    ds.Dispose();
                    r.pendingDocumentation = setPendingDocumentation(ref ds, 0);
                    r.staffAlerts = setStaffAlerts(ref ds, 1);
                    w.pendingDocumentation = RenderRazorViewToString("PendingDocumentation", r);
                    w.staffAlerts = RenderRazorViewToString("StaffAlerts", r.staffAlerts);
                }
                catch (Exception ex)
                {
                    w.er.code = 1;
                    w.er.msg = ex.Message;
                }
            }
            var jsonResult = Json(w);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        private async Task<string> savePlanOfCareAsFile(int docId)
        {
            string fileName = "";
            ProgressReportPdf rpr = new ProgressReportPdf();

            await Task.Run(() =>
            {
                getTherapyPlanOfCarePdf(Convert.ToString(docId), ref rpr);
            });
         
            fileName = "PlanOfCare_" + rpr.year + "_" + rpr.month + "_" + rpr.fileIdentifier + ".pdf";

            PdfGenerateConfig config = new PdfGenerateConfig();
            config.PageSize = PageSize.Letter;
            config.MarginBottom = 90;
            config.MarginTop = 40;
            config.MarginRight = 30;
            config.MarginLeft = 30;
            config.PageOrientation = PageOrientation.Portrait;
            PdfDocument pdf = null;

            try {
                pdf = PdfGenerator.GeneratePdf(RenderRazorViewToString("Document_PlanOfCareTherapy", rpr), config);
            }


            catch(Exception ex)
            {
                var x = ex.Message;


            }



           
            XFont A8 = new XFont("Arial", 8);
            XRect rect = new XRect(20, 31, 100, 13);
            for (int i = 0; i < pdf.PageCount; i++)
            {
                string footer = rpr.clientName + " (Plan Of Care) Page " + (i + 1) + " of " + pdf.PageCount;
                PdfPage page = pdf.Pages[i];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XTextFormatter tf = new XTextFormatter(gfx);
                rect.X = 200;
                rect.Y = 750;
                rect.Width = 300;
                rect.Height = 10;
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(footer, A8, XBrushes.DarkSlateGray, rect, XStringFormats.TopLeft);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                pdf.Save(ms, false);
                ms.Seek(0, SeekOrigin.Begin);


                //      FileStream file = new FileStream(Server.MapPath("~") +"templates//file.pdf", FileMode.Create, FileAccess.Write);
                //       ms.WriteTo(file);
                //       file.Close();

                FileData f = new FileData("evaluations", UserClaim.blobStorage);
                f.StoreFile(ms.ToArray(), fileName);
            }
            return fileName;
        }

        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> GetTherapyProgressReport(string docId, bool approval)
        {
            ProgressReport r = new ProgressReport();
            r.approval = approval;
            try
            {
                await Task.Run(() =>
                {
                    getTherapyProgressReportForm(docId, ref r);
                });
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 500;
                return null;
            }

            return PartialView("ModalProgressReportTherapy", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> AddLongTermVision(ProgressReport r)
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
                        SqlCommand cmd = new SqlCommand("sp_TaskAddTherapyLongTermObjective", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@serviceId", r.serviceId);
                        cmd.Parameters.AddWithValue("@formType", r.formType);
                        cmd.Parameters.AddWithValue("@progressId", r.docId);
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
        public async Task<ActionResult> AddActionStep(ProgressReport r)
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
                        SqlCommand cmd = new SqlCommand("sp_TaskAddTherapyShortTermGoal", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@progressId", r.docId);

                        cmd.Parameters.AddWithValue("@formType", r.formType);
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

                r.frequencies = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new Option()
                {
                    value = spR["frequencyId"].ToString(),
                    name = (string)spR["name"]
                }).ToList();

                r.statuses = ds.Tables[2].Rows.Cast<DataRow>().Select(spR => new Option()
                {
                    value = (string)spR["name"],
                    name = (string)spR["name"]
                }).ToList();

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
        public async Task<ActionResult> SetTherapyProgressReport(IEnumerable<HttpPostedFileBase> files, string _progressReport, bool complete)
        {
            HomeStaffPage r = new HomeStaffPage();
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();
            ProgressReport progressReport = JsonConvert.DeserializeObject<ProgressReport>(_progressReport);
            bool hasAttachment = false;
            string fileExtension = "";
            string fileName = null;

         //   string moreInfo = "";



            if (files != null)
            {
                var file = files.FirstOrDefault();
                // if the user selected a file
                if (file != null && file.ContentLength > 0)
                {
                    fileExtension = Path.GetExtension(file.FileName);
                    fileName = progressReport.attachmentName + fileExtension;
                    byte[] data = new byte[file.InputStream.Length];
                    file.InputStream.Read(data, 0, data.Length);
                    try
                    {
                        FileData f = new FileData("attachments", UserClaim.blobStorage);
                        f.StoreFile(data, fileName);
                        hasAttachment = true;
                    }
                    catch (Exception ex)
                    {
                        w.er.code = 1;
                        w.er.msg = ex.Message;
                    }
                }
            }

            if (w.er.code == 0)
            {

                DataTable answers = new DataTable();
                answers.Clear();
                answers.Columns.Add("questionId");
                answers.Columns.Add("answer");
                foreach (Question q in progressReport.questions)
                {
                    DataRow nRow = answers.NewRow();
                    nRow["questionId"] = q.questionId;
                    nRow["answer"] = q.answer;
                    answers.Rows.Add(nRow);
                }

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

                bool selfApproval = false;
                if (complete && (UserClaim.userLevel == "TherapySupervisor" || UserClaim.dcwRole == "TherapySupervisor"))
                {
                    selfApproval = true;
                //    complete = false;
                }


                foreach (LongTermObjective l in progressReport.longTermObjectives)
                {
                    DataRow nRow1 = objectives.NewRow();
                    nRow1["objectiveId"] = l.objectiveId;
                    nRow1["goalAreaId"] = l.goalAreaId;
                    nRow1["longTermVision"] = l.longTermVision;
                    nRow1["longTermGoal"] = l.longTermGoal == null ? "" : l.longTermGoal;
                    nRow1["objectiveStatus"] = l.objectiveStatus == null ? "" : l.objectiveStatus;
                    if (l.completedDt != "") nRow1["completedDt"] = l.completedDt;
                    nRow1["changes"] = l.changes;

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
                            if (s.completedDt != "") nRow2["completedDt"] = s.completedDt;
                            nRow2["frequencyId"] = s.frequencyId;
                            nRow2["progress"] = s.progress;
                            nRow2["recommendation"] = s.recommendation;
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
                            SqlCommand cmd = new SqlCommand("sp_TaskSetProgressTherapyReport", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@complete", complete);
                            cmd.Parameters.AddWithValue("@completedDt", DateTimeLocal().ToShortDateString());
                            cmd.Parameters.AddWithValue("@prId", progressReport.providerId);
                            cmd.Parameters.AddWithValue("@progressId", progressReport.docId);
                            cmd.Parameters.AddWithValue("@hasAttachment", hasAttachment);
                            cmd.Parameters.AddWithValue("@fileExtension", fileExtension);
                            //   cmd.Parameters.AddWithValue("@treatmentFrequencyId", progressReport.treatmentFrequencyId);
                            //   cmd.Parameters.AddWithValue("@treatmentDurationId", progressReport.treatmentDurationId);
                            cmd.Parameters.AddWithValue("@goalsToAdd", progressReport.goalsToAdd);

                            cmd.Parameters.AddWithValue("@answers", answers);
                            cmd.Parameters.AddWithValue("@shortTermGoals", shortTermGoals);
                            cmd.Parameters.AddWithValue("@objectives", objectives);
                            cmd.Parameters.AddWithValue("@getPendingDocumentation", selfApproval ? false : true);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    });
                    if (selfApproval)
                    {
                        // special case when the provider is a master therapist - self approval
                        string fileName2 = await saveTherapyProgressReportAsFile(progressReport.docId);
                        if (r.er.code == 0)
                        {
                            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                            {
                                SqlCommand cmd = new SqlCommand("sp_TaskSetProgressTherapyReportVerification", cn)
                                {
                                    CommandType = CommandType.StoredProcedure
                                };
                                cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                                cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                                cmd.Parameters.AddWithValue("@progressId", progressReport.docId);
                                cmd.Parameters.AddWithValue("@verifiedDt", DateTimeLocal().ToShortDateString());
                                cmd.Parameters.AddWithValue("@prId", progressReport.providerId);
                                cmd.Parameters.AddWithValue("@fileName", fileName2);
                                cmd.Parameters.AddWithValue("@rejected", false);
                                cmd.Parameters.AddWithValue("@rejectedreason", "");
                                cmd.Parameters.AddWithValue("@completed", true);
                                cmd.Parameters.AddWithValue("@verified", true);
                                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                            }

                        }
                    }

                    r.pendingDocumentation = setPendingDocumentation(ref ds, 0);
                    r.staffAlerts = setStaffAlerts(ref ds, 1);
                    w.pendingDocumentation = RenderRazorViewToString("PendingDocumentation", r);
                    w.staffAlerts = RenderRazorViewToString("StaffAlerts", r.staffAlerts);


                }
                catch (Exception ex)
                {
                    w.er.code = 1;
                    w.er.msg = ex.Message;
                 //   moreInfo = ex.StackTrace;
                }
                ds.Dispose();
            }
            
            var jsonResult = Json(w);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }



        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SetTherapyProgressReportApproval(IEnumerable<HttpPostedFileBase> files, string _progressReport)
        {
            HomeStaffPage r = new HomeStaffPage();
            r.userLevel = UserClaim.userLevel;
            Windows w = new Windows();
            ProgressReport progressReport = JsonConvert.DeserializeObject<ProgressReport>(_progressReport);
            bool hasAttachment = false;
            string fileExtension = "";
            string fileName = null;
            if (files != null)
            {
                var file = files.FirstOrDefault();
                // if the user selected a file
                if (file != null && file.ContentLength > 0)
                {
                    fileExtension = Path.GetExtension(file.FileName);
                    fileName = progressReport.attachmentName + fileExtension;
                    byte[] data = new byte[file.InputStream.Length];
                    file.InputStream.Read(data, 0, data.Length);
                    try
                    {
                        FileData f = new FileData("attachments", UserClaim.blobStorage);
                        f.StoreFile(data, fileName);
                        hasAttachment = true;
                    }
                    catch (Exception ex)
                    {
                        w.er.code = 1;
                        w.er.msg = ex.Message;
                    }
                }
            }

            if (w.er.code == 0)
            {

                DataTable answers = new DataTable();
                answers.Clear();
                answers.Columns.Add("questionId");
                answers.Columns.Add("answer");
                foreach (Question q in progressReport.questions)
                {
                    DataRow nRow = answers.NewRow();
                    nRow["questionId"] = q.questionId;
                    nRow["answer"] = q.answer;
                    answers.Rows.Add(nRow);
                }

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



                foreach (LongTermObjective l in progressReport.longTermObjectives)
                {
                    DataRow nRow1 = objectives.NewRow();
                    nRow1["objectiveId"] = l.objectiveId;
                    nRow1["goalAreaId"] = l.goalAreaId;
                    nRow1["longTermVision"] = l.longTermVision == null ? "" : l.longTermVision;
                    nRow1["longTermGoal"] = l.longTermGoal == null ? "" : l.longTermGoal;
                    nRow1["objectiveStatus"] = l.objectiveStatus;
                    if (l.completedDt != "") nRow1["completedDt"] = l.completedDt;
                    nRow1["changes"] = l.changes;

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

                            if (s.completedDt != "") nRow2["completedDt"] = s.completedDt;

                            nRow2["frequencyId"] = s.frequencyId;
                            nRow2["progress"] = s.progress;
                            nRow2["recommendation"] = s.recommendation;
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
                            SqlCommand cmd = new SqlCommand("sp_TaskSetProgressTherapyReport", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@complete", false);
                            cmd.Parameters.AddWithValue("@completedDt", DateTimeLocal().ToShortDateString());
                            cmd.Parameters.AddWithValue("@prId", progressReport.providerId);
                            cmd.Parameters.AddWithValue("@progressId", progressReport.docId);
                            cmd.Parameters.AddWithValue("@hasAttachment", hasAttachment);
                            cmd.Parameters.AddWithValue("@fileExtension", fileExtension);
                            //   cmd.Parameters.AddWithValue("@treatmentFrequencyId", progressReport.treatmentFrequencyId);
                            //   cmd.Parameters.AddWithValue("@treatmentDurationId", progressReport.treatmentDurationId);
                            cmd.Parameters.AddWithValue("@goalsToAdd", progressReport.goalsToAdd);

                            cmd.Parameters.AddWithValue("@answers", answers);
                            cmd.Parameters.AddWithValue("@shortTermGoals", shortTermGoals);
                            cmd.Parameters.AddWithValue("@objectives", objectives);
                            cmd.Parameters.AddWithValue("@getPendingDocumentation", false);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    });




                    bool completed;
                    bool verified;
                    if (progressReport.approvalNote.rejected)
                    {
                        completed = false;
                        verified = false;
                    }
                    else
                    {
                        completed = true;
                        verified = true;
                        progressReport.approvalNote.rejectedReason = "";                      
                    }
                    string fileName2 = await saveTherapyProgressReportAsFile(progressReport.docId);
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskSetProgressTherapyReportVerification", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                            cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                            cmd.Parameters.AddWithValue("@progressId", progressReport.approvalNote.docId);
                            cmd.Parameters.AddWithValue("@verifiedDt", DateTimeLocal().ToShortDateString());
                            cmd.Parameters.AddWithValue("@prId", progressReport.approvalNote.providerId);
                            cmd.Parameters.AddWithValue("@fileName", fileName2);
                            cmd.Parameters.AddWithValue("@rejected", progressReport.approvalNote.rejected);
                            cmd.Parameters.AddWithValue("@rejectedreason", progressReport.approvalNote.rejectedReason);
                            cmd.Parameters.AddWithValue("@completed", completed);
                            cmd.Parameters.AddWithValue("@verified", verified);

                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    });





                    r.pendingDocumentation = setPendingDocumentation(ref ds, 0);
                    r.staffAlerts = setStaffAlerts(ref ds, 1);
                    w.pendingDocumentation = RenderRazorViewToString("PendingDocumentation", r);
                    w.staffAlerts = RenderRazorViewToString("StaffAlerts", r.staffAlerts);


                }
                catch (Exception ex)
                {
                    w.er.code = 1;
                    w.er.msg = ex.Message;
                }
                ds.Dispose();
            }
            var jsonResult = Json(w);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }





        /*
        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SetTherapyProgressReportApproval(NoteAcceptance c)
        {
            HomeStaffPage r = new HomeStaffPage();
            Windows w = new Windows();
            bool completed;
            bool verified;
            if (c.rejected)
            {
                completed = false;
                verified = false;
            }
            else
            {
                completed = true;
                verified = true;
                c.rejectedReason = "";
            }

            DataSet ds = new DataSet();
            try
            {
                string fileName = "";
                if (verified)
                {
                    fileName = await saveTherapyProgressReportAsFile(c.docId);
                }

                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskSetProgressTherapyReportVerification", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@progressId", c.docId);
                        cmd.Parameters.AddWithValue("@verifiedDt", DateTimeLocal().ToShortDateString());
                        cmd.Parameters.AddWithValue("@prId", c.providerId);
                        cmd.Parameters.AddWithValue("@fileName", fileName);
                        cmd.Parameters.AddWithValue("@rejected", c.rejected);
                        cmd.Parameters.AddWithValue("@rejectedreason", c.rejectedReason);
                        cmd.Parameters.AddWithValue("@completed", completed);
                        cmd.Parameters.AddWithValue("@verified", verified);

                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.pendingDocumentation = setPendingDocumentation(ref ds, 0);
                r.staffAlerts = setStaffAlerts(ref ds, 1);
                w.pendingDocumentation = RenderRazorViewToString("PendingDocumentation", r);
                w.staffAlerts = RenderRazorViewToString("StaffAlerts", r.staffAlerts);

            }
            catch (Exception ex)
            {
                w.er.code = 1;
                w.er.msg = ex.Message;
            }

            ds.Dispose();
            return Json(w);
        }
        */
        private async Task<string> saveTherapyProgressReportAsFile(int docId)
        {
            string fileName = "";
            ProgressReportPdf rpr = new ProgressReportPdf();

            await Task.Run(() =>
            {
                getTherapyProgressReportPdf(Convert.ToString(docId), ref rpr);
            });
            if (rpr.clId != null && rpr.clId.Length == 10)
                fileName = "DDDProgressReport_" + rpr.year + "_" + rpr.month + "_" + UserClaim.pcode + "_" + rpr.clId + "_" + rpr.svc + "_001.pdf";
            else
                fileName = "ProgressReport_" + rpr.year + "_" + rpr.fileIdentifier + ".pdf";

            PdfGenerateConfig config = new PdfGenerateConfig();
            config.PageSize = PageSize.Letter;
            config.MarginBottom = 90;
            config.MarginTop = 40;
            config.MarginRight = 30;
            config.MarginLeft = 30;
            config.PageOrientation = PageOrientation.Portrait;
            PdfDocument pdf = PdfGenerator.GeneratePdf(RenderRazorViewToString("Document_ProgressReportTherapy", rpr), config);
            XFont A8 = new XFont("Arial", 8);
            XRect rect = new XRect(20, 31, 100, 13);
            for (int i = 0; i < pdf.PageCount; i++)
            {
                string footer = rpr.clientName + " (" + rpr.dt + ") Page " + (i + 1) + " of " + pdf.PageCount;
                PdfPage page = pdf.Pages[i];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XTextFormatter tf = new XTextFormatter(gfx);
                rect.X = 150;
                rect.Y = 750;
                rect.Width = 300;
                rect.Height = 10;
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(footer, A8, XBrushes.DarkSlateGray, rect, XStringFormats.TopLeft);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                pdf.Save(ms, false);
                ms.Seek(0, SeekOrigin.Begin);
                FileData f = new FileData("progressreports", UserClaim.blobStorage);
                f.StoreFile(ms.ToArray(), fileName);
            }
            return fileName;
        }

        private async Task<string> saveTherapyEvaluationAsFile(int docId)
        {
            string fileName = "";
            ProgressReportPdf rpr = new ProgressReportPdf();

            await Task.Run(() =>
            {
                getTherapyEvaluationPdf(Convert.ToString(docId), ref rpr);
            });

            fileName = "Eval_" + rpr.fileIdentifier + "_" + rpr.svc + ".pdf";
            PdfGenerateConfig config = new PdfGenerateConfig();
            config.PageSize = PageSize.Letter;
            config.MarginBottom = 95;
            config.MarginTop = 35;
            config.MarginRight = 30;
            config.MarginLeft = 30;
            config.PageOrientation = PageOrientation.Portrait;
            PdfDocument pdf = null;
            try
            {
                var y = RenderRazorViewToString("Document_EvaluationTherapy", rpr);

                pdf = PdfGenerator.GeneratePdf(y, config);
            }
             catch(Exception ex)
            {
                var x = ex;
            }   
                
             XFont A8 = new XFont("Arial", 8);
            XRect rect = new XRect(20, 31, 100, 13);
            for (int i = 0; i < pdf.PageCount; i++)
            {
                string footer = rpr.clientName + " (" + rpr.dt + ") Page " + (i + 1) + " of " + pdf.PageCount;
                PdfPage page = pdf.Pages[i];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XTextFormatter tf = new XTextFormatter(gfx);
                rect.X = 150;
                rect.Y = 750;
                rect.Width = 300;
                rect.Height = 10;
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(footer, A8, XBrushes.DarkSlateGray, rect, XStringFormats.TopLeft);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                pdf.Save(ms, false);
                ms.Seek(0, SeekOrigin.Begin);
                FileData f = new FileData("evaluations", UserClaim.blobStorage);
                f.StoreFile(ms.ToArray(), fileName);
            }
            return fileName;
        }

        private void getTherapyProgressReportForm(string docId, ref ProgressReport r)
        {

            if (UserClaim.userLevel == "TherapySupervisor" || UserClaim.dcwRole == "TherapySupervisor")
                r.isTherapistSupervisor = true;
            else
                r.isTherapistSupervisor = false;



            DataSet ds = new DataSet();

            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_TaskGetTherapyProgressReport", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                cmd.Parameters.AddWithValue("@progressTherapyId", docId);
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
            }

            DataRow dr = ds.Tables[0].Rows[0];
            r.signee = UserClaim.staffname;
            r.signeeCredentials = UserClaim.stafftitle + (UserClaim.staffnpi != "" ? " (NPI: " + UserClaim.staffnpi + ")" : "");
            r.reportingPeriod = (string)dr["ReportingPeriod"];

            r.clientName = (string)dr["cnm"];
            r.svc = (string)dr["svc"];
            r.serviceName = (string)dr["serviceName"];
            r.startDate = ((DateTime)dr["startDate"]).ToString("yyyy-MM-dd");
            r.endDate = ((DateTime)dr["endDate"]).ToString("yyyy-MM-dd");
            r.dt = ((DateTime)dr["startDate"]).ToShortDateString() + " to " + ((DateTime)dr["endDate"]).ToShortDateString();
            r.clientId = (int)dr["clsvId"];
            r.serviceId = (int)dr["serviceId"];
            r.docId = (int)dr["progressTherapyId"];
            r.formType = "TherapyProgressReport";
            r.hasAttachment = (bool)dr["hasAttachment"];
            r.attachmentName = dr["attachmentName"].ToString();
            r.completedDt = DateTimeLocal().ToShortDateString();
            r.goalsToAdd = (string)dr["goalsToAdd"];
            //  r.treatmentFrequencyId = Convert.ToString(dr["treatmentFrequencyId"]);
            //   r.treatmentFrequency = (string)dr["treatmentFrequency"];
            //   r.numberOfVisits = Convert.ToInt32(dr["numberOfVisits"]);
            //r.treatmentDurationId = Convert.ToString(dr["treatmentDurationId"]);
            r.rejectedReason = dr["rejectedReason"] == DBNull.Value ? "" : dr["rejectedReason"].ToString();

            r.extension = dr["fileExtension"] == DBNull.Value ? "" : (string)dr["fileExtension"];

            r.questions = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new Question()
            {
                questionId = (int)spR["questionId"],
                title = (string)spR["title"],
                answer = (string)spR["answer"],
                isRequired = (bool)spR["isRequired"],
                supervisorOnly = (bool)spR["supervisorOnly"]
            }).ToList();


            DataView dv = new DataView(ds.Tables[2]);
            r.longTermObjectives = dv.ToTable(true, "objectiveId", "longTermVision", "longTermGoal", "goalAreaId", "goalAreaName", "goalAreaActive", "objectiveStatus", "oCompletedDt", "changes").Rows.Cast<DataRow>().Select(spR => new LongTermObjective()
            {
                objectiveId = (int)spR["objectiveId"],
                longTermVision = (string)spR["longTermVision"],
                longTermGoal = (string)spR["longTermGoal"],
                goalAreaId = (int)spR["goalAreaId"],
                goalAreaName = (string)spR["goalAreaName"],
                goalAreaActive = (bool)spR["goalAreaActive"],
                objectiveStatus = (string)spR["objectiveStatus"],
                completedDt = spR["OcompletedDt"] == DBNull.Value ? "" : ((DateTime)spR["OcompletedDt"]).ToString("yyyy-MM-dd"),
                 changes = (string)spR["changes"]
            }).ToList();
          

            foreach (LongTermObjective o in r.longTermObjectives)
            {
                dv.RowFilter = "objectiveId=" + o.objectiveId + " AND goalId IS NOT NULL";
                o.shortTermGoals = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new ShortTermGoal()
                {
                    goalId = (int)spR["goalId"],
                    step = (short)spR["step"],
                    shortTermGoal = (string)spR["shortTermGoal"],
                    teachingMethod = (string)spR["teachingMethod"],
                    goalStatus = (string)spR["goalStatus"],
                    completedDt = spR["GcompletedDt"] == DBNull.Value ? "" : ((DateTime)spR["GcompletedDt"]).ToString("yyyy-MM-dd"),
                    frequencyId = Convert.ToString(spR["frequencyId"]),
                    frequency = (string)spR["frequency"],
                    progress = (string)spR["goalprogress"],
                    recommendation = (string)spR["goalrecommendations"],
                }).ToList();
            }

            r.goalAreas = ds.Tables[3].Rows.Cast<DataRow>().Select(spR => new Option()
            {
                value = spR["goalAreaId"].ToString(),
                name = (string)spR["name"]
            }).ToList();

            r.frequencies = ds.Tables[4].Rows.Cast<DataRow>().Select(spR => new Option()
            {
                value = spR["frequencyId"].ToString(),
                name = (string)spR["name"]
            }).ToList();

            r.statuses = ds.Tables[5].Rows.Cast<DataRow>().Select(spR => new Option()
            {
                value = (string)spR["name"],
                name = (string)spR["name"]
            }).ToList();

            r.durations = new List<Option>();
            for (int i = 12; i > 0; i--)
            {
                Option o = new Option
                {
                    value = i.ToString(),
                    name = i + " Months"
                };
                r.durations.Add(o);

            }
            ds.Dispose();
        }

        private void getTherapyProgressReportPdf(string docId, ref ProgressReportPdf r)
        {

            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_TaskGetTherapyProgressReportPdf", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                    cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                    cmd.Parameters.AddWithValue("@coId", UserClaim.coid);
                    cmd.Parameters.AddWithValue("@progressTherapyId", docId);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
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
                    DataRow dr = ds.Tables[5].Rows[0];
                    r.agency = dr["name"].ToString();
                    r.agencyPhone = dr["SkilledBillingPhone"].ToString();



                    dr = ds.Tables[0].Rows[0];
                    r.completedBy = dr["completedByFn"] + " " + dr["completedByLn"];
                    r.completedByCredentials = dr["completedByTitle"] + ((string)dr["completedByNpi"] != "" ? " (NPI: " + dr["completedByNpi"] + ")" : "");
                    r.approvedBy = dr["approvedByFn"] + " " + dr["approvedByLn"];
                    r.approvedByCredentials = dr["approvedByTitle"] + ((string)dr["approvedByNpi"] != "" ? " (NPI: " + dr["approvedByNpi"] + ")" : "");
                    r.reportingPeriod = (string)dr["ReportingPeriod"];
                    r.agency = UserClaim.companyName;
                   
                    r.npi = UserClaim.npi;
                    r.clientName = (string)dr["cnm"];
                    r.svc = (string)dr["svc"];
                    r.serviceName = (string)dr["serviceName"];
                    r.dt = ((DateTime)dr["startDate"]).ToShortDateString() + " to " + ((DateTime)dr["endDate"]).ToShortDateString();
                    r.clsvId = (int)dr["clsvId"];
                    r.serviceId = (int)dr["serviceId"];
                    r.docId = (int)dr["progressTherapyId"];
                    r.formType = "TherapyProgressReport";
                    r.hasAttachment = (bool)dr["hasAttachment"];
                    r.attachmentName = dr["attachmentName"].ToString();
                    r.completedDt = DateTimeLocal().ToShortDateString();
                    r.goalsToAdd = (string)dr["goalsToAdd"];
                    //     r.treatmentDurationId = (byte)dr["treatmentDurationId"];
                    //      r.treatmentFrequency = (string)dr["treatmentFrequency"];

                    r.extension = dr["fileExtension"] == DBNull.Value ? "" : (string)dr["fileExtension"];
                    r.dob = dr["dob"] == DBNull.Value ? "" : ((DateTime)dr["dob"]).ToShortDateString();

                    r.diagnosis = (string)dr["dddDiagnosis"];
                    r.physician = dr["physicianTitle"] + " " + dr["physicianFirstName"] + " " + dr["physicianMI"] + " " + dr["physicianLastName"] + " " + dr["physicianSuffix"];
                    r.physicianAgency = (string)dr["physicianAgency"];
                    r.physicianPhone = (string)dr["physicianTelephone"];


                    r.clId = dr["clId"] == DBNull.Value ? "" : (string)dr["clId"];
                    r.clientWorker = dr["clwNm"] == DBNull.Value ? "" : (string)dr["clwNm"];
                    r.therapySupervisor = dr["approvedByFn"] + " " + dr["approvedByLn"];
                    r.therapySupervisorTitle = dr["approvedByTitle"].ToString();


                    r.therapySupervisorPhone = (string)dr["approvedByphone"];

                    if ((bool)dr["selfResponsible"])
                        r.responsiblePerson = "Self Responsible";
                    else
                    {
                        r.responsiblePerson = (string)dr["responsiblePerson"];
                        r.responsiblePersonPhone = (string)dr["responsiblePersonTelephone"];
                        r.responsiblePersonRelationship = (string)dr["responsiblePersonRelationship"];
                    }

                    r.fileIdentifier = dr["attachmentName"].ToString();

                    r.year = ((DateTime)dr["endDate"]).Year;
                    r.month = ((DateTime)dr["endDate"]).ToString("MM");

                    DateTime Current = DateTimeLocal(DateTime.UtcNow);
                    r.currentDate = Current.ToShortDateString();
                    r.currentTime= Current.ToShortTimeString();
                    //    DataView GoalScores = new DataView(ds.Tables[4]);

                    r.providers = new List<Provider>();

                    DataView Sessions = new DataView(ds.Tables[4]);
                    DataTable providerList = Sessions.ToTable(true, "fn", "ln", "title", "npi", "providerPhone");
                    foreach (DataRow providerRow in providerList.Rows)
                    {
                        if ((string)providerRow["fn"] != (string)dr["approvedByFn"] && (string)providerRow["ln"] != (string)dr["approvedByln"])
                        {
                            Provider p = new Provider
                            {
                                name = providerRow["ln"] + ", " + providerRow["fn"],
                                title = providerRow["title"].ToString(),
                                phone = (string)providerRow["providerPhone"]

                            };
                            r.providers.Add(p);

                        }
                    }

                    providerList.Dispose();


                    r.appointments = ds.Tables[4].Rows.Cast<DataRow>().Select(spR => new Appointment()
                    {
                        date = ((DateTime)spR["dt"]).ToShortDateString(),
                        time = DateTimeLocal(((DateTime)spR["startAt"])).ToShortTimeString(),
                        title = (string)spR["Title"],
                        provider = (string)spR["ln"] + " " + (string)spR["fn"],
                        status = (string)spR["status"],
                        teletherapy = (bool)spR["teletherapy"],
                        locationTypeId = Convert.ToInt32(spR["locationTypeId"]),
                        location = Convert.ToInt32(spR["locationTypeId"]) == 1 ? "Client Home" : (Convert.ToInt32(spR["locationTypeId"]) == 2 ? "Provider Home" : "Clinic"),
                        ratio = Convert.ToInt32(spR["ratio"])


                    }).ToList();

                    bool clinicSetting = false;
                    bool clientHomeSetting = false;
                    bool providerHomeSetting = false;
                    bool groupFormat = false;
                    bool individualFormat = false;
                    foreach (var item in r.appointments)
                    {
                        if (item.locationTypeId == 1)
                            clientHomeSetting = true;
                        else if (item.locationTypeId == 2)
                            providerHomeSetting = true;
                        else
                            clinicSetting = true;
                        if (item.ratio == 1)
                            individualFormat = true;
                        else 
                            groupFormat = true;

                    }
                    if (clientHomeSetting)
                        r.setting = "Client Home";
                    if (providerHomeSetting || clinicSetting)
                    {
                        if (r.setting == null)
                            r.setting = "Clinic/Office";
                        else
                            r.setting += "/Clinic/Office";

                    }
                    if (individualFormat)
                        r.format = "Individual";
                    if (groupFormat)
                    {
                        if (r.format == null)
                            r.setting = "Group";
                        else
                            r.setting += "/Group";

                    }


                    // Clinic / OfficeSetting
                    r.sessionCount = r.appointments.Count;


                    r.questions = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new Question()
                    {
                        questionId = (int)spR["questionId"],
                        title = ConvertToHtml((string)spR["title"]),
                        answer = ConvertToHtml((string)spR["answer"]),
                        isRequired = (bool)spR["isRequired"]
                    }).ToList();


                    DataView dv = new DataView(ds.Tables[2]);

                    char ObjIndex = '1';
                    r.longTermObjectives = dv.ToTable(true, "objectiveId", "longTermVision", "longTermGoal", "goalAreaId", "goalAreaName", "objectiveStatus", "changes").Rows.Cast<DataRow>().Select(spR => new LongTermObjective()
                    {
                        objIndex = ObjIndex++ + ".",
                        objectiveId = (int)spR["objectiveId"],
                        longTermVision = ConvertToHtml((string)spR["longTermVision"]),
                        longTermGoal = ConvertToHtml((string)spR["longTermGoal"]),
                        goalAreaId = (int)spR["goalAreaId"],
                        goalAreaName = spR["goalAreaName"].ToString(),
                        objectiveStatus = (string)spR["objectiveStatus"],
                        changes = (string)spR["changes"]
                    }).ToList();


                    foreach (LongTermObjective o in r.longTermObjectives)
                    {
                        dv.RowFilter = "objectiveId=" + o.objectiveId + " AND goalId IS NOT NULL";
                        char GoalIndex = 'A';
                        o.shortTermGoals = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new ShortTermGoal()
                        {
                            goalIndex = o.objIndex +  GoalIndex++,
                            goalId = (int)spR["goalId"],
                            step = (short)spR["step"],
                            shortTermGoal = ConvertToHtml((string)spR["shortTermGoal"]),
                            teachingMethod = ConvertToHtml((string)spR["teachingMethod"]),
                            goalStatus = (string)spR["goalStatus"],
                            frequencyId = Convert.ToString(spR["frequencyId"]),
                            frequency = (string)spR["frequency"],
                            progress = ConvertToHtml((string)spR["goalprogress"]),
                            recommendation = (string)spR["goalrecommendations"],
                        }).ToList();
                       
                    }


                    r.scoring = ds.Tables[3].Rows.Cast<DataRow>().Select(spR => new Option()
                    {
                        value = ((string)spR["scoreValue"]).Substring(0, 1),
                        name = (string)spR["scoreName"]
                    }).ToList();



                }
                catch (Exception ex)
                {
                    r.er.code = 1;
                    r.er.msg = ex.Message;
                }

            }
            ds.Dispose();
        }

        private void getTherapyEvaluationPdf(string docId, ref ProgressReportPdf r)
        {
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_TaskGetTherapyEvalNotePdf", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.AddWithValue("@clientSessionTherapyId", docId);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
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
                    DataRow dr = ds.Tables[0].Rows[0];
                    r.completedBy = dr["completedByFn"] + " " + dr["completedByLn"];
                    r.completedByCredentials = dr["completedByTitle"] + ((string)dr["completedByNpi"] != "" ? " (NPI: " + dr["completedByNpi"] + ")" : "");
                    r.completedByTitle = (string)dr["completedByTitle"];
                    r.completedByPhone = (string)dr["completedByPhone"];

                    r.agency = UserClaim.companyName;
                    r.npi = UserClaim.npi;
                    r.clientName = (string)dr["cnm"];
                    r.svc = (string)dr["svc"];
                    r.serviceName = (string)dr["serviceName"];
                    r.dt = ((DateTime)dr["dt"]).ToShortDateString();
                    r.clsvId = (int)dr["clsvId"];
                    r.serviceId = (int)dr["serviceId"];


                    r.treatmentFrequency = (string)dr["treatmentFrequency"];
                    r.numberOfVisits = Convert.ToInt32(dr["numberOfVisits"]);
                    r.treatmentDuration = Convert.ToString(dr["treatmentDurationId"]);



                    r.dob = dr["dob"] == DBNull.Value ? "" : ((DateTime)dr["dob"]).ToShortDateString();

                    r.diagnosis = (string)dr["dddDiagnosis"];
                    r.physician = dr["physicianTitle"] + " " + dr["physicianFirstName"] + " " + dr["physicianMI"] + " " + dr["physicianLastName"] + " " + dr["physicianSuffix"];
                    r.physicianAgency = (string)dr["physicianAgency"];
                    r.physicianPhone = (string)dr["physicianTelephone"];

                    r.clId = dr["clId"] == DBNull.Value ? "" : (string)dr["clId"];
                    r.clientWorker = dr["clwNm"] == DBNull.Value ? "" : (string)dr["clwNm"];



                    if ((bool)dr["selfResponsible"])
                        r.responsiblePerson = "Self Responsible";
                    else
                    {
                        r.responsiblePerson = (string)dr["responsiblePerson"];
                        r.responsiblePersonPhone = (string)dr["responsiblePersonTelephone"];
                        r.responsiblePersonRelationship = (string)dr["responsiblePersonRelationship"];
                    }



                    r.year = ((DateTime)dr["dt"]).Year;
                    r.month = ((DateTime)dr["dt"]).ToString("MM");

                    r.fileIdentifier = dr["attachmentName"].ToString();



                    r.questions = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new Question()
                    {
                        questionId = (int)spR["questionId"],
                        title = ConvertToHtml((string)spR["title"]),
                        answer = ConvertToHtml((string)spR["answer"]),
                        isRequired = (bool)spR["isRequired"]
                    }).ToList();

                 
                    DataView dv = new DataView(ds.Tables[2]);
                    char ObjIndex = 'A';
                    r.longTermObjectives = dv.ToTable(true, "objectiveId", "longTermVision", "longTermGoal", "goalAreaId", "goalAreaName", "objectiveStatus").Rows.Cast<DataRow>().Select(spR => new LongTermObjective()
                    {
                        objIndex = ObjIndex++ + ".",
                        objectiveId = (int)spR["objectiveId"],
                        longTermVision = ConvertToHtml((string)spR["longTermVision"]),
                        longTermGoal = ConvertToHtml((string)spR["longTermGoal"]),
                        goalAreaId = (int)spR["goalAreaId"],
                        goalAreaName = spR["goalAreaName"].ToString(),
                        objectiveStatus = (string)spR["objectiveStatus"]
                    }).ToList();


                    foreach (LongTermObjective o in r.longTermObjectives)
                    {
                        dv.RowFilter = "objectiveId=" + o.objectiveId + " AND goalId IS NOT NULL";
                        char GoalIndex = 'A';
                        o.shortTermGoals = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new ShortTermGoal()
                        {
                            goalIndex = o.objIndex + GoalIndex++,
                            goalId = (int)spR["goalId"],
                            step = (short)spR["step"],
                            shortTermGoal = ConvertToHtml((string)spR["shortTermGoal"]),
                            teachingMethod = ConvertToHtml((string)spR["teachingMethod"]),
                            goalStatus = (string)spR["goalStatus"],
                            frequencyId = Convert.ToString(spR["frequencyId"]),
                            frequency = (string)spR["frequency"]
                        }).ToList();


                    }

                }
                catch (Exception ex)
                {
                    r.er.code = 1;
                    r.er.msg = ex.Message;
                }

            }
            ds.Dispose();
        }

        private void getTherapyPlanOfCarePdf(string docId, ref ProgressReportPdf r)
        {
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_TaskGetTherapyPlanOfCarePdf", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                    cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                    cmd.Parameters.AddWithValue("@planOfCareId", docId);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
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
                    DataRow dr = ds.Tables[0].Rows[0];
                    r.completedBy = dr["completedByFn"] + " " + dr["completedByLn"];
                    r.completedByCredentials = dr["completedByNpi"] == DBNull.Value ? "" : (string)dr["completedByNpi"];
                    r.completedByTitle = (string)dr["completedByTitle"];
                    r.completedByPhone = (string)dr["completedByPhone"];
                    r.completedByEmail = (string)dr["completedByEmail"];
                    r.completedDt = DateTimeLocal().ToShortDateString();
                    r.agency = UserClaim.companyName;
                    r.npi = UserClaim.npi;
                    r.clientName = (string)dr["cnm"];

                    r.serviceName = (string)dr["serviceName"];
                    r.svc = (string)dr["svc"];
                    r.serviceStartDate = dr["serviceStartDate"] == DBNull.Value ? "" : ((DateTime)dr["serviceStartDate"]).ToShortDateString();

                    r.treatmentStart = dr["treatmentStart"] == DBNull.Value ? "" : ((DateTime)dr["treatmentStart"]).ToShortDateString();
                    r.treatmentEnd = dr["treatmentEnd"] == DBNull.Value ? "" : ((DateTime)dr["treatmentEnd"]).ToShortDateString();
                    r.numberOfVisits = Convert.ToInt32(dr["numberOfVisits"]);

                    r.treatmentFrequency = (string)dr["treatmentFrequency"];

                    r.duration = Convert.ToInt32(dr["treatmentDurationId"]);

                    r.dob = dr["dob"] == DBNull.Value ? "" : ((DateTime)dr["dob"]).ToShortDateString();
                    r.diagnosis = (string)dr["dddDiagnosis"];
                    
                    
                    
                    r.physician = dr["physicianTitle"] + " " + dr["physicianFirstName"] + " " + dr["physicianMI"] + " " + dr["physicianLastName"] + " " + dr["physicianSuffix"];
                    r.physicianAgency = (string)dr["physicianAgency"];
                    r.physicianPhone = (string)dr["physicianTelephone"];
                    r.physicianNpi = (string)dr["physicianNpi"];
                    r.physicianEmail = (string)dr["physicianEmail"];
                    r.clId = dr["clId"] == DBNull.Value ? "" : (string)dr["clId"];
                    r.clientWorker = dr["clwNm"] == DBNull.Value ? "" : (string)dr["clwNm"];

                    r.year = ((DateTime)dr["dt"]).Year;
                    r.month = ((DateTime)dr["dt"]).ToString("MM");
                    r.fileIdentifier = dr["fileIdentifier"].ToString();

                    r.currentDate = DateTimeLocal().ToShortDateString();
                    r.currentTime = DateTimeLocal().ToShortTimeString();



                    if ((bool)dr["selfResponsible"])
                        r.responsiblePerson = "Self Responsible";
                    else
                    {
                        r.responsiblePerson = (string)dr["responsiblePerson"];
                        r.responsiblePersonPhone = (string)dr["responsiblePersonTelephone"];
                        r.responsiblePersonRelationship = (string)dr["responsiblePersonRelationship"];
                    }


                    r.questions = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new Question()
                    {
                        questionId = (int)spR["questionId"],
                        title = ConvertToHtml((string)spR["title"]),
                        answer = ConvertToHtml((string)spR["answer"]),
                        isRequired = (bool)spR["isRequired"]
                    }).ToList();


                    DataView dv = new DataView(ds.Tables[2]);
                    char ObjIndex = '1';
                    r.longTermObjectives = dv.ToTable(true, "objectiveId", "longTermVision", "longTermGoal", "goalAreaId","goalAreaName", "objectiveStatus").Rows.Cast<DataRow>().Select(spR => new LongTermObjective()
                    {
                        objIndex = ObjIndex++ +".",
                        objectiveId = (int)spR["objectiveId"],
                        longTermVision = ConvertToHtml((string)spR["longTermVision"]),
                        longTermGoal = ConvertToHtml((string)spR["longTermGoal"]),
                        goalAreaId = (int)spR["goalAreaId"],
                        goalAreaName  = spR["goalAreaName"].ToString(),
                        objectiveStatus = (string)spR["objectiveStatus"],
                    }).ToList();


                    foreach (LongTermObjective o in r.longTermObjectives)
                    {
                        dv.RowFilter = "objectiveId=" + o.objectiveId + " AND goalId IS NOT NULL";
                        char  GoalIndex = 'A';
                        o.shortTermGoals = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new ShortTermGoal()
                        {
                            goalIndex = o.objIndex + GoalIndex++,
                            goalId = (int)spR["goalId"],
                            step = (short)spR["step"],
                            shortTermGoal = ConvertToHtml((string)spR["shortTermGoal"]),
                            teachingMethod = ConvertToHtml((string)spR["teachingMethod"]),
                            goalStatus = (string)spR["goalStatus"],

                            frequency = (string)spR["frequency"],

                        }).ToList();
                    }
                    if (ds.Tables[3].Rows.Count != 0)
                        r.lastEvaluationDate = ((DateTime)ds.Tables[3].Rows[0].ItemArray[0]).ToShortDateString();

                }
                catch (Exception ex)
                {
                    r.er.code = 1;
                    r.er.msg = ex.Message;
                }

            }
            ds.Dispose();
        }


        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> GetHahProgressReport(string docId, bool approval)
        {
            HabProgressReport r = new HabProgressReport();

            r.docId = Convert.ToInt32(docId);
            DataSet ds = new DataSet();
            r.approval = approval;
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskGetHAHProgressReport", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@progressHabId", docId);
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

                    if (ds.Tables[0].Rows.Count != 0)
                    {
                        DataRow dr = ds.Tables[0].Rows[0];
                        r.client = (string)dr["client"];
                        r.provider = (string)dr["provider"];

                        r.service = (string)dr["svc"];
                        r.sequenceNumber = (short)dr["sequenceNumber"];
                      
                        r.year = (int)dr["year"];
                        r.startDate = ((DateTime)dr["startDate"]).ToShortDateString();
                        r.endDate = ((DateTime)dr["endDate"]).ToShortDateString();
                        r.assistId = (string)dr["assistId"];

                        r.progressReportGoals = new ProgressReportGoal[ds.Tables[1].Rows.Count];
                        int i = 0;
                        foreach (DataRow goalRow in ds.Tables[1].Rows)
                        {

                            r.progressReportGoals[i] = new ProgressReportGoal();
                            r.progressReportGoals[i].objectiveId = (int)goalRow["objectiveId"];
                            r.progressReportGoals[i].goalId = (int)goalRow["goalId"];
                            r.progressReportGoals[i].teachingMethod = (string)goalRow["teachingMethod"];
                            r.progressReportGoals[i].goal = (string)goalRow["shortTermGoal"];
                            r.progressReportGoals[i].note = goalRow["note"] == DBNull.Value ? "" : (string)goalRow["note"];
                            r.progressReportGoals[i].monthlyScores = new MonthlyScores[ds.Tables[2].Rows.Count];
                            int j = 0;
                            foreach (DataRow monthRow in ds.Tables[2].Rows)
                            {
                                r.progressReportGoals[i].monthlyScores[j] = new MonthlyScores();
                                r.progressReportGoals[i].monthlyScores[j].month = (string)monthRow["month"];
                                r.progressReportGoals[i].monthlyScores[j].monthNumber = (int)monthRow["monthNumber"];

                                DataView scores = new DataView(ds.Tables[3]);

                                scores.RowFilter = "goalId=" + r.progressReportGoals[i].goalId + " AND monthNumber=" + r.progressReportGoals[i].monthlyScores[j].monthNumber;
                                foreach (DataRowView drv in scores) 
                                {
                                    r.progressReportGoals[i].monthlyScores[j].score[(int)drv["day"] - 1] = (string)drv["score"];
                                }
                                scores.Dispose();
                                j++;
                            }
                            i++;
                        }

                    }
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
                Response.StatusCode = 500;
                return null;
            }

            return PartialView("ModalProgressReportHabilitation", r);
        }




  

        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> SetHAHProgressReport(HabProgressReport pr)
        {
            HomeStaffPage r = new HomeStaffPage();
            Windows w = new Windows();
            DataSet ds = new DataSet();

            bool autoApproval = false;
            if (UserClaim.userLevel != "Provider")
                autoApproval = true;


            try
            {
                DataTable dt = new DataTable();
                dt.Clear();
                dt.Columns.Add("objectiveId");
                dt.Columns.Add("goalId");
                dt.Columns.Add("note");

                if (pr.progressReportGoals != null)
                {
                    for (int i = 0; i < pr.progressReportGoals.Length; i++)
                    {
                        DataRow nRow = dt.NewRow();
                        nRow["objectiveId"] = pr.progressReportGoals[i].objectiveId;
                        nRow["goalId"] = pr.progressReportGoals[i].goalId;
                        nRow["note"] = pr.progressReportGoals[i].note;
                        dt.Rows.Add(nRow);
                    }
                }

                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskSetHAHProgressReport", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@prId", pr.providerId);
                        cmd.Parameters.AddWithValue("@progressHabId", pr.docId);
                        cmd.Parameters.AddWithValue("@goalNotes", dt);
                        cmd.Parameters.AddWithValue("@completedDt", DateTimeLocal().ToShortDateString());
                        cmd.Parameters.AddWithValue("@autoApproval", autoApproval);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                if (autoApproval)
                {
                    ds = new DataSet();
                    try
                    {
                        string sequenceId;
                        if (pr.sequenceNumber >= 10)
                            sequenceId = "0" + pr.sequenceNumber;
                        else
                            sequenceId = "00" + pr.sequenceNumber;

                        string fileName = "";

                        fileName = await saveHAHProgressReport(Convert.ToString(pr.docId), sequenceId);
  
                        await Task.Run(() =>
                        {
                            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                            {
                                SqlCommand cmd = new SqlCommand("sp_TaskSetHAHProgressReportVerification", cn)
                                {
                                    CommandType = CommandType.StoredProcedure
                                };
                                cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                                cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                                cmd.Parameters.AddWithValue("@progressId", pr.docId);
                                cmd.Parameters.AddWithValue("@verifiedDt", DateTimeLocal().ToShortDateString());
                                cmd.Parameters.AddWithValue("@prId", pr.providerId);
                                cmd.Parameters.AddWithValue("@fileName", fileName);
                                cmd.Parameters.AddWithValue("@rejected", false);
                                cmd.Parameters.AddWithValue("@rejectedreason", "");
                                cmd.Parameters.AddWithValue("@completed", true);
                                cmd.Parameters.AddWithValue("@verified", true);
                                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                            }
                        });
                    
                    }
                    catch (Exception ex)
                    {
                        w.er.code = 1;
                        w.er.msg = ex.Message;
                    }





                }

                r.pendingDocumentation = setPendingDocumentation(ref ds, 0);
                r.staffAlerts = setStaffAlerts(ref ds, 1);
                w.pendingDocumentation = RenderRazorViewToString("PendingDocumentation", r);
                w.staffAlerts = RenderRazorViewToString("StaffAlerts", r.staffAlerts);
            }
            catch (Exception ex)
            {
                w.er.code = 1;
                w.er.msg = ex.Message;
            }
            ds.Dispose();

            if (w.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 500;
                return null;
            }
            else
            {
                var jsonResult = Json(w);
                jsonResult.MaxJsonLength = int.MaxValue;
                return jsonResult;
            }
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SetHAHProgressReportApproval(NoteAcceptance c)
        {
            HomeStaffPage r = new HomeStaffPage();
            Windows w = new Windows();
            bool completed;
            bool verified;
            if (c.rejected)
            {
                completed = false;
                verified = false;
            }
            else
            {
                completed = true;
                verified = true;
                c.rejectedReason = "";
            }

            DataSet ds = new DataSet();
            try
            {
                string fileName = "";
                if (verified)
                {
                    string sequenceId;
                    if (c.sequenceNumber >= 10)
                        sequenceId = "0" + c.sequenceNumber;
                    else
                        sequenceId = "00" + c.sequenceNumber;

                    fileName = await saveHAHProgressReport(Convert.ToString(c.docId), sequenceId);
                }

                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskSetHAHProgressReportVerification", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@progressId", c.docId);
                        cmd.Parameters.AddWithValue("@verifiedDt", DateTimeLocal().ToShortDateString());
                        cmd.Parameters.AddWithValue("@prId", c.providerId);
                        cmd.Parameters.AddWithValue("@fileName", fileName);
                        cmd.Parameters.AddWithValue("@rejected", c.rejected);
                        cmd.Parameters.AddWithValue("@rejectedreason", c.rejectedReason);
                        cmd.Parameters.AddWithValue("@completed", completed);
                        cmd.Parameters.AddWithValue("@verified", verified);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.pendingDocumentation = setPendingDocumentation(ref ds, 0);
                r.staffAlerts = setStaffAlerts(ref ds, 1);
                w.pendingDocumentation = RenderRazorViewToString("PendingDocumentation", r);
                w.staffAlerts = RenderRazorViewToString("StaffAlerts", r.staffAlerts);

            }
            catch (Exception ex)
            {
                w.er.code = 1;
                w.er.msg = ex.Message;
            }

            ds.Dispose();
            var jsonResult = Json(w);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
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
            Response.AddHeader("Content-Disposition", "attachment;filename=" + fileName);
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
                    i++;
                }


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
            if (er.code == 0)
            {

                Response.AddHeader("Content-Disposition", "attachment;filename=" + filePath);
                return new FileContentResult(ms.ToArray(), "application/pdf");

            }
            else
            {
                Response.Write(er.msg);
                Response.StatusCode = 500;
                return null;
            }

        }

        private async Task<string> saveHAHProgressReport(string docId, string sequenceNumber)
        {




            string fileName = "";

            HabProgressReport r = new HabProgressReport();
            DataSet ds = new DataSet();
            await Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_TaskGetHAHProgressReport", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@coId", UserClaim.coid);
                    cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                    cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                    cmd.Parameters.AddWithValue("@progressHabId", docId);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
            });
           
            if (ds.Tables[0].Rows.Count != 0)
            {
                DataRow dr = ds.Tables[0].Rows[0];
                fileName = "DDDProgressReport_" + dr["year"] + "_" + dr["monthnumber"] + "_" + UserClaim.pcode + "_" + dr["assistId"] + "_" + dr["svc"] + "_" + sequenceNumber + ".pdf";
              
                r.companyName= 
                
                r.client = (string)dr["client"];
                r.provider = (string)dr["provider"];

                r.service = (string)dr["svc"];
                r.sequenceNumber = (short)dr["sequenceNumber"];

                r.year = (int)dr["year"];
                r.startDate = ((DateTime)dr["startDate"]).ToShortDateString();
                r.endDate = ((DateTime)dr["endDate"]).ToShortDateString();
                r.assistId = (string)dr["assistId"];
                r.clientWorker = dr["clwNm"] != DBNull.Value ? (string)dr["clwNm"] : "";
                r.dob = dr["dob"] != DBNull.Value ? ((DateTime)dr["dob"]).ToShortDateString() : "";

                dr = ds.Tables[5].Rows[0];
                r.companyName = (string)dr["name"];


               r.scoringKeys = ds.Tables[4].Rows.Cast<DataRow>().Select(spR => new ScoringKey()
                {
                    key = ((string)spR["scoreValue"]).Substring(0,1),
                    name = (string)spR["scoreName"],
                }).ToList();

                r.progressReportGoals = new ProgressReportGoal[ds.Tables[1].Rows.Count];
                int i = 0;
                foreach (DataRow goalRow in ds.Tables[1].Rows)
                {

                    r.progressReportGoals[i] = new ProgressReportGoal();
                    r.progressReportGoals[i].objectiveId = (int)goalRow["objectiveId"];
                    r.progressReportGoals[i].goalId = (int)goalRow["goalId"];
                    r.progressReportGoals[i].shortTermGoal = ConvertToHtml((string)goalRow["shortTermGoal"]);
                    r.progressReportGoals[i].teachingMethod = ConvertToHtml((string)goalRow["teachingMethod"]);
                    r.progressReportGoals[i].goal = ConvertToHtml((string)goalRow["shortTermGoal"]);
                    r.progressReportGoals[i].note = goalRow["note"] == DBNull.Value ? "" : ConvertToHtml((string)goalRow["note"]);
                    r.progressReportGoals[i].monthlyScores = new MonthlyScores[ds.Tables[2].Rows.Count];
                    int j = 0;
                    foreach (DataRow monthRow in ds.Tables[2].Rows)
                    {
                        r.progressReportGoals[i].monthlyScores[j] = new MonthlyScores();
                        r.progressReportGoals[i].monthlyScores[j].month = (string)monthRow["month"];
                        r.progressReportGoals[i].monthlyScores[j].monthNumber = (int)monthRow["monthNumber"];

                        DataView scores = new DataView(ds.Tables[3]);

                        scores.RowFilter = "goalId=" + r.progressReportGoals[i].goalId + " AND monthNumber=" + r.progressReportGoals[i].monthlyScores[j].monthNumber;
                        foreach (DataRowView drv in scores)
                        {
                            r.progressReportGoals[i].monthlyScores[j].score[(int)drv["day"] - 1] = ((string)drv["score"]).Substring(0,1);
                        }
                        scores.Dispose();
                        j++;
                    }
                    i++;
                }

            }




            PdfGenerateConfig config = new PdfGenerateConfig();
            config.PageSize = PageSize.Letter;
            config.MarginBottom = 90;
            config.MarginTop = 40;
            config.MarginRight = 30;
            config.MarginLeft = 30;
            config.PageOrientation = PageOrientation.Portrait;
            PdfDocument pdf = null;

                pdf = PdfGenerator.GeneratePdf(RenderRazorViewToString("Document_ProgressReportHabilitation", r), config);
           
                
                
             XFont A8 = new XFont("Arial", 8);
            XRect rect = new XRect(20, 31, 100, 13);
            /*
            for (int i = 0; i < pdf.PageCount; i++)
            {
                string footer = rpr.clientName + " (" + rpr.dt + ") Page " + (i + 1) + " of " + pdf.PageCount;
                PdfPage page = pdf.Pages[i];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XTextFormatter tf = new XTextFormatter(gfx);
                rect.X = 150;
                rect.Y = 750;
                rect.Width = 300;
                rect.Height = 10;
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(footer, A8, XBrushes.DarkSlateGray, rect, XStringFormats.TopLeft);
            }
            */
            using (MemoryStream ms = new MemoryStream())
            {
                pdf.Save(ms, false);
                ms.Seek(0, SeekOrigin.Begin);
                FileData f = new FileData("progressreports", UserClaim.blobStorage);
                f.StoreFile(ms.ToArray(), fileName);
            }
            return fileName;

        }



        
    
        private static int InsertSharedStringItem(string text, SharedStringTablePart shareStringPart)
        {
            // If the part does not contain a SharedStringTable, create one.
            if (shareStringPart.SharedStringTable == null)
            {
                shareStringPart.SharedStringTable = new SharedStringTable();
            }

            int i = 0;

            // Iterate through all the items in the SharedStringTable. If the text already exists, return its index.
            foreach (SharedStringItem item in shareStringPart.SharedStringTable.Elements<SharedStringItem>())
            {
                if (item.InnerText == text)
                {
                    return i;
                }

                i++;
            }

            // The text does not exist in the part. Create the SharedStringItem and return its index.
            shareStringPart.SharedStringTable.AppendChild(new SharedStringItem(new DocumentFormat.OpenXml.Spreadsheet.Text(text)));
            shareStringPart.SharedStringTable.Save();

            return i;
        }

        private static WorksheetPart GetWorksheetPartByName(SpreadsheetDocument document, string sheetName)
        {
            IEnumerable<Sheet> sheets = document.WorkbookPart.Workbook.GetFirstChild<Sheets>().Elements<Sheet>().Where(s => s.Name == sheetName);

            if (sheets.Count() == 0)
            {
                // The specified worksheet does not exist.

                return null;
            }

            string relationshipId = sheets.First().Id.Value;
            WorksheetPart worksheetPart = (WorksheetPart)
                 document.WorkbookPart.GetPartById(relationshipId);
            return worksheetPart;

        }
        private static Cell GetCell(Worksheet worksheet, string columnName, uint rowIndex)
        {
            DocumentFormat.OpenXml.Spreadsheet.Row row = GetRow(worksheet, rowIndex);

            if (row == null)
                return null;

            return row.Elements<Cell>().Where(c => string.Compare
                   (c.CellReference.Value, columnName +
                   rowIndex, true) == 0).First();
        }

        private static DocumentFormat.OpenXml.Spreadsheet.Row GetRow(Worksheet worksheet, uint rowIndex)
        {
           
            return worksheet.GetFirstChild<SheetData>().
              Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().Where(r => r.RowIndex == rowIndex).First();
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
        /*
        private List<ClientAlert> GetPreAuthAlerts()
        {
            var alerts = new List<ClientAlert>();
            DataTable dt = new DataTable();

            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_GetPreAuthAlerts", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                sqlHelper.ExecuteSqlDataAdapter(cmd, dt);

                if(dt.Rows.Count > 0)
                {
                    alerts = dt.Rows.Cast<DataRow>().Select(x => new ClientAlert()
                    {
                        alert = x.GetValueOrDefault<string>("Alert"),
                        id = x.GetValueOrDefault<Int32>("ClientId"),
                        name = x.GetValueOrDefault<string>("Name"),
                        priority = x.GetValueOrDefault<Int32>("Priority"),
                        clwEm = x.GetValueOrDefault<string>("Email"),
                        clwNm = x.GetValueOrDefault<string>("ClientName"),
                        clwPh = x.GetValueOrDefault<string>("Phone")
                    }).ToList();
                }
            }
            return alerts;
        }
        */
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

        private decimal calulateUnits15MinRule(DateTime End, DateTime Start)
        {

            TimeSpan timespan = (End - Start);
            decimal units = Convert.ToDecimal(timespan.Hours);
            double minutes = timespan.Minutes;
            if (minutes >= 53)
                units += 1;
            else if (minutes >= 38)
                units += 0.75M;
            else if (minutes >= 23)
                units += 0.5M;
            else if (minutes >= 8)
                units += 0.25M;
            if (units < 0)
                units = 0;
            return units;

        }




    }
}