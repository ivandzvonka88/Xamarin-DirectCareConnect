using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using DCC.Models;
using DCC.SQLHelpers.Helpers;

namespace DCC.Controllers
{
    public class SkilledPayrollInputController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;

        public SkilledPayrollInputController()
        {
            sqlHelper = new SQLHelper();
        }

        [Authorize]
        public async Task<ActionResult> Index()
        {
            Payroll r = new Payroll();

            DataSet ds = new DataSet();
            await Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_GetPayPeriods", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@TimeZone", UserClaim.timeZone);
                    cmd.Parameters.AddWithValue("@IsNonSkilled", false);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
            });
            r.PeriodId = Convert.ToInt32(ds.Tables[0].Rows[0].ItemArray[0]);
            r.Periods = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new Period()
            {
                startDate = Convert.ToDateTime(spR["s"]).ToShortDateString(),
                endDate = Convert.ToDateTime(spR["e"]).ToShortDateString(),
                periodId = (int)spR["ppId"]
            }).ToList();
            r.Providers = ds.Tables[2].Rows.Cast<DataRow>().Select(spR => new ProviderSelect()
            {
                providerName = (string)spR["providerName"],
                providerId = (int)spR["providerId"]
            }).ToList();


            setViewModelBase((ViewModelBase)r);
            ds.Dispose();
            return View("Index", r);
        }



        [AJAXAuthorize]
        public async Task<ActionResult> getProviderPayroll(int providerId, int payrollId, string startEndDates)
        {
            ProviderPayrollTimesheet r = null;
            DataSet ds = new DataSet();
            Er er = new Er();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ProviderPayrollTherapyGet", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@providerId", providerId);
                        cmd.Parameters.AddWithValue("@payrollId", payrollId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                r = PopulateTimeSheet(ref ds, providerId, startEndDates);
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

            return PartialView("TimeSheet", r);
        }



        private ProviderPayrollTimesheet PopulateTimeSheet(ref DataSet ds, int providerId, string startEndDates)
        {

            ProviderPayrollTimesheet r = new ProviderPayrollTimesheet();
            DataRow dr = ds.Tables[0].Rows[0];
            r.ProviderName = dr["ln"] + " " + dr["fn"];
            r.ProviderId = providerId;
            r.TimeSheetEntries = new List<TimeSheetEntry>();
            foreach (DataRow entry in ds.Tables[1].Rows)
            {
                r.TimeSheetEntries.Add(new TimeSheetEntry()
                {
                    id = (int)entry["id"],
                    SessionDate = ((DateTime)entry["dt"]).ToShortDateString(),
                    NoteDate = entry["completedDt"] == DBNull.Value ? "" : ((DateTime)entry["completedDt"]).ToShortDateString(),
                    ApprovedDate = entry["designeeApprovedDt"] == DBNull.Value ? "" : ((DateTime)entry["designeeApprovedDt"]).ToShortDateString(),
                    SupervisorApprovedDate = entry["verifiedDt"] == DBNull.Value ? "" : ((DateTime)entry["verifiedDt"]).ToShortDateString(),
                    Code = (string)entry["PayRollCode"],
                    Start = entry["utcIn"] != DBNull.Value ? DateTimeLocal((DateTime)entry["utcIn"]).ToShortTimeString() : "",
                    End = entry["utcOut"] != DBNull.Value ? DateTimeLocal((DateTime)entry["utcOut"]).ToShortTimeString() : "",
                    Units = (decimal)entry["units"],
                    IsEditable = (bool)entry["isEditable"]
                });
            }

            r.PayrollCodes = ds.Tables[2].Rows.Cast<DataRow>().Select(spR => new PayrollCode()
            {
                Code = (string)spR["payRollCode"],
                RequiresHours = (bool)spR["requiresHours"] ? 1 : 0
            }).ToList();


            // for day entry checks within specified dtea
            string[] dates = startEndDates.Split('-');
            DateTime startDate = Convert.ToDateTime(dates[0]);
            DateTime endDate = Convert.ToDateTime(dates[1]);
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

            return r;
        }


        [AJAXAuthorize]
        public async Task<ActionResult> InsertProviderPayrollRecord(int id, int providerId, int payrollId, string startEndDates, string payrollCode, string date, int inOffsetMinutes, int outOffSetMinutes, decimal units)
        {
            ProviderPayrollTimesheet r = null;
            DataSet ds = new DataSet();
            Er er = new Er();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ProviderPayrollTherapyAdd", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@providerId", providerId);
                        cmd.Parameters.AddWithValue("@payrollId", payrollId);
                        cmd.Parameters.AddWithValue("@date", date);
                        cmd.Parameters.AddWithValue("@payrollCode", payrollCode);
                        cmd.Parameters.AddWithValue("@utcIn", ConvertToUTC((Convert.ToDateTime(date)).AddMinutes(inOffsetMinutes)));
                        cmd.Parameters.AddWithValue("@utcOut", ConvertToUTC((Convert.ToDateTime(date)).AddMinutes(outOffSetMinutes)));
                        cmd.Parameters.AddWithValue("@units", units);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                r = PopulateTimeSheet(ref ds, providerId, startEndDates);
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

            return View("TimeSheet", r);
        }



        [AJAXAuthorize]
        public ActionResult openDeletePayrollRecord(TimeSheetEntry t)
        {
            return PartialView("ModalDeleteEntry", t);
        }


        [AJAXAuthorize]
        public async Task<ActionResult> DeletePayrollRecord(int id, int providerId, int payrollId, string startEndDates)
        {

            ProviderPayrollTimesheet r = new ProviderPayrollTimesheet();
            DataSet ds = new DataSet();
            Er er = new Er();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ProviderPayrollTherapyDelete", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@providerId", providerId);
                        cmd.Parameters.AddWithValue("@payrollId", payrollId);

                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                r = PopulateTimeSheet(ref ds, providerId, startEndDates);
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

            return PartialView("TimeSheet", r);

        }
    }
}