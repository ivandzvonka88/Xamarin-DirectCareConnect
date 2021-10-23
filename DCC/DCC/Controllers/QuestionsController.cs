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
using DCC.Models.Questions;
using DCC.SQLHelpers.Helpers;

namespace DCC.Controllers
{
    public class QuestionsController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;

        public QuestionsController()
        {
            sqlHelper = new SQLHelper();
        }

        [AJAXAuthorize]
        public async Task<ActionResult> Index()
        {
            QuestionList r = new QuestionList();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_QuestionGetQuestions", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                r.questions = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new Question()
                {
                    questionId = (int)spR["QuestionId"],
                    title = (string)spR["Title"],
                    valueType = (string)spR["name"],
                    valueTypeId = (short)spR["id"],
                    maxValue = spR["MaxValue"] == DBNull.Value ? "" : Convert.ToString(spR["MaxValue"]),
                    minValue = spR["MinValue"] == DBNull.Value ? "" : Convert.ToString(spR["MinValue"]),
                    isActive = (bool)spR["isActive"],
                    isActiveStr = (bool)spR["isActive"] == false ? "NO" : "YES"

                }).ToList();
                r.valueIdOptions = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new Option()
                {
                    value = (short)spR["id"],
                    name = (string)spR["name"]

                }).ToList();
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            ds.Dispose();
         

            setViewModelBase((ViewModelBase)r);

            return View(r);
        }

        [AJAXAuthorize]
        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
   

        private List<Question> setQuestions(ref DataSet ds, int tableIdx)
        {
            List<Question> r = ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new Question()
            {
                questionId = (int)spR["QuestionId"],
                title = (string)spR["Title"],
                valueType = (string)spR["name"],
                valueTypeId = (short)spR["id"],
                maxValue = spR["MaxValue"] == DBNull.Value ? "" : Convert.ToString(spR["MaxValue"]),
                minValue = spR["MinValue"] == DBNull.Value ? "" : Convert.ToString(spR["MinValue"]),
                isActive = (bool)spR["isActive"],
                isActiveStr = (bool)spR["isActive"] == false ? "NO" : "YES"

            }).ToList();
            return r;
        }


        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> SaveQuestion(Question q)
        {
            QuestionList r = new QuestionList();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_QuestionUpdateQuestion", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@questionId", q.questionId);
                        cmd.Parameters.AddWithValue("@title", q.title);
                        cmd.Parameters.AddWithValue("@valueTypeId", q.valueTypeId);
                        cmd.Parameters.AddWithValue("@minValue", ((object)q.minValue) ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@maxValue", ((object)q.maxValue) ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@isActive", q.isActive);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        return ds;
                    }
                });
                r.questions = setQuestions(ref ds, 0);
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
            return Json(r,JsonRequestBehavior.AllowGet);
        }

        [AJAXAuthorize]
        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public JsonResult GetAllQuestions(int? questionId = null)
        {
            QuestionList r = new QuestionList();

            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_QuestionGetQuestions", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@QuestionId", questionId);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);

                }
                r.questions = setQuestions(ref ds, 0);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }

            ds.Dispose();
            return Json(r.questions.OrderBy(x=>x.title).ToList(), JsonRequestBehavior.AllowGet);
        }
    }
}