using DCC.ModelsLegacy;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DCC.SQLHelpers.Helpers;
using DCC.Helpers;

namespace DCC.Controllers
{
    public class DiagnosisController : DCCBaseController
    {

        private readonly SQLHelper sQLHelper;

        public DiagnosisController()
        {
            sQLHelper = new SQLHelper();
        }

        public ActionResult Index()
        {
            var response = new EmptyView();
            setViewModelBase((ViewModelBase)response);
            return View(response);  
        }


        [HttpPost]
        public ActionResult EditDiagnosis(int? id = null)
        {
            var response = new Diagnosis();
            if (id != 0)
            {
                response = GetDiagnosisCodes(id).FirstOrDefault();
            }
            return View(response);
        }

        [HttpPost]
        public JsonResult ManageDiagnosis(Diagnosis diagnosis)
        {
            var response = true;
            SqlParameter isExist = new SqlParameter("@ToCheck", SqlDbType.Int);
            isExist.Direction = ParameterDirection.ReturnValue;

            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_ManageDiagnosis", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlCommand.Parameters.AddWithValue("@DiagnosisCode", diagnosis.DiagnosisCode);
                    sqlCommand.Parameters.AddWithValue("@Description", diagnosis.Description);
                    sqlCommand.Parameters.AddWithValue("@ID", diagnosis.ID);
                    sqlCommand.Parameters.Add(isExist);

                    try
                    {
                        if (connection.State == ConnectionState.Closed)
                        {
                            connection.Open();
                        }
                        sqlCommand.ExecuteNonQuery();
                        int isDuplicate = Convert.ToInt32(isExist.Value);
                        if (isDuplicate == 0)
                        {
                            response = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public List<Diagnosis> GetDiagnosisCodes(int? id = null, int? pageSize = null)
        {
            var toReturn = new List<Diagnosis>();
            var result = new DataTable();
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_GetDiagnosisCodes", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    if (id == 0)
                    {
                        sqlCommand.Parameters.AddWithValue("@ID", null);
                        sqlCommand.Parameters.AddWithValue("@PageSize", pageSize);
                    }
                    else
                    {
                        sqlCommand.Parameters.AddWithValue("@ID", id);
                        sqlCommand.Parameters.AddWithValue("@PageSize", pageSize);
                    }
                    sQLHelper.ExecuteSqlDataAdapter(sqlCommand, result);
                }

                if (result.HasRows())
                {
                    toReturn = result.Rows.Cast<DataRow>().Select(x => new Diagnosis()
                    {
                        DiagnosisCode = x.GetValueOrDefault<string>("id").Trim(),
                        Description = x.GetValueOrDefault<string>("Name"),
                        ID = x.GetValueOrDefault<int>("DiaCodeID")
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return toReturn.OrderBy(x => x.Description).ToList();
        }

        [HttpGet]
        public JsonResult DeleteDiagnosis(int id)
        {
            var response = false;
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_DeleteDiagnosis", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlCommand.Parameters.AddWithValue("@ID", id);
                    try
                    {
                        if (connection.State == ConnectionState.Closed)
                        {
                            connection.Open();
                        }
                        sqlCommand.ExecuteNonQuery();
                        response = true;
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetDiagnosis()
        {
            var toReturn = new List<Diagnosis>();
            var result = new DataTable();
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_GetDiagnosisCodes", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sQLHelper.ExecuteSqlDataAdapter(sqlCommand, result);
                }

                if (result.HasRows())
                {
                    toReturn = result.Rows.Cast<DataRow>().Select(x => new Diagnosis()
                    {
                        DiagnosisCode = x.GetValueOrDefault<string>("id").Trim(),
                        Description = x.GetValueOrDefault<string>("Name"),
                        ID = x.GetValueOrDefault<int>("DiaCodeID")
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return Json(toReturn, JsonRequestBehavior.AllowGet);
        }
    }
}