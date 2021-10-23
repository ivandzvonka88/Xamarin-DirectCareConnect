using DCC.Models.Clients;
using DCC.Models.Providers;
using DCC.ModelsLegacy;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using DCC.Helpers;
using DCC.SQLHelpers.Helpers;

namespace DCC.Controllers
{
    [Authorize]
    public class InsuranceCompanyController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;
        public InsuranceCompanyController()
        {
            sqlHelper = new SQLHelper();
        }

        InsuranceCompanyInit r = new InsuranceCompanyInit();
        public ActionResult Index()
        {
            var response = new EmptyView();
            setViewModelBase((ViewModelBase)response);
            return View(response);
        }


        public List<InsuranceCompany> GetInsuranceCompanies()
        {
            DataSet ds = new DataSet();
            InsuranceCompanyInit r = new InsuranceCompanyInit();
            using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_InsuranceGetInsuranceList", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
            }

            r.InsuranceCompanyList = ds.Tables[0].Rows.Cast<DataRow>().Select(x => new InsuranceCompany()
            {
                PayerId = Convert.ToString(x.GetColumnValueOrNull("PayerId")),
                Name = Convert.ToString(x.GetColumnValueOrNull("Name")),
                InsuranceCompanyId = Convert.ToInt32(x.GetColumnValueOrNull("InsuranceCompanyId")),
                Line1 = Convert.ToString(x.GetColumnValueOrNull("Line1")),
                Line2 = Convert.ToString(x.GetColumnValueOrNull("Line2")),
                City = Convert.ToString(x.GetColumnValueOrNull("City")),
                State = Convert.ToString(x.GetColumnValueOrNull("State")),
                PostalCode = Convert.ToString(x.GetColumnValueOrNull("PostalCode")),
                IsGovt = Convert.ToBoolean(x.GetColumnValueOrNull("IsGovt")),
                InsCode = Convert.ToString(x.GetColumnValueOrNull("InsCode"))
            }).ToList();
            return r.InsuranceCompanyList;
        }
        public List<Options> GetGovtProgramLinks()
        {
            DataSet ds = new DataSet();
            InsuranceCompanyInit r = new InsuranceCompanyInit();
            using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_GetGovernmentProgram", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
            }
            try
            {
                if (ds.Tables[0].HasRows())
                {
                    r.GovtProgramLinks = ds.Tables[0].Rows.Cast<DataRow>().Select(spr => new Options()
                    {
                        Name = (string)spr["Name"],
                        Id = (Int16)spr["Id"]
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return r.GovtProgramLinks;
        }

        [HttpPost]
        public JsonResult InsuranceCompanies()
        {
            return Json(GetInsuranceCompanies(), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        //[ValidateJsonAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> OpenManageCompanyView()
        {
            InsuranceCompanyInit r = new InsuranceCompanyInit();
            r.GovtProgramLinks = GetGovtProgramLinks();
            return PartialView("_ManageCompany", r);
        }

        [HttpPost]
        //[ValidateJsonAntiForgeryToken]
        [Authorize]
        public async Task<JsonResult> ManageCompany(InsuranceCompany insuranceCompany)
        {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("sp_ManageInsuranceCompany", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    try
                    {
                        cmd.Parameters.AddWithValue("@name", insuranceCompany.Name);
                        cmd.Parameters.AddWithValue("@line1", insuranceCompany.Line1);
                        cmd.Parameters.AddWithValue("@line2", insuranceCompany.Line2);
                        cmd.Parameters.AddWithValue("@city", insuranceCompany.City);
                        cmd.Parameters.AddWithValue("@state", insuranceCompany.State);
                        cmd.Parameters.AddWithValue("@postalCode", insuranceCompany.PostalCode);
                        cmd.Parameters.AddWithValue("@insuranceCompanyId", insuranceCompany.InsuranceCompanyId);
                        cmd.Parameters.AddWithValue("@insPayerId", insuranceCompany.InsPayerId);
                        cmd.Parameters.AddWithValue("@payerid", insuranceCompany.PayerId);
                        cmd.Parameters.AddWithValue("@clearingHousesEligibilityId", insuranceCompany.EligibilityCheckId);
                        cmd.Parameters.AddWithValue("@clearingHousesStatusCheckId", insuranceCompany.StatusCheckId);
                        cmd.Parameters.AddWithValue("@clearingHousingId", 1);
                        cmd.Parameters.AddWithValue("@MCID", insuranceCompany.MCID);

                        XmlDocument programXML = new XmlDocument();
                        var programs = new List<GovernmentProgramInsuranceCompany>();
                        if (insuranceCompany.GovtProgramLinks != null && insuranceCompany.GovtProgramLinks.Any())
                        {

                            foreach (var item in insuranceCompany.GovtProgramLinks)
                            {
                                programs.Add(new GovernmentProgramInsuranceCompany
                                {
                                    ID = item.ID,
                                    Code = item.Code,
                                });
                            }


                            programXML.AppendChild(programXML.CreateElement("GovernmentProgram"));
                            foreach (var item in programs)
                            {
                                XmlElement element = programXML.CreateElement("Program");
                                element.AppendChild(programXML.CreateElement("ProgramID")).InnerText = Convert.ToString(item.ID);
                                element.AppendChild(programXML.CreateElement("Code")).InnerText = item.Code;
                                programXML.DocumentElement.AppendChild(element);
                            }

                        }

                        cmd.Parameters.AddWithValue("@ProgramXML", !string.IsNullOrWhiteSpace(programXML?.InnerXml) ? programXML?.InnerXml : null);

                        if (cn.State == ConnectionState.Closed) cn.Open();
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        if (cn.State == ConnectionState.Open) cn.Close();

                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            r.InsuranceCompanyList = GetInsuranceCompanies();

            return Json(new { res = insuranceCompany },
            JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        //[ValidateJsonAntiForgeryToken]
        [Authorize]
        public async Task<JsonResult> DeleteCompany(int insuranceCompanyId)
        {
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("sp_DeleteInsuranceCompany", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    try
                    {
                        cmd.Parameters.AddWithValue("@insuranceCompanyId", insuranceCompanyId);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
            }

            catch (Exception ex)
            {
                throw ex;
            }
            r.InsuranceCompanyList = GetInsuranceCompanies();

            return Json(new { res = r.InsuranceCompanyList },
                   JsonRequestBehavior.AllowGet);
        }

        [HttpPost]

        [Authorize]
        public async Task<JsonResult> EditCompany(int insuranceCompanyId)
        {
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("sp_InsuranceGetInsuranceList", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    try
                    {
                        cmd.Parameters.AddWithValue("@insuranceCompanyId", insuranceCompanyId);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
            }

            catch (Exception ex)
            {
                throw ex;
            }

            InsuranceCompany insuranceCompany = new InsuranceCompany();

            insuranceCompany.Name = Convert.ToString(ds.Tables[0].Rows[0]["Name"]);
            insuranceCompany.City = Convert.ToString(ds.Tables[0].Rows[0]["City"]);
            insuranceCompany.State = Convert.ToString(ds.Tables[0].Rows[0]["state"]);
            insuranceCompany.Line1 = Convert.ToString(ds.Tables[0].Rows[0]["line1"]);
            insuranceCompany.Line2 = Convert.ToString(ds.Tables[0].Rows[0]["line2"]);
            insuranceCompany.PostalCode = Convert.ToString(ds.Tables[0].Rows[0]["PostalCode"]);
            insuranceCompany.InsuranceCompanyId = Convert.ToInt32(ds.Tables[0].Rows[0]["insuranceCompanyId"]);
            insuranceCompany.InsuranceCompanyPayerId = Convert.ToString(ds.Tables[0].Rows[0]["payerid"]);
            insuranceCompany.MCID = Convert.ToString(ds.Tables[0].Rows[0]["MCID"]);
            if (ds.Tables[1].HasRows())
            {
                insuranceCompany.PayerId = ds.Tables[1].Rows[0]["payerid"] == DBNull.Value ? "" : Convert.ToString(ds.Tables[1].Rows[0]["payerid"]);
                insuranceCompany.EligibilityCheckId = ds.Tables[1].Rows[0]["code"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[1].Rows[0]["code"]);

                insuranceCompany.StatusCheckId = ds.Tables[1].Rows[0]["statuscheckcode"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[1].Rows[0]["statuscheckcode"]);

                insuranceCompany.ClearinghouseId = ds.Tables[1].Rows[0]["clearinghouseId"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[1].Rows[0]["clearinghouseId"]);
            }

            if (ds.Tables[2].HasRows())
                try
                {
                    insuranceCompany.GovtProgramLinks = ds.Tables[2].Rows.Cast<DataRow>().Select(x => new GovernmentProgramInsuranceCompany()
                    {
                        ID = Convert.ToInt16(x.GetColumnValueOrNull("GovernmentProgramId")),
                        Code = Convert.ToString(x.GetColumnValueOrNull("Code")),
                    }).ToList();
                }
                catch (Exception ex)
                {

                    throw ex;
                }

            return Json(new { res = insuranceCompany },
               JsonRequestBehavior.AllowGet);
        }
    }
}



