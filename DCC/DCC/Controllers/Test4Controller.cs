using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Configuration;
using System.Net;
using System.Data;
using System.Data.SqlClient;

using DCC.Models;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Microsoft.WindowsAzure.Storage;
using System.Runtime.ExceptionServices;
using DocumentFormat.OpenXml.Office2016.Drawing.Charts;
using DocumentFormat.OpenXml.Drawing;
using DHTMLX.Scheduler.RecurringEvents;
using DCC.Helpers;

namespace DCC.Controllers
{

    public class SchedulerEventNew: DHTMLX.Scheduler.RecurringEvents.SchedulerEvent
    {
        public int client_id { get; set; }
        public int service_id { get; set; }
        public int provider_id { get; set; }

        public string client_fn { get; set; }

        public string client_ln { get; set; }
        public string service_name { get; set; }

        public string ClientFullName { get; set; }

        public bool isActive { get; set; }


    }





    public class Test4Controller : DCCBaseController
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> GetSchedule()
        {
            Er er = new Er();

            List<Schedule> scheduleList = new List<Schedule>();
       
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ScheduleGetSchedules", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                       

                        cmd.Parameters.AddWithValue("@prids", 903);
                        cmd.Parameters.AddWithValue("@getAll", 1);
                        cmd.Parameters.AddWithValue("@providerId", 903);

                        if (UserClaim.userLevel == "SuperAdmin")
                        {
                            cmd.Parameters.AddWithValue("@IsAdmin", 1);
                        }
                        cmd.Parameters.AddWithValue("@startDate", "12/28/2020");
                        cmd.Parameters.AddWithValue("@endDate", "1/4/2021");

                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(ds);
                    }
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }

            List<SchedulerEventNew> data;



            data = ds.Tables[0].Rows.Cast<DataRow>().Select(spR =>
            {
                var sch = new SchedulerEventNew()
                {
                    id = Convert.ToString(spR["id"]),
                    client_id = (int)spR["client_ID"],
                    
                    service_id = (int)spR["service_ID"],
                    provider_id = (int)spR["provider_ID"],
                    client_fn = (string)spR["client_fn"],
                    client_ln = (string)spR["client_ln"],
                    service_name = (string)spR["service_name"],
                    start_date = (DateTime)spR["start_date"],
                    end_date = (DateTime)spR["end_date"],

                    ClientFullName = ExtensionsMethods.GetValueOrDefault<string>(spR, "client_fn") + " " + ExtensionsMethods.GetValueOrDefault<string>(spR, "client_ln"),
                    rec_type = (string)spR["rec_type"],
                    text = (string)spR["text"],
                    isActive = (bool)spR["is_active"]

                };

                if (spR["event_pid"] != DBNull.Value)
                {
                    sch.event_pid = Convert.ToString(spR["event_pid"]);
                }
                if (spR["event_length"] != DBNull.Value)
                {
                    sch.event_length = (long)spR["event_length"];
                }

                return sch;
            }).ToList();
            var helper = new RecurringEventsHelper
            {
                OccurrenceTimestampInUtc = true
            };

         //   var helper = new RecurringEventsHelper();
            var items = helper.GetOccurrences(data, new DateTime(2020, 12, 28), new DateTime(2021, 1, 4));





            ds.Dispose();
            return Json(er);
        }



    }
}