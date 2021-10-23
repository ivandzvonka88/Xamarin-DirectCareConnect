using DCC.Helpers;
using DCC.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using System.Configuration;
using DCC.SQLHelpers.Helpers;

namespace DCC.Controllers
{
    [Authorize]
    public class TherapyRateController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;
        public TherapyRateController()
        {
            sqlHelper = new SQLHelper();
        }
        public ActionResult Index()
        {
            var response = new EmptyView();
            setViewModelBase((ViewModelBase)response);
            return View(response);
        }

        [HttpPost]
        public ActionResult EditTherapyRate(int rateId)
        {
            var response = new TherapyRate();
            if (rateId > 0)
            {
                response = GetTherapyRates(rateId)?.FirstOrDefault();
            }
            response.Services = GetTherapyRates().FirstOrDefault().Services;
            return View(response);
        }

        [HttpPost]
        public JsonResult GetAllRates()
        {
            return Json(GetTherapyRates(), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public List<TherapyRate> GetTherapyRates(int? rateId = null)
        {
            var toReturn = new List<TherapyRate>();
            var result = new DataSet();
            var services = new List<AZService>();
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_GetAZTherapyRates", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlCommand.Parameters.AddWithValue("@RateId", rateId);
                    sqlHelper.ExecuteSqlDataAdapter(sqlCommand, result);
                }

                if (result.HasTables())
                {
                    if (result.Tables[1].HasRows())
                    {
                        var azServices = result.Tables[1];
                        services = azServices.Rows.Cast<DataRow>().Select(x => new AZService()
                        {
                            ServiceId = x.GetValueOrDefault<Int32>("ServiceId"),
                            Name = x.GetValueOrDefault<string>("Service"),
                        }).ToList();
                    }

                    if (result.Tables[0].HasRows())
                    {
                        var therapyRates = result.Tables[0];
                        toReturn = therapyRates.Rows.Cast<DataRow>().Select(x => new TherapyRate()
                        {
                            RateId = x.GetValueOrDefault<Int32>("RateId"),
                            ServiceId = x.GetValueOrDefault<Int32>("ServiceId"),
                            BillingTierId = x.GetValueOrDefault<Int16>("BillingTierId"),
                            IsClinicTxt = x.GetValueOrDefault<bool>("IsClinic") == true ? "Yes" : "No",
                            IsQualifiedTherapistTxt = x.GetValueOrDefault<bool>("IsQualifiedTherapist") == true ? "Yes" : "No",
                            CurTxt = x.GetValueOrDefault<bool>("Cur") == true ? "Yes" : "No",
                            From = x.GetValueOrDefault<DateTime>("EfDt").ToString("yyyy-MM-dd"),
                            To = x.GetValueOrDefault<DateTime>("FnDt").ToString("yyyy-MM-dd"),
                            Rate = x.GetValueOrDefault<decimal>("Rate"),
                            Ratio = x.GetValueOrDefault<Int16>("Ratio"),
                            Service = x.GetValueOrDefault<string>("Service"),
                            IsClinic = x.GetValueOrDefault<bool>("IsClinic"),
                            IsQualifiedTherapist = x.GetValueOrDefault<bool>("IsQualifiedTherapist"),
                            Cur = x.GetValueOrDefault<bool>("Cur"),
                            ServiceName = x.GetValueOrDefault<string>("ServiceName")
                        }).ToList();
                    }
                    toReturn.FirstOrDefault().Services = services;
                }
            }
            catch (Exception ex)
            {
            }
            return toReturn;
        }

        [HttpPost]
        public JsonResult ManageTherapyRate(TherapyRate insurance)
        {
            var isUpdate = false;
            var isAdd = false;
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_ManageTherapyRate", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    sqlCommand.Parameters.AddWithValue("@RateId", insurance.RateId);
                    sqlCommand.Parameters.AddWithValue("@ServiceId", insurance.ServiceId);
                    sqlCommand.Parameters.AddWithValue("@BillingTierId", insurance.BillingTierId);
                    sqlCommand.Parameters.AddWithValue("@Ratio", insurance.Ratio);
                    sqlCommand.Parameters.AddWithValue("@EfDt", insurance.From);
                    sqlCommand.Parameters.AddWithValue("@FnDt", insurance.To);
                    sqlCommand.Parameters.AddWithValue("@IsClinic", insurance.IsClinic);
                    sqlCommand.Parameters.AddWithValue("@IsQualifiedTherapist", insurance.IsQualifiedTherapist);
                    sqlCommand.Parameters.AddWithValue("@Cur", insurance.Cur);
                    sqlCommand.Parameters.AddWithValue("@Rate", insurance.Rate);
                    sqlCommand.Parameters.AddWithValue("@Service", insurance.Service);
                    sqlCommand.Parameters.AddWithValue("@ServiceName", insurance.ServiceName);

                    if (connection.State == ConnectionState.Closed)
                    {
                        connection.Open();
                    }
                    sqlCommand.ExecuteNonQuery();
                    if (insurance.RateId > 0)
                    {
                        isUpdate = true;
                    }
                    else { isAdd = true; }
                }
            }
            catch (Exception ex)
            {
            }
            return Json(new { IsUpdate = isUpdate, IsADD = isAdd }, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult DeleteTherapyRate(int rateId)
        {
            var response = false;
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_RemoveTherapyRate", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlCommand.Parameters.AddWithValue("@RateId", rateId);

                    if (connection.State == ConnectionState.Closed)
                    {
                        connection.Open();
                    }
                    sqlCommand.ExecuteNonQuery();
                    response = true;
                }
            }
            catch (Exception ex)
            {
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }
    }
}
