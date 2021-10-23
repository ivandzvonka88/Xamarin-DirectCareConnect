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
using Newtonsoft.Json.Linq;

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
            //check if user exists on SendBird
            string url = "https://api-F742C54C-036E-4DD0-B74D-E59E38ED7B30.sendbird.com/v3/users/";
            string token = "42b4f9494342b49f4334e0f8a9a1a65a5a969a6f";
            await ChekSBUser(url, UserClaim.staffname,  r.sendbirdId, token);

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

        [AJAXAuthorize]
        public async Task<ActionResult> Chat()
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

        public async Task<HttpResponseMessage> ChekSBUser(string url, string name, string id, string token)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Api-Token", token);
                var response = await client.GetAsync(url + id);
                var contentJson = await response.Content.ReadAsStringAsync();
                dynamic content = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(contentJson);

                //user doesn't not exist on SB, proceed to create one
                if (content.error != null && content.error.Value)
                {
                    var data = new { user_id = id, profile_url = "", nickname = name };
                    string dataJson = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    var user_create_response = await client.PostAsync(url, new StringContent(dataJson, Encoding.UTF8, "application/json"));
                    var user_create_contentJson = await user_create_response.Content.ReadAsStringAsync();

                }

                return response;
            }
        }
    }
}