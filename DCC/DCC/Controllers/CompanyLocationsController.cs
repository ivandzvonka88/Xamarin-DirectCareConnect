using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using DCC.Models;
using DCC.Models.CompanyLocations;
using DCC.SQLHelpers.Helpers;
using DCC.Geo;
namespace DCC.Controllers
{
    public class CompanyLocationsController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;
        public CompanyLocationsController()
        {
            sqlHelper = new SQLHelper();
        }



        [AJAXAuthorize]
        public async Task<ActionResult> Index()
        {
            LocationOptions r = new LocationOptions();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_LocationsGetOptions", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                r.billingLocationTypes = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new Option()
                {
                    value = Convert.ToString(spR["billingLocationTypeId"]),
                    name = (string)spR["billingLocationType"],
                }).ToList();
                r.districts = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new Option()
                {
                    value = Convert.ToString(spR["districtId"]),
                    name = (string)spR["district"],
                }).ToList();

                r.ranges = ds.Tables[2].Rows.Cast<DataRow>().Select(spR => new Option()
                {
                    value = Convert.ToString(spR["rangeId"]),
                    name = (string)spR["range"],
                }).ToList();
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            

            ds.Dispose();

            setViewModelBase((ViewModelBase)r);

            return View("Index", r);
        }

        [AJAXAuthorize]
        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public JsonResult GetAllLocations()
        {
            LocationList locationList = new LocationList();

            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_LocationsGetLocations", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
                locationList.locations = setLocations(ref ds, 0);
            }
            catch (Exception ex)
            {
                locationList.er.code = 1;
                locationList.er.msg = ex.Message;
            }
            ds.Dispose();



            return Json(locationList.locations, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> AddLocation(CompanyLocation c)
        {
           LocationList r = new LocationList();
           GeoWebResult geoWebResult = new GeoWebResult();

           GeoLocator geoLocator = new GeoLocator(c.ad1, c.ad2, c.cty, c.st, c.zip);
           geoWebResult = await geoLocator.GetGeoLocation();

            if (geoWebResult.er.code == 0)
            {
                c.lat = geoWebResult.lat;
                c.lon = geoWebResult.lon;
                c.locationType = geoWebResult.locationType;
                c.radius = 1000;

                DataSet ds = new DataSet();
                try
                {
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_LocationsAddLocation", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                            cmd.Parameters.AddWithValue("@name", c.name);
                            cmd.Parameters.AddWithValue("@ad1", c.ad1);
                            cmd.Parameters.AddWithValue("@ad2", c.ad2 ?? "");
                            cmd.Parameters.AddWithValue("@cty", c.cty);
                            cmd.Parameters.AddWithValue("@st", c.st);
                            cmd.Parameters.AddWithValue("@zip", c.zip);
                            cmd.Parameters.AddWithValue("@loc", c.loc);
                            cmd.Parameters.AddWithValue("@npi", c.npi ?? "");
                            cmd.Parameters.AddWithValue("@billingLocationTypeId", c.billingLocationTypeId);

                            cmd.Parameters.AddWithValue("@districtId", c.districtId);
                            cmd.Parameters.AddWithValue("@range", c.range);
                            cmd.Parameters.AddWithValue("@contractCapacity", c.contractCapacity);
                            cmd.Parameters.AddWithValue("@lat", c.lat);
                            cmd.Parameters.AddWithValue("@lon", c.lon);
                            cmd.Parameters.AddWithValue("@locationType", c.locationType);
                            cmd.Parameters.AddWithValue("@radius", c.radius);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    }); 
                    r.locations = setLocations(ref ds, 0);
                }
                catch (Exception ex)
                {
                    r.er.code = 1;
                    r.er.msg = ex.Message;
                }           
                ds.Dispose();
            }
            else
            {
                r.er.code = 2;
                r.er.msg = "Something went unexpectably wrong";
            }
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return Json(r,JsonRequestBehavior.AllowGet);
        }


        private List<CompanyLocation> setLocations(ref DataSet ds, int tableIdx)
        {
            List<CompanyLocation> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new CompanyLocation()
            {
                locationId = (int)spR["locationId"],
                name = (string)spR["name"],
                ad1 = (string)spR["ad1"],
                ad2 = spR["ad2"] == DBNull.Value ? "" : (string)spR["ad2"],
                cty = (string)spR["cty"],
                st = (string)spR["st"],
                zip = (string)spR["zip"],
                range = (short)spR["range"],
                contractCapacity = (short)spR["contractCapacity"],
                districtId = (int)spR["districtId"],
                district = spR["district"] == DBNull.Value ? "" : (string)spR["district"],
                billingLocationTypeId = (short)spR["billingLocationTypeId"],
                billingLocationType = (string)spR["billingLocationType"],

                reg = (bool)spR["isFlagstaff"] ? "Flagfstaff" : "Statewide",
                npi = (string)spR["locationNPI"],


                loc = spR["loc"] == DBNull.Value ? "" : (string)spR["loc"],


                lat = (decimal)spR["lat"],
                lon = (decimal)spR["lon"],
                radius = (short)spR["radius"],
                isActive = (bool)spR["active"],
                isActiveStr = (bool)spR["active"] == false ? "NO" : "YES"

            }).ToList();
            return r;
        }

    }
}