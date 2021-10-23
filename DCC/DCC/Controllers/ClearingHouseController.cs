using DCC.Helpers;
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

namespace DCC.Controllers
{
    public class ClearingHouseController : DCCBaseController
    {
        private readonly SQLHelper sQLHelper;

        public ClearingHouseController()
        {
            sQLHelper = new SQLHelper();
        }

        public ActionResult Index()
        {
            var response = new ClearingHousesInit();
            setViewModelBase((ViewModelBase)response);
            return View(response);
        }

        [HttpGet]
        public JsonResult GetClearingHouses(int? id = null)
        {
            var toReturn = new List<ClearingHouses>();
            var result = new DataTable();
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_GetClearingHousesByCompanyID", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlCommand.Parameters.AddWithValue("@CompanyID", id);
                    sQLHelper.ExecuteSqlDataAdapter(sqlCommand, result);
                }

                if (result.HasRows())
                {
                    toReturn = result.Rows.Cast<DataRow>().Select(x => new ClearingHouses()
                    {
                        ClearingHouseLogin = x.GetValueOrDefault<string>("ClearingHouseLogin"),
                        ClearingHouseRTUser = x.GetValueOrDefault<string>("ClearingHouseRTUser"),
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return Json(toReturn,JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ManageClearingHouse(ClearingHouses clearingHouses)
        {
            var response = false;
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_ManageClearingHouse", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlCommand.Parameters.AddWithValue("@CompanyID", clearingHouses.CompanyID);
                    sqlCommand.Parameters.AddWithValue("@ClearingHouseLogin", clearingHouses.ClearingHouseLogin);
                    sqlCommand.Parameters.AddWithValue("@ClearingHousePasscode", clearingHouses.ClearingHousePasscode);
                    sqlCommand.Parameters.AddWithValue("@ClearingHouseRTUser", clearingHouses.ClearingHouseRTUser);
                    sqlCommand.Parameters.AddWithValue("@ClearingHouseRTPass", clearingHouses.ClearingHouseRTPass);

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
    }
}