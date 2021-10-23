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

namespace DCC.Controllers
{
    public class MessageController : DCCBaseController
    {

        private readonly SQLHelper sqlHelper;

        public MessageController()
        {
            sqlHelper = new SQLHelper();
        }

        [AJAXAuthorize]
        public async Task<ActionResult> Index()
        {
            StaffInit r = new StaffInit();


            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_StaffGetStaffNewMessage", cn)
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
            if (r.er.code == 0)
            {
                r.staffList = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new Staff()
                {
                    sendBirdUserId = UserClaim.coid + "-" + spR["prid"],
                    name = (string)spR["nm"],
                    deleted = (bool)spR["deleted"],
                }).ToList();

            }
            ds.Dispose();
            setViewModelBase((ViewModelBase)r);
            return View(r);
        }



    }
}




