using DCC.Helpers;
using DCC.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using DCC.SQLHelpers.Helpers;

namespace DCC.Controllers
{
    [Authorize]
    public class BillingInsuranceCompanyController : DCCBaseController
    {
        // GET: BillingInsuranceCompany
        private readonly SQLHelper sqlHelper;
        // InsuranceCompanyController InsuranceCompanyCnt = new InsuranceCompanyController();
        public BillingInsuranceCompanyController()
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
        public ActionResult EditBillingCompany(int insuranceCompanyId)
        {
            var response = new BillingInsuranceCompany();
            if (insuranceCompanyId > 0)
            {
                response = GetBillingInsuranceCompanies(insuranceCompanyId)?.FirstOrDefault();
            }
            return View(response);
        }

        public List<BillingInsuranceCompany> GetBillingInsuranceCompanies(int? id = null)
        {
            var toReturn = new List<BillingInsuranceCompany>();
            var result = new DataTable();
            try
            {
                using (SqlConnection connection = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_GetBillingInsuranceCompanies", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlCommand.Parameters.AddWithValue("@ID", id);
                    sqlHelper.ExecuteSqlDataAdapter(sqlCommand, result);

                }

                if (result.HasRows())
                {
                    toReturn = result.Rows.Cast<DataRow>().Select(x => new BillingInsuranceCompany()
                    {
                        InsuranceCompanyId = x.GetValueOrDefault<Int32>("InsuranceCompanyId"),
                        PatientCount = x.GetValueOrDefault<Int32>("ClientCount"),
                        EnableEligibility = x.GetValueOrDefault<bool>("EnableEligibility"),
                        StatusDelay = x.GetValueOrDefault<Int16>("StatusDelay"),
                        StatusFreq = x.GetValueOrDefault<Int16>("StatusFreq"),
                        ExcludeRenderer = x.GetValueOrDefault<bool>("ExcludeRenderer"),
                    }).ToList();

                    var insuranceCompanyIds = toReturn.Select(x => x.InsuranceCompanyId);

                    InsuranceCompanyController InsuranceCompanyCntroller = new InsuranceCompanyController();
                    var insuranceCompaniesFull = InsuranceCompanyCntroller.GetInsuranceCompanies();

                    insuranceCompaniesFull.RemoveAll(x => !insuranceCompanyIds.Contains(x.InsuranceCompanyId));

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


        [HttpPost]
        public JsonResult UpdateBillingInsuranceCompany(BillingInsuranceCompany billingInsurance)
        {
            var response = false;
            try
            {
                using (SqlConnection connection = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_UpdateBillingInsuranceCompany", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlCommand.Parameters.AddWithValue("@ID", billingInsurance.InsuranceCompanyId);
                    sqlCommand.Parameters.AddWithValue("@StatusDelay", billingInsurance.StatusDelay);
                    sqlCommand.Parameters.AddWithValue("@StatusFreq", billingInsurance.StatusFreq);
                    sqlCommand.Parameters.AddWithValue("@EnableEligibility", billingInsurance.EnableEligibility);
                    sqlCommand.Parameters.AddWithValue("@ExcludeRenderer", billingInsurance.ExcludeRenderer);
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

        [HttpGet]
        public JsonResult DeleteBillingInsuranceCompany(int insuranceCompanyID)
        {
            SqlParameter havePatientCount = new SqlParameter("@HaveCount", SqlDbType.Int);
            var isCount = -1;
            var response = false;
            try
            {
                using (SqlConnection connection = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_DeleteBillingInsuranceCompany", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    havePatientCount.Direction = ParameterDirection.ReturnValue;
                    sqlCommand.Parameters.Add(havePatientCount);
                    sqlCommand.Parameters.AddWithValue("@InsuranceCompanyId", insuranceCompanyID);
                    try
                    {
                        if (connection.State == ConnectionState.Closed)
                        {
                            connection.Open();
                        }
                        sqlCommand.ExecuteNonQuery();
                        isCount = Convert.ToInt32(havePatientCount.Value);
                        if (isCount > 0)
                        {
                            response = true;
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


        [HttpPost]
        public JsonResult AddBillingInsuranceCompany(BillingInsuranceCompany billingInsurance)
        {
            var response = false;
            var existingCompanies = GetBillingInsuranceCompanies();
            var isExist = existingCompanies.Any(x => x.InsuranceCompanyId == billingInsurance.InsuranceCompanyId);
            if (!isExist)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand sqlCommand = new SqlCommand("sp_AddBillingInsuranceCompany", connection)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        sqlCommand.Parameters.AddWithValue("@ID", billingInsurance.InsuranceCompanyId);
                        sqlCommand.Parameters.AddWithValue("@StatusDelay", billingInsurance.StatusDelay);
                        sqlCommand.Parameters.AddWithValue("@StatusFreq", billingInsurance.StatusFreq);
                        sqlCommand.Parameters.AddWithValue("@EnableEligibility", billingInsurance.EnableEligibility);
                        sqlCommand.Parameters.AddWithValue("@ExcludeRenderer", billingInsurance.ExcludeRenderer);
                        sqlCommand.Parameters.AddWithValue("@Active", billingInsurance.Active);
                        sqlCommand.Parameters.AddWithValue("@AuditActionId", billingInsurance.AuditActionId);
                        sqlCommand.Parameters.AddWithValue("@BillingAddressId", DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("@AddressLine1", DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("@AddressLine2", DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("@City", DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("@State", DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("ZipCode", DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("@ContactName", DBNull.Value);

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
            }
            else
            {
                response = false;
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetAllInsuranceCompanies()
        {
            InsuranceCompanyController InsuranceCompanyCnt = new InsuranceCompanyController();
            var insCompanies = InsuranceCompanyCnt.GetInsuranceCompanies();
            var options = insCompanies.Select(x => new
            {
                x.Name,
                x.InsuranceCompanyId
            }).ToList();

            return Json(options, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetGovtInsuranceCompanies(int insuranceCompanyId)
        {
            var toReturn = new InsurancePolicyDTO();
            var result = new DataTable();
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_GetGovtProgramInsuranceCompanies", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlCommand.Parameters.AddWithValue("@InsuranceCompanyId", insuranceCompanyId);
                    sqlHelper.ExecuteSqlDataAdapter(sqlCommand, result);

                }

                if (result.HasRows())
                {
                    toReturn = result.Rows.Cast<DataRow>().Select(x => new InsurancePolicyDTO()
                    {
                        mcid = x.GetValueOrDefault<string>("MCID")
                    }).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
            }

            return Json(toReturn, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetAllBillingInsuranceCompanies(int? id = null)
        {
            var toReturn = new List<BillingInsuranceCompany>();
            try
            {
                toReturn = GetBillingInsuranceCompanies();
            }
            catch (Exception ex)
            {
            }
            return Json(toReturn.OrderBy(x => x.Name).ToList(),JsonRequestBehavior.AllowGet);
        }


    }
}
