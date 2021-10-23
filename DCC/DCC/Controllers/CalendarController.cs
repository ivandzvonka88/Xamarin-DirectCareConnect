using DCC.Helpers;
using DCC.Models;
using DCC.Models.Clients;
using DCC.Models.Services;
using DCC.Models.Staff;
using DCC.SQLHelpers.Helpers;
using DHTMLX.Common;
using DHTMLX.Scheduler;
using DHTMLX.Scheduler.Controls;
using DHTMLX.Scheduler.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace DCC.Controllers
{
    public class CalendarController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;

        private class Option
        {
            public int key { get; set; }
            public string label { get; set; }
        }

        private class ScheduleResponse
        {
            public long id { get; set; }
            public string clientName { get; set; } = "";
            public string clientLocation { get; set; } = "";
        }

        public CalendarController()
        {
            sqlHelper = new SQLHelper();
        }

        private DHXScheduler ConfigureScheduler()
        {
            var scheduler = new DHXScheduler(this);

            scheduler.Extensions.Add(SchedulerExtensions.Extension.Limit);
            scheduler.Extensions.Add(SchedulerExtensions.Extension.Collision);
            scheduler.Extensions.Add(SchedulerExtensions.Extension.Recurring);
            scheduler.Extensions.Add(SchedulerExtensions.Extension.Tooltip);
            scheduler.Config.collision_limit = 1;
            scheduler.Config.show_loading = true;
            scheduler.Skin = DHXScheduler.Skins.Material;

            if (UserClaim.userLevel == "Provider" || UserClaim.userLevel == "TherapyAssistant" || UserClaim.userLevel == "TherapySupervisor")
            {
                scheduler.Config.isReadonly = true;
            }

            // scheduler.Templates.event_text = "Client: {ClientFullName}";
            scheduler.Templates.event_bar_text = "Client: {ClientFullName}";
            scheduler.Templates.tooltip_text = @"<b>Service:</b> {service_name}<br/>
                <b>Location:</b> {Location}<br />
            ";
            scheduler.InitialValues.Add("text", "");

            scheduler.BeforeInit.Add("pre_init();");
            scheduler.AfterInit.Add("post_init();");

            // ajax call for Data on loading
            scheduler.LoadData = true;

            // send changes to the Save action
            scheduler.EnableDataprocessor = true;
            scheduler.EnableDynamicLoading(SchedulerDataLoader.DynamicalLoadingMode.Day);
            scheduler.UpdateFieldsAfterSave(); // this line activates the required mode

            return scheduler;
        }
        private IEnumerable<Option> AddDefaultOption(IEnumerable<Option> options, string defaultLabel)
        {
            return new List<Option>() {
                new Option()
                {
                    key = 0,
                    label = defaultLabel
                }
            }.Concat(options);
        }

        private string getAddress(DataRow dr)
        {
            var addr = ExtensionsMethods.GetValueOrDefault<string>(dr, "ad1") + " " +
                ExtensionsMethods.GetValueOrDefault<string>(dr, "cty") + " " +
                ExtensionsMethods.GetValueOrDefault<string>(dr, "st");
            var zip = ExtensionsMethods.GetValueOrDefault<string>(dr, "zip");

            if (String.IsNullOrEmpty(zip))
            {
                return addr;
            }

            return addr + ", " + zip;
        }

        private async Task<List<Client>> getClients(int providerID)
        {
            DataSet ds = new DataSet();

            await Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ClientsGetClientsByProvider", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@providerID", providerID);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
            });

            var clients = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new Client()
            {
                id = (int)spR["clsvid"],
                name = (string)spR["fn"] + " " + (string)spR["ln"]
            }).ToList();

            return clients;
        }
        public async Task<ActionResult> GetClients(string providerID)
        {
            var clients = await getClients(Int32.Parse(providerID));
            var options = clients.Select(client => new Option()
            {
                label = client.name,
                key = client.id
            });

            return Json(AddDefaultOption(options, "--Select Client--"), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetServices(string clientID)
        {
            SQLHelper sqlHelper = new SQLHelper();

            DataSet dataSet = new DataSet();
            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_ClientGetClientServices", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@clsvID", clientID);
                sqlHelper.ExecuteSqlDataAdapter(cmd, dataSet);
            }

            var services = dataSet.Tables[0].Rows.Cast<DataRow>().Select(spR => new Option() {
                label = Convert.ToString(spR["name"]),
                key = Convert.ToInt32(spR["ServiceId"])
            }).ToList();

            return Json(AddDefaultOption(services, "--Select Service--"), JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> Index()
        {
            if (UserClaim.userLevel != "Director" && UserClaim.userLevel != "Provider" && UserClaim.userLevel != "TherapyAssistant" && UserClaim.userLevel != "TherapySupervisor" && UserClaim.userLevel != "Supervisor" && UserClaim.userLevel != "AssistantDirector" && UserClaim.userLevel != "SuperAdmin")
            {
                return Json("You are not authorized to see this page!", JsonRequestBehavior.AllowGet);
            }

            DataSet dataSet = new DataSet();
            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_CalendarGetProviders", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@prId", UserClaim.prid);
                cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                sqlHelper.ExecuteSqlDataAdapter(cmd, dataSet);
            }

            ViewBag.StaffList = dataSet.Tables[0].Rows.Cast<DataRow>().Select(spR => new Staff()
            {
                name = Convert.ToString(spR["fn"]) + " " + Convert.ToString(spR["ln"]),
                id = Convert.ToInt32(spR["prId"])
            }).ToList();

            ViewBag.UserClaim = new EmptyView();
            setViewModelBase(ViewBag.UserClaim);
            
            return View(ConfigureScheduler());
        }
        public async Task<ContentResult> Data(int? providerID, DateTime from, DateTime to)
        {
            List<Schedule> scheduleList = new List<Schedule>();
            var prId = providerID ?? UserClaim.prid;

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
                        //if (UserClaim.supervisoryLevel > 3)
                        //{
                        //    cmd.Parameters.AddWithValue("@getAll", 1);
                        //}else if (UserClaim.supervisoryLevel == 1)
                        //{
                        //    cmd.Parameters.AddWithValue("@prids", UserClaim.prid.ToString());
                        //}
                        //else if(UserClaim.supervisoryLevel==2)
                        //{
                        //    cmd.Parameters.AddWithValue("@prids", UserClaim.prid.ToString());
                        //    cmd.Parameters.AddWithValue("@getAllProvider", 1);
                        //}

                        cmd.Parameters.AddWithValue("@prids", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@getAll", 1);
                        cmd.Parameters.AddWithValue("@providerId", prId);

                        if (UserClaim.userLevel == "SuperAdmin")
                        {
                            cmd.Parameters.AddWithValue("@IsAdmin", 1);
                        }
                        cmd.Parameters.AddWithValue("@startDate", from);
                        cmd.Parameters.AddWithValue("@endDate", to);

                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(ds);
                    }
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }

            try
            {
                scheduleList = ds.Tables[0].Rows.Cast<DataRow>().Select(spR =>
                {
                    var sch = new Schedule()
                    {
                        id = (int)spR["id"],
                        client_id = (int)spR["client_ID"],
                        service_id = (int)spR["service_ID"],
                        provider_id = (int)spR["provider_ID"],
                        client_fn = (string)spR["client_fn"],
                        client_ln = (string)spR["client_ln"],
                        service_name = (string)spR["service_name"],
                        start_date = (DateTime)spR["start_date"],
                        end_date = (DateTime)spR["end_date"],
                        ClientFullName = ExtensionsMethods.GetValueOrDefault<string>(spR, "client_fn") + " " + ExtensionsMethods.GetValueOrDefault<string>(spR, "client_ln"),
                        Location = getAddress(spR),
                        rec_type = (string)spR["rec_type"],
                        text = (string)spR["text"],
                        isActive = (bool)spR["is_active"]
                    };

                    if (spR["event_pid"] != DBNull.Value)
                    {
                        sch.event_pid = (int)spR["event_pid"];
                    }
                    if (spR["event_length"] != DBNull.Value)
                    {
                        sch.event_length = (long)spR["event_length"];
                    }

                    return sch;
                }).ToList();
            }
            catch (Exception e)
            {
                throw e;
            }
            ds.Dispose();

            var data = new SchedulerAjaxData(scheduleList);

            return data;
        }

        public ContentResult Save(int? id, FormCollection actionValues)
        {
            var action = new DataAction(actionValues);
            ScheduleResponse sch = new ScheduleResponse();

            try
            {
                var changedSchedule = DHXEventsHelper.Bind<Schedule>(actionValues);

                if (changedSchedule.rec_type == "null") changedSchedule.rec_type = null;

                if (action.Type != DataActionTypes.Delete && changedSchedule.rec_type != "none" && IsCollidesWithOthers(changedSchedule))
                {
                    action.Type = DataActionTypes.Error;
                    action.Message = "Confliction happened. Try again!";

                    return new AjaxSaveResponse(action);
                }

                switch (action.Type)
                {
                    case DataActionTypes.Insert:
                        if (changedSchedule.client_id == 0 || changedSchedule.service_id.GetValueOrDefault(0) == 0 || changedSchedule.provider_id.GetValueOrDefault(0) == 0)
                        {
                            throw new Exception("Missing information");
                        }
                        sch = CRUD(changedSchedule, "INSERT");

                        if (changedSchedule.rec_type == "none") //delete one event from the serie
                            action.Type = DataActionTypes.Delete;
                        break;
                    case DataActionTypes.Delete:
                        sch = CRUD(changedSchedule, "DELETE");
                        break;
                    case DataActionTypes.Update:
                    default:
                        sch = CRUD(changedSchedule, "UPDATE");
                        break;
                }
                action.TargetId = sch.id;
            }
            catch (Exception e)
            {
                //action.Type = DataActionTypes.Error;
                action.Message = e.Message;
            }

            var response = new AjaxSaveResponse(action);

            response.UpdateFields(new Dictionary<string, object>()
            {
                {"ClientFullName", sch.clientName},
                {"Location", sch.clientLocation}
            });

            return response;
        }

        private bool IsCollidesWithOthers(Schedule schedule)
        {
            return false;
            
        }
 
        private ScheduleResponse CRUD(Schedule schedule, string mode)
        {
            DataSet dataSet = new DataSet();

            try
            {
                SQLHelper sqlHelper = new SQLHelper();
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_GuardianAddNewSchedule", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.AddWithValue("@clientId", schedule.client_id);
                    cmd.Parameters.AddWithValue("@serviceId", schedule.service_id);
                    cmd.Parameters.AddWithValue("@providerId", schedule.provider_id);
                    cmd.Parameters.AddWithValue("@startDate", schedule.start_date);
                    cmd.Parameters.AddWithValue("@endDate", schedule.end_date);
                    cmd.Parameters.AddWithValue("@recurringType", schedule.rec_type);
                    cmd.Parameters.AddWithValue("@eventLength", schedule.event_length);
                    cmd.Parameters.AddWithValue("@eventPId", schedule.event_pid);
                    cmd.Parameters.AddWithValue("@text", schedule.text);
                    cmd.Parameters.AddWithValue("@id", schedule.id);
                    cmd.Parameters.AddWithValue("@mode", mode);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, dataSet);

                    var clientInfo = dataSet.Tables[1].Rows[0];

                    return new ScheduleResponse()
                    {
                        clientName = ExtensionsMethods.GetValueOrDefault<string>(clientInfo, "fn") + " " + ExtensionsMethods.GetValueOrDefault<string>(clientInfo, "ln"),
                        clientLocation = getAddress(clientInfo),
                        id = Convert.ToInt64(dataSet.Tables[0].Rows[0][0])
                    };
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /*
        [HttpGet]
        [AJAXAuthorize]
        public ActionResult ScheduleDetails(int id = 0)
        {
            SQLHelper sqlHelper = new SQLHelper();
            ScheduleInit r = new ScheduleInit();

            DataSet dataSet = new DataSet();
            try
            {

                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ScheduleEdit", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@shID", id);
                    //  cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, dataSet);
                }
                r.schedule = dataSet.Tables[0].Rows.Cast<DataRow>().Select(spR => new Schedule()
                {
                    ClientFullName = Convert.ToString(spR["fn"]) + " " + Convert.ToString(spR["ln"]),
                    id = (int)(spR["id"]),
                    service_name = Convert.ToString(spR["Name"]),
                    scheduled_at = (DateTime)spR["scheduled_at"],
                    endDate = (DateTime)spR["endDate"],
                    client_ID = (int)(spR["clsvID"]),
                    recurring_type = (int)(spR["recurring_type"]),
                    AdditionalInfo = Convert.ToString(spR["AdditionalInfo"]),
                    providerID = (int?)(spR["providerId"]),
                    providerName = Convert.ToString(spR["providerName"]),
                    service_id = (int?)(spR["serviceId"]),
                    reasonCodeId = spR["reasonCodeId"]==DBNull.Value ? 0 :Convert.ToInt16(spR["reasonCodeId"]),
                    resolutionCodeId = spR["resolutionCodeId"] == DBNull.Value ? 0 : Convert.ToInt16(spR["resolutionCodeId"])
                    //missedVisit=Convert.ToBoolean(spR["missedVisit"])
                }).FirstOrDefault();
                if (r.schedule.resolutionCodeId.HasValue && r.schedule.resolutionCodeId.Value > 0)
                {
                    r.schedule.missedVisit = true;
                }
                else
                {
                    r.schedule.missedVisit = false;
                }
                //short? sVal = dr["ColName"] is System.DBNull ? null : (short?)(dr["ColName"]);

                r.reasonCodes = dataSet.Tables[1].Rows.Cast<DataRow>().Select(spR => new AZSandataVisitChangeReasonCode()
                {
                    Description = Convert.ToString(spR["Description"]),
                    ReasonCodeID = (int)(spR["ReasonCodeID"])
                }).ToList();

                r.resolutionCodes = dataSet.Tables[2].Rows.Cast<DataRow>().Select(spR => new AZSandataResolutionCode()
                {
                    Description = Convert.ToString(spR["Description"]),
                    ResolutionCodeId = (int)(spR["ResolutionCodeId"])
                }).ToList();

                DataSet dataSet2 = new DataSet();
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ClientGetClientServices", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@clsvID", r.schedule.client_ID);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, dataSet2);
                }
                r.services = dataSet2.Tables[0].Rows.Cast<DataRow>().Select(spR => new Service()
                {
                    name = Convert.ToString(spR["name"]),
                    serviceId = Convert.ToInt32(spR["ServiceId"])
                }).ToList();


                r.schedule.StartDate = r.schedule.scheduled_at.ToString("yyyy-MM-dd");
                r.schedule.StartTime = r.schedule.scheduled_at.ToString("HH:mm:ss");

                if (r.schedule.endDate != null)
                {
                    r.schedule.EndDate = r.schedule.endDate.ToString("yyyy-MM-dd");
                    r.schedule.EndTime = r.schedule.endDate.ToString("HH:mm:ss");
                }
            }
            catch (Exception e)
            {

            }


            return PartialView("ModalServiceSchedule", r);
        }
        */

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateSchedule(FormCollection fc)
        {
            string ReturnMsg = "";
            try
            {
                SQLHelper sqlHelper = new SQLHelper();
                DateTime ddt;
                int IsMissed =Convert.ToInt32(fc["IsMissedCount"]);
                DateTime startDate = DateTime.Parse(fc["StartDateSelect"] + " " + fc["StartTimeSelect"]);
                ReturnMsg = startDate.ToString("dd MMMM yyyy 'at' hh:mm tt");
                //DateTime endDate = DateTime.Parse(fc["EndDateSelect"] + " " + fc["EndTimeSelect"]);
                DataSet dataSet = new DataSet();
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ScheduleUpdateSchedule", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@scheduleId", Convert.ToInt32(fc["ScheduleID"]));
                    cmd.Parameters.AddWithValue("@clientId", Convert.ToInt32(fc["ClientSelection"]));
                    cmd.Parameters.AddWithValue("@serviceId", fc["ServiceSelection"]);
                    cmd.Parameters.AddWithValue("@startDate", startDate);
                    cmd.Parameters.AddWithValue("@endDate", startDate.AddHours(1));
                    cmd.Parameters.AddWithValue("@recurringType", Convert.ToInt32(fc["RecurringOption"]));
                    cmd.Parameters.AddWithValue("@additionalInfo", fc["comments"]);
                    if (IsMissed>0)
                    {
                        cmd.Parameters.AddWithValue("@missedVisit", 1);
                        cmd.Parameters.AddWithValue("@reasonCodeId", Convert.ToInt32(fc["SelecteMissingReason"]));
                        cmd.Parameters.AddWithValue("@ResolutionCodeId", Convert.ToInt32(fc["SelecteMissingResolution"]));
                    }
                    sqlHelper.ExecuteSqlDataAdapter(cmd, dataSet);
                }
            }
            catch (Exception ex)
            {

            }
            if (fc["IsFromHomePage"] == "YES")
            {

                return Json(ReturnMsg);
            }

            return Json("success");
        }
        public ActionResult ChangeScheduleDate(CalenderSchedule sc)
        {
            DateTime startDate = Convert.ToDateTime(sc.start_date);
            DateTime endDate = Convert.ToDateTime(sc.end_date);
            try
            {
                SQLHelper sqlHelper = new SQLHelper();

                DataSet dataSet = new DataSet();
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ScheduleUpdateSchedule", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@isDateUpdate", 1);
                    cmd.Parameters.AddWithValue("@scheduleId", sc.ScheduleID);
                    cmd.Parameters.AddWithValue("@startDate", startDate);
                    cmd.Parameters.AddWithValue("@endDate", endDate);
                    if (UserClaim.userLevel == "Provider")
                    {
                        cmd.Parameters.AddWithValue("@IsProvider", 1);
                    }
                    sqlHelper.ExecuteSqlDataAdapter(cmd, dataSet);
                }
            }
            catch (Exception ex)
            {
                return Json("error");
            }
            return Json("success");
        }
        public ActionResult AddNewSchedule()
        {
            SQLHelper sqlHelper = new SQLHelper();
            ScheduleInit r = new ScheduleInit();


            DataSet dataSet = new DataSet();
            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_ClientsGetClientList", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                sqlHelper.ExecuteSqlDataAdapter(cmd, dataSet);
            }
            r.Clients = dataSet.Tables[0].Rows.Cast<DataRow>().Select(spR => new Client()
            {
                id = (int)spR["clsvid"],
                name = (string)spR["nm"],
                deleted = (bool)spR["deleted"]
            }).ToList();

            setViewModelBase((ViewModelBase)r);
            return PartialView("AddNewSchedule", r);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Schedule(FormCollection fc)
        {
            DataSet dataSet = new DataSet();
            DateTime startDate = DateTime.Parse(fc["StartDateSelect"] + " " + fc["StartTimeSelect"]);

            try
            {
                SQLHelper sqlHelper = new SQLHelper();
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_GuardianAddNewSchedule", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    //cmd.Parameters.AddWithValue("@guardianUserId", UserClaim.uid);
                    cmd.Parameters.AddWithValue("@clientId", Convert.ToInt32(fc["ClientSelection"]));
                    cmd.Parameters.AddWithValue("@serviceIds", fc["ServiceSelection"]);
                    cmd.Parameters.AddWithValue("@startDate", startDate);
                    cmd.Parameters.AddWithValue("@endDate", startDate.AddHours(1));
                    cmd.Parameters.AddWithValue("@recurringType", Convert.ToInt32(fc["RecurringOption"]));
                    cmd.Parameters.AddWithValue("@additionalInfo", fc["comments"]);
                    if (fc["ProviderSelect"] != "--Select Provider--")
                    {
                        cmd.Parameters.AddWithValue("@providerId", fc["ProviderSelect"]);
                    }
                    sqlHelper.ExecuteSqlDataAdapter(cmd, dataSet);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //return RedirectToLocal("/Schedules");
            return Json("success", JsonRequestBehavior.AllowGet);
        }
    }

    public class CalenderSchedule
    {
        public int id { get; set; }
        public string text { get; set; }
        public string start_date { get; set; }
        public string end_date { get; set; }
        public int ScheduleID { get; set; }
        public string color { get; set; }
        public string textColor { get; set; }
    }
}