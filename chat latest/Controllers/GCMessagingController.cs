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
using DCC.Models.Staff;
using DCC.SQLHelpers.Helpers;
using DCCHelper;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace DCC.Controllers
{
    public class GCMessagingController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;

        public GCMessagingController()
        {
            sqlHelper = new SQLHelper();
        }

        [AJAXAuthorize]
        public async Task<ActionResult> Index()
        {
            StaffMessagingInit r = new StaffMessagingInit();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_StaffGetStaffMessageList", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            r.sendbirdId = ds.Tables[0].Rows[0].ItemArray[0].ToString();
            if (r.er.code == 0)
            {
                r.staffList = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new StaffMember()
                {
                    id = spR["sendbirdId"].ToString(),
                    name = (string)spR["nm"]
                }).ToList();

            }
            ds.Dispose();
            setViewModelBase((ViewModelBase)r);
            return View(r);
        }
    }
}