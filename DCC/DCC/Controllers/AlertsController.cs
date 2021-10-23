using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using DCC.Models;
using DCC.Models.Alerts;
using DCC.SQLHelpers.Helpers;

namespace DCC.Controllers
{
    public class AlertsController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;
        public AlertsController()
        {
            sqlHelper = new SQLHelper();
        }



        [AJAXAuthorize]
        public async Task<ActionResult> Index()
        {
            AlertList r = new AlertList();
            setViewModelBase((ViewModelBase)r);
            DataSet ds = new DataSet();
            try
            {

                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_AlertsSettingsGet", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                     
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                setAlertSettings(ref r, ref ds);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }


            return View("Index", r);
        }

    
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SetAlerts (AlertTableResp c)
        {
            AlertList r = new AlertList();
            DataSet ds = new DataSet();
            try
            {
                DataTable dt = new DataTable();
                dt.Clear();
                dt.Columns.Add("roleId");
                dt.Columns.Add("alertTypeId");
                dt.Columns.Add("redEnabled");
                dt.Columns.Add("redValue");
                dt.Columns.Add("amberEnabled");
                dt.Columns.Add("amberValue");
                for (int i = 0; i < c.alertRows.Count; i++)
                {
                    DataRow nRow = dt.NewRow();
                    nRow["roleId"] = c.alertRows[i].roleId;
                    nRow["alertTypeId"] = c.alertRows[i].alertTypeId;
                    nRow["redEnabled"] = c.alertRows[i].redEnabled;
                    nRow["redValue"] = c.alertRows[i].redValue;
                    nRow["amberEnabled"] = c.alertRows[i].amberEnabled;
                    nRow["amberValue"] = c.alertRows[i].amberValue;

                    dt.Rows.Add(nRow);
                }


                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_AlertsSettingsSet", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@alertTbl", dt);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                setAlertSettings(ref r, ref ds);
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
            return PartialView("AlertsPage", r);
        }




        private void setAlertSettings(ref AlertList r, ref DataSet ds)
        {

            r.roles = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new Role()
            {
                roleId = (int)spR["roleId"],
                roleName = (string)spR["roleName"]
            }).ToList();


            r.alertTypes = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new AlertType()
            {
                alertTypeId = (int)spR["alertTypeId"],
                alertName = (string)spR["alertName"],
                alertType = (string)spR["alertType"]
            }).ToList();

            DataView dv = new DataView(ds.Tables[2]);
            foreach (AlertType Alert in r.alertTypes)
            {
                dv.RowFilter = "alertTypeId=" + Alert.alertTypeId;
                Alert.alertSettings = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new AlertSetting()
                {
                    redEnabled = (bool)spR["redEnabled"],
                    amberEnabled = (bool)spR["amberEnabled"],
                    redValue = (short)spR["redValue"],
                    amberValue = (short)spR["amberValue"]
                }).ToList();

            }
        }


    

    }
}