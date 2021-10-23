using DCC.Models.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using DCC.Helpers;
using DCC.Models;
using DCC.SQLHelpers.Helpers;
using DCCHelper;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;

namespace DCC.Controllers
{
    public class AccountsReceivableController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;
        public AccountsReceivableController()
        {
            sqlHelper = new SQLHelper();
        }

        [Authorize]
        public async Task<ActionResult> Index()
        {
            ProviderInit r = new ProviderInit();

            DataSet ds = null;
            DataSet ds1 = null;
            DataSet ds2 = null;

            try
            {
                //alphabetic order
                ds = await getInsuranceList();
                ds1 = await getClaimStatusList();
                ds2 = await getClientList();

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
                }).ToList();
                r.providerList.Add(new Provider { payerId = "ABCDE", name = "DDD", insuranceCompanyId = 3010 });
            }

            r.claimStatusList = ds1.Tables[0].Rows.Cast<DataRow>().Select(spR => new ClaimsStatus()
            {
                claimstatusid = int.Parse(spR["ClaimStatusid"].ToString()),
                name = spR["Name"] == DBNull.Value ? "" : (string)spR["Name"],
                statusType = spR["statusType"].ToString()
            }).Where(x => x.claimstatusid != 5 && x.claimstatusid != 6).ToList();
            r.claimStatusList.Insert(0, new ClaimsStatus() { claimstatusid = -1, name = "All Statuses" });

            r.clientInfoList = ds2.Tables[0].Rows.Cast<DataRow>().Select(spR => new ClientInfo()
            {
                clientId = (int)spR["clsvid"],
                name = (string)spR["nm"]
            }).ToList();

            setViewModelBase((ViewModelBase)r);
            return View(r);
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> GetClaimsCount(string clientId, string insuranceCompanyId, int claimStatusId, string asOfDate)
        {
            AccountsReceivable AR = new AccountsReceivable();
            DataSet ds = await getARClaimsCount(clientId, insuranceCompanyId, claimStatusId, asOfDate);
            AR.ArChart = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new ARChart()
            {
                AgingBucket = Convert.ToString(spR["AgingBucket"]),
                ClaimCount = spR["ClaimCount"] == DBNull.Value ? 0 : Convert.ToInt32(spR["ClaimCount"]),
                Percentage = spR["Percentage"] == DBNull.Value ? 0 : Convert.ToDecimal(spR["Percentage"]),
                ClaimAmount = spR["Amount"] == DBNull.Value ? 0 : Convert.ToDecimal(spR["Amount"]),
                ClaimAgeRange = spR["ClaimAgeRange"] == DBNull.Value ? 0 : Convert.ToInt32(spR["ClaimAgeRange"]),
            }).ToList();

            return PartialView("Chart", AR);
        }

        private Task<DataSet> getARClaimsCount(string clientId, string insuranceCompanyId, int claimStatusId, string asOfDate)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_AccountsReceivableGetClaimsCount", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@ClientID", clientId);
                    cmd.Parameters.AddWithValue("@InsuranceCompanyId", insuranceCompanyId);
                    cmd.Parameters.AddWithValue("@ClaimStatusId", claimStatusId);
                    cmd.Parameters.AddWithValue("@AsOfDate", asOfDate);
                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> GetClaimList(string clientId, string insuranceCompanyId, int claimStatusId, string asOfDate, int ClaimAgeRange)
        {
            AccountsReceivable AR = new AccountsReceivable();
            DataSet ds = await getARClaimList(clientId, insuranceCompanyId, claimStatusId, asOfDate, ClaimAgeRange);
            AR.ClaimList = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new ARClaims()
            {
                ClaimId = spR["ClaimId"] == DBNull.Value ? 0 : Convert.ToInt32(spR["ClaimId"]),
                ClaimDate = spR.GetValueOrDefault<DateTime>("ClaimDate"),
                ClaimAge = spR["Age"] == DBNull.Value ? 0 : Convert.ToInt32(spR["Age"]),
                AmountDue = spR["AmountDue"] == DBNull.Value ? 0 : Convert.ToDecimal(spR["AmountDue"]),
                ClaimStatus = new ClaimsStatus
                {
                    claimstatusid = spR["ClaimStatusId"] == DBNull.Value ? 0 : Convert.ToInt32(spR["ClaimStatusId"]),
                    name = Convert.ToString(spR["StatusName"])
                },
                InsuranceCompanyName = Convert.ToString(spR["CompanyName"]),
                InsurancePriorityId = spR["InsurancePriorityId"] == DBNull.Value ? 0 : Convert.ToInt32(spR["InsurancePriorityId"]),
                ServiceName = Convert.ToString(spR["Service"]),
                ClientName = Convert.ToString(spR["Client Name"]),
                ClientId = spR["ClientId"] == DBNull.Value ? 0 : Convert.ToInt32(spR["ClientId"]),
                InsuranceCompanyId = spR["InsuranceCompanyId"] == DBNull.Value ? 0 : Convert.ToInt32(spR["InsuranceCompanyId"]),
            }).ToList();

            return PartialView("Claims", AR);
        }

        [AJAXAuthorize]
        public async Task<ActionResult> ExportAccountsReceivable(string clientId, string insuranceCompanyId, int claimStatusId, string asOfDate, int ClaimAgeRange)
        {
            byte[] fileBytes = null;
            string fileName = "";
            DataSet ds = new DataSet();
            List<ARClaims> claimList = new List<ARClaims>();
            Er er = new Er();
            DateTime asOfDateTime = asOfDate == "" || String.IsNullOrEmpty(asOfDate) ? DateTime.UtcNow : DateTime.Parse(asOfDate);
            try
            {
                ds = await getARClaimList(clientId, insuranceCompanyId, claimStatusId, asOfDate, ClaimAgeRange);
                claimList = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new ARClaims()
                {
                    ClaimId = spR["ClaimId"] == DBNull.Value ? 0 : Convert.ToInt32(spR["ClaimId"]),
                    ClaimDate = spR.GetValueOrDefault<DateTime>("ClaimDate"),
                    ClaimAge = spR["Age"] == DBNull.Value ? 0 : Convert.ToInt32(spR["Age"]),
                    AmountDue = spR["AmountDue"] == DBNull.Value ? 0 : Convert.ToDecimal(spR["AmountDue"]),
                    ClaimStatus = new ClaimsStatus
                    {
                        claimstatusid = spR["ClaimStatusId"] == DBNull.Value ? 0 : Convert.ToInt32(spR["ClaimStatusId"]),
                        name = Convert.ToString(spR["StatusName"])
                    },
                    InsuranceCompanyName = Convert.ToString(spR["CompanyName"]),
                    InsurancePriorityId = spR["InsurancePriorityId"] == DBNull.Value ? 0 : Convert.ToInt32(spR["InsurancePriorityId"]),
                    ServiceName = Convert.ToString(spR["Service"]),
                    ClientName = Convert.ToString(spR["Client Name"])

                }).ToList();

                string templateFolder = Server.MapPath("~/Templates/");
                string templateFileName = templateFolder + "AccountsReceivableExportFile.xlsx";

                XSSFWorkbook AccountsReceivableFile;
                using (FileStream file = new FileStream(templateFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    AccountsReceivableFile = new XSSFWorkbook(file);
                }

                ICellStyle boldStyle = /*(XSSFCellStyle)*/AccountsReceivableFile.CreateCellStyle();

                IFont boldFont = (XSSFFont)AccountsReceivableFile.CreateFont();
                boldFont.IsBold = true;
                boldStyle.SetFont(boldFont);

                IDataFormat decimalCellFormate = AccountsReceivableFile.CreateDataFormat();
                var dataFormate = decimalCellFormate.GetFormat("0.00");
                ICellStyle decimalCellStyle = AccountsReceivableFile.CreateCellStyle();
                decimalCellStyle.DataFormat = dataFormate;

                ICellStyle decimalBoldCellStyle = AccountsReceivableFile.CreateCellStyle();
                decimalBoldCellStyle.DataFormat = dataFormate;
                decimalBoldCellStyle.SetFont(boldFont);


                ISheet sheet = AccountsReceivableFile.GetSheetAt(0);
                IRow row = sheet.GetRow(0);
                fileName = asOfDateTime.Year + (asOfDateTime.Month < 10 ? "0" + asOfDateTime.Month : asOfDateTime.Month.ToString()) + (asOfDateTime.Day < 10 ? "0" + asOfDateTime.Day : asOfDateTime.Day.ToString()) + "AccountsReceivable.xlsx";

                int rowIndex = 1;
                foreach (var claim in claimList)
                {
                    row = sheet.GetRow(rowIndex);
                    if (row == null)
                    {
                        row = sheet.CreateRow(rowIndex);
                        for (int i = 0; i < 9; i++)
                            row.CreateCell(i);

                    }

                    row.GetCell(0).SetCellValue(claim.ClaimId);
                    row.GetCell(0).CellStyle = boldStyle;
                    row.GetCell(1).SetCellValue(claim.ClaimDate.ToString("MM/dd/yyyy"));
                    row.GetCell(2).SetCellValue(claim.ClaimAge);
                    row.GetCell(3).SetCellValue(Convert.ToDouble(claim.AmountDue));
                    row.GetCell(4).SetCellValue(claim.ClaimStatus.name);
                    row.GetCell(5).SetCellValue(claim.ClientName);
                    row.GetCell(6).SetCellValue(claim.InsuranceCompanyName);
                    row.GetCell(7).SetCellValue(claim.InsurancePriorityId == 1 ? "Primary" : claim.InsurancePriorityId == 2 ? "Secondary" : "Tertiary");
                    row.GetCell(8).SetCellValue(claim.ServiceName);
                    rowIndex++;
                }

                using (var ms = new NpoiMemoryStream())
                {
                    ms.AllowClose = false;
                    AccountsReceivableFile.Write(ms);
                    ms.Flush();
                    ms.Seek(0, SeekOrigin.Begin);
                    byte[] data = new byte[ms.Length];
                    ms.Read(data, 0, data.Length);
                    Response.ClearHeaders();
                    Response.AddHeader("Content-Disposition", "attachment;filename=\"" + fileName + "\"");
                    return new FileContentResult(data, " application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                }

            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;

            }
            ds.Dispose();
            Response.Write(er.msg);
            Response.StatusCode = 400;
            return null;
        }
        private Task<DataSet> getARClaimList(string clientId, string insuranceCompanyId, int claimStatusId, string asOfDate, int ClaimAgeRange)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    //need to add the storeproc to get Insurance
                    SqlCommand cmd = new SqlCommand("sp_AccountsReceivableGetClaims", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@ClientID", clientId);
                    cmd.Parameters.AddWithValue("@InsuranceCompanyId", insuranceCompanyId);
                    cmd.Parameters.AddWithValue("@ClaimStatusId", claimStatusId);
                    cmd.Parameters.AddWithValue("@AsOfDate", asOfDate);
                    cmd.Parameters.AddWithValue("@ClaimAgeRange", ClaimAgeRange);
                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
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
        private Task<DataSet> getClientList()
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ClientsGetClientList", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                    cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
        }
    }
}