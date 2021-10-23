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
using DCC.Models.CredentialSettings;
using DCC.SQLHelpers.Helpers;

namespace DCC.Controllers
{
    public class CredentialSettingsController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;

        public CredentialSettingsController()
        {
            sqlHelper = new SQLHelper();
        }


        [AJAXAuthorize]
        public ActionResult Index()
        {
            EmptyView r = new EmptyView();
            setViewModelBase((ViewModelBase)r);
            return View(r);
        }


        [AJAXAuthorize]
        public async Task<ActionResult> GetReqCredentials()
        {
            RequiredCredentials r = new RequiredCredentials();


            DataSet ds =new DataSet();
            try
            {
                ds = await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_CredentialSettingsGet", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        return ds;
                    }
                });
                setCredentialData(ref r, ref ds);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            ds.Dispose();
            if (r.er.code != 0)
            {

                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;


            }
            else
            return PartialView("CredentialSettings", r);
        }


        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SetReqCredentials(CredentialTableResp c)
        {
            RequiredCredentials r = new RequiredCredentials();


            DataTable dt = new DataTable();
            dt.Clear();
            dt.Columns.Add("roleId");
            dt.Columns.Add("credTypeId");
            dt.Columns.Add("required");
            dt.Columns.Add("blocking");
            for (int i = 0; i < c.credentialRows.Count; i++)
            {
                DataRow nRow = dt.NewRow();
                nRow["roleId"] = c.credentialRows[i].roleId;
                nRow["credTypeId"] = c.credentialRows[i].credTypeId;
                nRow["required"] = c.credentialRows[i].required;
                nRow["blocking"] = c.credentialRows[i].blocking;


                dt.Rows.Add(nRow);
            }



            DataSet ds = new DataSet();
            try
            {
                ds = await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_CredentialSettingsSet", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@credTbl", dt);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        return ds;
                    }
                });



                setCredentialData(ref r, ref ds);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            ds.Dispose();
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("CredentialSettings", r);
        }


        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> AddNewCredential(NewCredential c)
        {
            RequiredCredentials r = new RequiredCredentials();
            DataSet ds = new DataSet();
            try
            {
                ds = await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_CredentialSettingsAdd", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@credName", c.credName);
                        cmd.Parameters.AddWithValue("@roleSpecific", c.roleSpecific);

                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        return ds;
                    }
                });
                setCredentialData(ref r, ref ds);
            }

            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            ds.Dispose();
            if (r.er.code != 0)
            {
                Response.Write(r.er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else
                return PartialView("CredentialSettings", r);
        }

        private void setCredentialData(ref RequiredCredentials r, ref DataSet ds)
        {

            r.roles = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new Role()
            {
                roleId = (int)spR["roleId"],
                roleName = (string)spR["roleName"]
            }).ToList();


            r.credentialTypes = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new CredentialType()
            {
                credTypeId = (int)spR["credTypeId"],
                credName = (string)spR["credName"],
                roleSpecific = (bool)spR["roleSpecific"]
            }).ToList();

            DataView dv = new DataView(ds.Tables[2]);
            foreach (Role Role in r.roles)
            {
                if (Role.roleId == 7)
                {


                }
                dv.RowFilter = "roleId=" + Role.roleId;
                Role.credentialSettings = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new CredentialSetting()
                {
                    blocking = (bool)spR["blocking"],
                    required = (bool)spR["required"]
                }).ToList();

            }

            


        }


     
    }
}