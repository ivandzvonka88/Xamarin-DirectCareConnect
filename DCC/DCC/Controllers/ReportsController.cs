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
using DCC.Models.Reports;
using DCC.SQLHelpers.Helpers;

namespace DCC.Controllers
{
    public class ReportsController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;

        public ReportsController()
        {
            sqlHelper = new SQLHelper();
        }

        [Authorize]
        public async Task<ActionResult> Index()
        {
            ServiceOptions r = new ServiceOptions();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ReportsGetServiceOptions", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                r.services = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new Option()
                {
                    value = Convert.ToString(spR["serviceId"]),
                    name = (string)spR["name"],
                }).ToList();

                r.allQuestions = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new Question()
                {
                    questionId = (int)spR["questionId"],
                    question = (string)spR["title"]
                }).ToList();

            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
          
            ds.Dispose();

            setViewModelBase(r);

            return View(r);
        }

        [Authorize]
        public async Task<ActionResult> GetReportsPage(int serviceId)
        {

            ServiceInfo r = new ServiceInfo();
            r.serviceId = serviceId;
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ReportsGetInfo", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@serviceId", serviceId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                r.periodicQuestions = setQuestions(ref ds, 0);
                r.goalAreas = setGoalAreas(ref ds, 1);
                r.sessionQuestions = setQuestions(ref ds, 3);
                r.isEvaluation = (bool)ds.Tables[4].Rows[0].ItemArray[0];
                r.isTherapy = (bool)ds.Tables[4].Rows[0].ItemArray[1];
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
            return PartialView("ReportView", r);
        }

        [Authorize]
        public async Task<ActionResult> GetPeriodicReportView(int serviceId)
        {

            ServiceInfo r = new ServiceInfo();
            r.serviceId = serviceId;
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ReportsGetPeriodicQuestions", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@serviceId", serviceId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                r.periodicQuestions = setQuestions(ref ds, 0);
              
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
            return PartialView("PeriodicQuestionView", r);
        }

        [Authorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SavePeriodicQuestions(ServiceInfo r)
        {
            DataSet ds = new DataSet();
            try
            {

                DataTable dt = new DataTable();
                dt.Clear();
                dt.Columns.Add("questionId");

                dt.Columns.Add("isRequired");
                dt.Columns.Add("prepopulate");
                dt.Columns.Add("orderNumber");
                dt.Columns.Add("pocShared");
                dt.Columns.Add("supervisorOnly");
                for (int i = 0; i < r.periodicQuestions.Count; i++)
                {
                    DataRow nRow = dt.NewRow();
                    nRow["questionId"] = r.periodicQuestions[i].questionId;
                    nRow["isRequired"] = r.periodicQuestions[i].isRequired;
                    nRow["prepopulate"] = r.periodicQuestions[i].prepopulate;
                    nRow["orderNumber"] = r.periodicQuestions[i].orderNumber;
                    nRow["pocShared"] = r.periodicQuestions[i].sharedQuestion;
                    nRow["supervisorOnly"] = r.periodicQuestions[i].supervisorOnly;
                    dt.Rows.Add(nRow);
                }
                await  Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ReportsSavePeriodicQuestions", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@serviceId", r.serviceId);
                        cmd.Parameters.AddWithValue("@questions", dt);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.periodicQuestions = setQuestions(ref ds, 0);
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
            return PartialView("PeriodicQuestionView", r);
        }


        [Authorize]
        public async Task<ActionResult> GetSessionReportView(int serviceId)
        {

            ServiceInfo r = new ServiceInfo();
            r.serviceId = serviceId;
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ReportsGetSessionQuestions", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@serviceId", serviceId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                r.sessionQuestions = setQuestions(ref ds, 0);

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
            return PartialView("SessionQuestionView", r);
        }

        [Authorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SaveSessionQuestions(ServiceInfo r)
        {
            DataSet ds = new DataSet();
            try
            {

                DataTable dt = new DataTable();
                dt.Clear();
                dt.Columns.Add("questionId");

                dt.Columns.Add("isRequired");
                dt.Columns.Add("prepopulate");
                dt.Columns.Add("orderNumber");
                dt.Columns.Add("pocShared");
                dt.Columns.Add("supervisorOnly");
                for (int i = 0; i < r.sessionQuestions.Count; i++)
                {
                    DataRow nRow = dt.NewRow();
                    nRow["questionId"] = r.sessionQuestions[i].questionId;
                    nRow["isRequired"] = r.sessionQuestions[i].isRequired;
                    nRow["prepopulate"] = r.sessionQuestions[i].prepopulate;
                    nRow["orderNumber"] = r.sessionQuestions[i].orderNumber;
                    nRow["pocShared"] = r.sessionQuestions[i].sharedQuestion;
                    nRow["supervisorOnly"] = false;
                    dt.Rows.Add(nRow);
                }
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ReportsSaveSessionQuestions", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@serviceId", r.serviceId);
                        cmd.Parameters.AddWithValue("@questions", dt);

                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.sessionQuestions = setQuestions(ref ds, 0);
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
            return PartialView("SessionQuestionView", r);

        }


        [Authorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SaveGoalArea(GoalArea g)
        {

            ServiceInfo r = new ServiceInfo();

            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ReportsSaveGoalArea", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@serviceId", g.serviceId);
                        cmd.Parameters.AddWithValue("@goalAreaId", g.goalAreaId);
                        cmd.Parameters.AddWithValue("@name", g.name);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.goalAreas = setGoalAreas(ref ds, 0);

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
            return PartialView("GoalAreaView", r);

        }
      


        [Authorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> RemoveGoalArea(GoalArea g)
        {

            ServiceInfo r = new ServiceInfo();

            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ReportsRemoveGoalArea", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@serviceId", g.serviceId);
                        cmd.Parameters.AddWithValue("@goalAreaId", g.goalAreaId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.goalAreas = setGoalAreas(ref ds, 0);

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
            return PartialView("GoalAreaView", r);

        }

        private List<Question> setQuestions(ref DataSet ds, int tableIdx)
        {
            List<Question> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new Question()
            {
                questionId = (int)spR["questionId"],
                question = (string)spR["title"],
                orderNumber = (short)spR["orderNumber"],
                prepopulate = (bool)spR["prepopulate"],
                isRequired = (bool)spR["isRequired"],
                supervisorOnly = (bool)spR["supervisorOnly"],
                sharedQuestion = (bool)spR["PocShared"]
            }).ToList();
            return r;
        }
        private List<GoalArea> setGoalAreas(ref DataSet ds, int tableIdx)
        {
            List<GoalArea> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new GoalArea()
            {
                goalAreaId = (int)spR["GoalAreaId"],
                name = (string)spR["name"],
                isActive = (bool)spR["isActive"]
            }).ToList();
            return r;
        }
    }
}