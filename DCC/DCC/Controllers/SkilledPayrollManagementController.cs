using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Text;
using System.IO;
using DCC.Models;
using DCC.SQLHelpers.Helpers;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace DCC.Controllers
{
    public class SkilledPayrollManagementController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;

        public SkilledPayrollManagementController()
        {
            sqlHelper = new SQLHelper();
        }
        [AJAXAuthorize]
        public async Task<ActionResult> Index()
        {

            Payroll r = new Payroll();

            DataSet ds = new DataSet();
            await Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_GetPayPeriodsOnly", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@TimeZone", UserClaim.timeZone);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
            });

            r.Periods = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new Period()
            {
                startDate = Convert.ToDateTime(spR["s"]).ToShortDateString(),
                endDate = Convert.ToDateTime(spR["e"]).ToShortDateString(),
                periodId = (int)spR["ppId"]
            }).ToList();

            try
            {
                DataSet Company = new DataSet();
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetCompanyInfoById", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@coId", UserClaim.coid);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, Company);
                    }
                });
                DataRow CompanyRow = Company.Tables[0].Rows[0];
                UserClaim.iSolvedCompanyId = (int)CompanyRow["iSolvedCompanyId"];
                r.hasISolved = UserClaim.iSolvedCompanyId != 0;
            }
            catch (Exception ex)
            {
                r.hasISolved = false;
            }

            setViewModelBase((ViewModelBase)r);
            ds.Dispose();
            return View("Index", r);
        }

        [AJAXAuthorize]
        public async Task<ActionResult> GetISolvedPayollReport(int payrollId)
        {
            byte[] fileBytes = null;
            string fileName = "";
            DataSet ds = new DataSet();
            Er er = new Er();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ProvidersAllPayrollTherapyGet", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@payrollId", payrollId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });


                DateTime startDate = (DateTime)ds.Tables[0].Rows[0].ItemArray[1];
                DateTime endDate = (DateTime)ds.Tables[0].Rows[0].ItemArray[0];

                fileName = endDate.Year + (endDate.Month < 10 ? "0" + endDate.Month : endDate.Month.ToString()) + (endDate.Day < 10 ? "0" + endDate.Day : endDate.Day.ToString()) + ".csv";

                string payrollItemList = "Key,PayItem,Hours\r\n";
                foreach (DataRow dr in ds.Tables[1].Rows)
                {
                    string payrollCode = "E" + (string)dr["PayRollCode"];
                    payrollItemList += (dr["iSolvedID"].ToString()) + "," + ISolvedAPI.GetPayRollCode(payrollCode) + "," + (decimal)dr["units"] + "\r\n";
                }
                using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(payrollItemList)))
                {
                    fileBytes = memoryStream.ToArray();
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

            Response.ClearHeaders();
            Response.AddHeader("Content-Disposition", "attachment;filename=" + fileName);
            return new FileContentResult(fileBytes, " text/csv");

        }

        [AJAXAuthorize]
        public async Task<ActionResult> OpenIsolvedPayrollPreview(int payrollId)
        {
            DataSet ds = new DataSet();
            Er er = new Er();
            ISolvedPayroll pr = new ISolvedPayroll();
            pr.payrollId = payrollId;
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ProvidersAllPayrollTherapyGet", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@payrollId", payrollId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                DateTime startDate = (DateTime)ds.Tables[0].Rows[0].ItemArray[1];
                DateTime endDate = (DateTime)ds.Tables[0].Rows[0].ItemArray[0];

                pr.payrollLines = new ISolvedPayrollLine[ds.Tables[1].Rows.Count];
                int index = 0;
                foreach (DataRow dr in ds.Tables[1].Rows)
                {
                    ISolvedPayrollLine prLine = pr.payrollLines[index++] = new ISolvedPayrollLine();
                    prLine.iSolvedID = dr["iSolvedID"].ToString();
                    prLine.PayRollCode = ISolvedAPI.GetPayRollCode("E" + (string)dr["PayRollCode"]);
                    prLine.units = (decimal)dr["units"];
                    prLine.error = prLine.iSolvedID == "" ? "No ISolvedID: " + dr["fn"] + " " + dr["ln"] : "";
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

            pr.status = await GetISovledPayrollStatus(payrollId);

            return PartialView("ModalISolvedPayroll", pr);
        }


        public async Task<ISolvedPayrollStatus> GetISovledPayrollStatus(int payrollId)
        {
            DataSet ds = new DataSet();
            Er er = new Er();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetISolvedPayrollID", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@payrollId", payrollId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                int iSolvedID = (int)ds.Tables[0].Rows[0].ItemArray[0];
                return await ISolvedAPI.getStatus(iSolvedID);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public async Task<ISolvedPayrollStatus> InitiateISolvedPayrollPreview(int payrollId)
        {
            byte[] fileBytes = null;
            string fileName = "";
            DataSet ds = new DataSet();
            Er er = new Er();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ProvidersAllPayrollTherapyGet", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@payrollId", payrollId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                DateTime startDate = (DateTime)ds.Tables[0].Rows[0].ItemArray[1];
                DateTime endDate = (DateTime)ds.Tables[0].Rows[0].ItemArray[0];

                fileName = endDate.Year + (endDate.Month < 10 ? "0" + endDate.Month : endDate.Month.ToString()) + (endDate.Day < 10 ? "0" + endDate.Day : endDate.Day.ToString()) + ".csv";

                string payrollItemList = "Key,PayItem,Hours\r\n";
                foreach (DataRow dr in ds.Tables[1].Rows)
                {
                    payrollItemList += (dr["iSolvedID"].ToString()) + "," + ISolvedAPI.GetPayRollCode("E" + (string)dr["PayRollCode"]) + "," + (decimal)dr["units"] + "\r\n";
                }
                using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(payrollItemList)))
                {
                    fileBytes = memoryStream.ToArray();
                }

                String data = Convert.ToBase64String(fileBytes);

                ISolvedPayrollStatus status = await ISolvedAPI.initiateImport(UserClaim.iSolvedCompanyId, data);

                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_SetISolvedIDSkilled", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@payrollId", payrollId);
                        cmd.Parameters.AddWithValue("@iSolvedID", status.id);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                return status;
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

            return null;
        }
        public async Task<ISolvedPayrollStatus> SubmitISolvedPayroll(int payrollId)
        {
            ISolvedPayrollStatus status = await GetISovledPayrollStatus(payrollId);
            status = await ISolvedAPI.submit(status.id);
            return status;
        }

        [AJAXAuthorize]
        public async Task<ActionResult> GetPayollReport(int payrollId)
        {
            byte[] fileBytes = null;
            string fileName = "";
            DataSet ds = new DataSet();
            Er er = new Er();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ProvidersAllPayrollTherapyGet", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@payrollId", payrollId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });


                DateTime startDate = (DateTime)ds.Tables[0].Rows[0].ItemArray[1];
                DateTime endDate = (DateTime)ds.Tables[0].Rows[0].ItemArray[0];
                Boolean hasISolved = ds.Tables[1].Columns.Contains("iSolvedID");

                fileName = endDate.Year + (endDate.Month < 10 ? "0" + endDate.Month : endDate.Month.ToString()) + (endDate.Day < 10 ? "0" + endDate.Day : endDate.Day.ToString()) + "Report.csv";

                string payrollItemList = "PayRoll Report For " + startDate.ToShortDateString() + " to " + endDate.ToShortDateString() + "\r\n";
                payrollItemList += "Name,ISolvedID,File No,Dept,Code,Hours\r\n";

                DataView dv = new DataView(ds.Tables[1]);
                DataTable Providers = null;
                if (hasISolved)
                {
                    Providers = dv.ToTable(true, "fn", "ln", "iSolvedID", "eId");
                }
                else
                {
                    Providers = dv.ToTable(true, "fn", "ln", "eId");
                }

                foreach (DataRow dr in Providers.Rows)
                {
                    dv.RowFilter = "fn='" + dr["fn"] + "' AND ln='" + dr["ln"] + "'";
                    payrollItemList += "\"" + dr["ln"] + ", " + dr["fn"] + "\"," + (hasISolved ? dr["iSolvedID"] : "") + "," + dr["eId"].ToString();
                    decimal totalUnits = 0;
                    foreach (DataRowView drv in dv)
                    {
                        payrollItemList += ",Therapy," + (string)drv["PayrollCode"] + "," + Convert.ToDouble(drv["Units"]);
                        totalUnits += (decimal)drv["Units"];
                        payrollItemList += "\r\n,,";
                    }

                    payrollItemList += ",,Total," + Convert.ToDouble(totalUnits) + "\r\n\r\n";
                }

                using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(payrollItemList)))
                {
                    fileBytes = memoryStream.ToArray();
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

            Response.ClearHeaders();
            Response.AddHeader("Content-Disposition", "attachment;filename=" + fileName);
            return new FileContentResult(fileBytes, " text/csv");
        }

        [AJAXAuthorize]
        public async Task<ActionResult> GetPayollErrors(int payrollId)
        {

            byte[] fileBytes = null;
            string fileName = "";
            DataSet ds = new DataSet();
            Er er = new Er();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ProvidersAllPayrollTherapyGet", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@payrollId", payrollId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });


           
                DateTime endDate = (DateTime)ds.Tables[0].Rows[0].ItemArray[0];
                fileName = endDate.Year + (endDate.Month < 10 ? "0" + endDate.Month : endDate.Month.ToString()) + (endDate.Day < 10 ? "0" + endDate.Day : endDate.Day.ToString()) + "Errors.txt";


                string errorList = "";
                DataView dv = new DataView(ds.Tables[1]);
                dv.RowFilter = "eid IS NULL OR eId=''";
                if (dv.Count != 0)
                {
                    DataTable NoFileIds = dv.ToTable(true, "fn", "ln");
                    foreach (DataRow dr in NoFileIds.Rows)
                    {
                        errorList += dr["ln"] + " " + dr["fn"] + " is missing a file number/employee ID\r\n";
                    }
                    NoFileIds.Dispose();
                }
                else
                    errorList += "All clients in payroll have a file number/employee ID\r\n";
                dv.Dispose();
                using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(errorList)))
                {
                    fileBytes = memoryStream.ToArray();
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
            Response.ClearHeaders();
            Response.AddHeader("Content-Disposition", "attachment;filename=\"" + fileName + "\"");
            return new FileContentResult(fileBytes, "text/plain");
           
        }


        [AJAXAuthorize]
        public async Task<ActionResult> GetPayollExport(int payrollId)
        {
            byte[] fileBytes = null;
            string fileName = "";
            DataSet ds = new DataSet();
            DataSet company = new DataSet();
            Er er = new Er();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetCompanyById", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@coId", UserClaim.coid);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, company);
                    }
                });
                DataRow drx = company.Tables[0].Rows[0];
                string companyCode = (string)drx["SkilledPayrollId"];

                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ProvidersAllPayrollTherapyGet", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@payrollId", payrollId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                DateTime endDate = (DateTime)ds.Tables[0].Rows[0].ItemArray[0];
                fileName = endDate.Year + (endDate.Month < 10 ? "0" + endDate.Month : endDate.Month.ToString()) + (endDate.Day < 10 ? "0" + endDate.Day : endDate.Day.ToString()) + ".csv";
                string batchId = (endDate.Day < 10 ? "0" + endDate.Day : endDate.Day.ToString()) + (endDate.Month < 10 ? "0" + endDate.Month : endDate.Month.ToString()) + endDate.Year;

                string payrollItemList = "Co Code, Batch ID,File #,Reg Hours,Hours 3 Code,Hours 3 Amount\r\n";
                foreach (DataRow dr in ds.Tables[1].Rows)
                {
                    payrollItemList += companyCode + "," + batchId + "," + dr["eId"] + ",," + (string)dr["PayRollCode"] + "," + (decimal)dr["units"] + "\r\n";

                }
                using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(payrollItemList)))
                {
                    fileBytes = memoryStream.ToArray();
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

            Response.ClearHeaders();
            Response.AddHeader("Content-Disposition", "attachment;filename=\"" + fileName + "\"");
            return new FileContentResult(fileBytes, " text/csv");
        }
    }
}