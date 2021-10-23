using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using DCC.Models;
using System.Data;
using System.Data.SqlClient;
using DCC.SQLHelpers.Helpers;
using DCCHelper;
using DocumentFormat.OpenXml.Drawing;

namespace DCC.Controllers
{
    public class GuardiansController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;
        public GuardiansController()
        {
            sqlHelper = new SQLHelper();
        }

        [AJAXAuthorize]
        public async Task<ActionResult> Index()
        {
            GuardianList r = new GuardianList();
            setViewModelBase((ViewModelBase)r);

            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GuardianGetGuardiansList", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@companyId", UserClaim.coid);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.guardians = setGuardianList(ref ds, 0);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
         
            ds.Dispose();

            return View("Index", r);
        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> Guardian(int id)
        {
            GuardianPageData r = new GuardianPageData();
            r.userLevel = UserClaim.userLevel;
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GuardianGetGuardian", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@companyId", UserClaim.coid);
                        cmd.Parameters.AddWithValue("@guardianUId", id);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.guardianProfile = new GuardianProfile();
                setGuardianProfile(ref r, ref ds, 0);
                r.guardianClients = setGuardianClients(ref ds, 1);
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
                return PartialView("Guardian_Page", r);
        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenGuardianActivateDeactivateModal(int id)
        {
            GuardianProfile r = new GuardianProfile();
            Er er = new Er();
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GuardianGetProfile", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@companyId", UserClaim.coid);
                        cmd.Parameters.AddWithValue("@guardianUId", id);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                DataRow dr = ds.Tables[0].Rows[0];
                r.guardianUId = (int)dr["guardianUId"];
                r.firstName = (string)dr["firstName"];
                r.lastName = (string)dr["lastName"];

                r.deleted = (bool)dr["deleted"];
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else if (r.deleted)
                return PartialView("ModalActivateGuardian", r);
            else
                return PartialView("ModalDeactivateGuardian", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> ToggleGuardian(GuardianProfile s)
        {
            GuardianPageData r = new GuardianPageData();
            GuardianWindows w = new GuardianWindows();
            DataSet ds = new DataSet();
            DataSet ds2 = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GuardianActivateDeactivate", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@companyId", UserClaim.coid);
                        cmd.Parameters.AddWithValue("@guardianUId", s.guardianUId);
                        cmd.Parameters.AddWithValue("@deleted", s.deleted);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GuardianGetGuardianAll", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@companyId", UserClaim.coid);
                        cmd.Parameters.AddWithValue("@guardianUId", s.guardianUId);

                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds2);
                    }
                });

                GuardianList x = new GuardianList();
                x.guardians = setGuardianList(ref ds2, 0);

                w.nameList = RenderRazorViewToString("GuardianList", x);

                r.guardianProfile = new GuardianProfile();
                setGuardianProfile(ref r, ref ds2, 1);
                r.guardianClients = setGuardianClients(ref ds2, 2);
                w.guardianPage = RenderRazorViewToString("Guardian_Page", r);

            }
            catch (Exception ex)
            {
                w.er.code = 1;
                w.er.msg = ex.Message;

            }
            ds.Dispose();
         

            return Json(w);
        }


        [HttpGet]
        [AJAXAuthorize]
        public ActionResult OpenNewGuardianModal()
        {
            return PartialView("Modal_AddGuardian");
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> AddGuardian(GuardianProfile g)
        {

            GuardianPageData r = new GuardianPageData();
            GuardianWindows w = new GuardianWindows();
            r.userLevel = UserClaim.userLevel;
            DataTable dt = new DataTable();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetDesigneeByEmail", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@email", g.email);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, dt);
                    }
                });
                if (dt.Rows.Count != 0)
                {
                    DataRow dr = dt.Rows[0];
                    w.er.code = 1;
                    w.er.msg = " This email is in use by a designee " + dr["firstName"] + " " + dr["lastName"];
                }

            }
            catch (Exception ex)
            {
                w.er.code = 1;
                w.er.msg = ex.Message;
            }
            dt.Dispose();


            if (w.er.code == 0)
            {
                DataSet ds = new DataSet();
                DataSet ds2 = new DataSet();
                DataSet ds3 = new DataSet();
                try
                {
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                        {
                            SqlCommand cmd = new SqlCommand("sp_GuardianAdd", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                            cmd.Parameters.AddWithValue("@companyId", UserClaim.coid);
                            cmd.Parameters.AddWithValue("@firstName", g.firstName);
                            cmd.Parameters.AddWithValue("@lastName", g.lastName);
                            cmd.Parameters.AddWithValue("@phone", g.phone);
                            cmd.Parameters.AddWithValue("@email", g.email);
                            cmd.Parameters.AddWithValue("@addressLine1", g.addressLine1 == null ? "" : g.addressLine1);
                            cmd.Parameters.AddWithValue("@addressLine2", g.addressLine2 == null ? "" : g.addressLine2);
                            cmd.Parameters.AddWithValue("@city", g.city == null ? "" : g.city);
                            cmd.Parameters.AddWithValue("@state", g.state == null ? "" : g.state);
                            cmd.Parameters.AddWithValue("@postalCode", g.postalCode == null ? "" : g.postalCode);
                            cmd.Parameters.AddWithValue("@code", createCode(30));
                            cmd.Parameters.AddWithValue("@codeExpires", DateTime.UtcNow.AddDays(7));


                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    });
                    if (!ds.Tables[0].Columns.Contains("Error"))
                    {
                        int guardianUId = Convert.ToInt32(ds.Tables[0].Rows[0].ItemArray[0]);
                        await Task.Run(() =>
                        {
                            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                            {
                                SqlCommand cmd = new SqlCommand("sp_GuardianGetGuardianAll", cn)
                                {
                                    CommandType = CommandType.StoredProcedure
                                };
                                cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                                cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                                cmd.Parameters.AddWithValue("@companyId", UserClaim.coid);
                                cmd.Parameters.AddWithValue("@guardianUId", guardianUId);

                                sqlHelper.ExecuteSqlDataAdapter(cmd, ds2);
                            }
                        });

                        GuardianList x = new GuardianList();
                        x.guardians = setGuardianList(ref ds2, 0);

                        w.nameList = RenderRazorViewToString("GuardianList", x);

                        r.guardianProfile = new GuardianProfile();
                        setGuardianProfile(ref r, ref ds2, 1);
                        r.guardianClients = setGuardianClients(ref ds2, 2);
                        w.guardianPage = RenderRazorViewToString("Guardian_Page", r);


                        string registrationCode = createCode(30);

                        await Task.Run(() =>
                        {
                            using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                            {
                                SqlCommand cmd = new SqlCommand("sp_InviteGuardian", cn)
                                {
                                    CommandType = CommandType.StoredProcedure
                                };
                                cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                                cmd.Parameters.AddWithValue("@guardianUId", r.guardianProfile.guardianUId);
                                cmd.Parameters.AddWithValue("@code", registrationCode);
                                sqlHelper.ExecuteSqlDataAdapter(cmd, ds3);
                            }
                        });
                        DataRow dr = ds3.Tables[0].Rows[0];

                        string message =
                           dr["name"] +
                            "\r\n" +
                           "Please register by clicking on on the following link" +
                           "\r\n" +
                            "https://guardian.therapycorner.com/register/verification?guid=" + dr["code"] +
                           "\r\n" +
                           "If the registration text box is not prepoplulated, copy and paste the following registration link into the registration code text box" +
                           "\r\n" +
                            dr["code"];

                        string erMessage = CommunicationHelper.SendEmail((string)dr["email"], "Invitation to register from " + UserClaim.companyName, message, null);
                    }
                    else
                    {
                        DataRow dr = ds.Tables[0].Rows[0];
                        w.er.code = 1;
                        w.er.msg = dr["Error"].ToString();

                    }

                }
                catch (Exception ex)
                {
                    w.er.code = 1;
                    w.er.msg = ex.Message;
                }
                ds.Dispose();
                ds2.Dispose();
                ds3.Dispose();

            }



          

            return Json(w);
        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenGuardianEditModal(string id)
        {
            GuardianProfile r = new GuardianProfile();

            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GuardianGetGuardian", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@companyId", UserClaim.coid);
                        cmd.Parameters.AddWithValue("@guardianUId", id);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                DataRow dr = ds.Tables[0].Rows[0];
                r.firstName = (string)dr["firstName"];
                r.lastName = (string)dr["lastName"];
                r.email = (string)dr["email"];
                r.phone = (string)dr["phone"];
                r.addressLine1 = (string)dr["addressLine1"];
                r.addressLine2 = (string)dr["addressLine2"];
                r.city = (string)dr["city"];
                r.state = (string)dr["state"];
                r.postalCode = (string)dr["postalCode"];


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
                return PartialView("Modal_EditGuardian", r);
        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> UpdateGuardian(GuardianProfile g)
        {

            GuardianPageData r = new GuardianPageData();
            r.userLevel = UserClaim.userLevel;
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GuardianUpdate", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@companyId", UserClaim.coid);
                        cmd.Parameters.AddWithValue("@guardianUId", g.guardianUId);

                        cmd.Parameters.AddWithValue("@firstName", g.firstName);
                        cmd.Parameters.AddWithValue("@lastName", g.lastName);
                        cmd.Parameters.AddWithValue("@phone", g.phone);
                        cmd.Parameters.AddWithValue("@email", g.email);
                        cmd.Parameters.AddWithValue("@addressLine1", g.addressLine1 == null ? "" : g.addressLine1);
                        cmd.Parameters.AddWithValue("@addressLine2", g.addressLine2 == null ? "" : g.addressLine2);
                        cmd.Parameters.AddWithValue("@city", g.city == null ? "" : g.city);
                        cmd.Parameters.AddWithValue("@state", g.state == null ? "" : g.state);
                        cmd.Parameters.AddWithValue("@postalCode", g.postalCode == null ? "" : g.postalCode);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                r.userLevel = UserClaim.userLevel;
                r.guardianProfile = new GuardianProfile();
                setGuardianProfile(ref r, ref ds, 0);

               
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            return PartialView("Guardian_Profile", r);
        }


        [HttpPost]
        [AJAXAuthorize]
        public async Task<ActionResult> InviteGuardian(string id)
        {
            Er er = new Er();
            string registrationCode = createCode(30);
            DataSet ds = new DataSet();

            try
            {

                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                    {
                        SqlCommand cmd = new SqlCommand("sp_InviteGuardian", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@guardianUId", id);
                        cmd.Parameters.AddWithValue("@code", registrationCode);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }
            if (er.code == 0)
            {
                string erMessage = "";
                DataRow dr = ds.Tables[0].Rows[0];
                try
                {
                    string message =
                        dr["name"] +
                         "\r\n" +
                        "Please register by clicking on on the following link" +
                        "\r\n" +
                         "https://guardian.therapycorner.com/register/verification?guid=" + dr["code"] +
                        "\r\n" +
                        "If the registration text box is not prepoplulated, copy and paste the following registration link into the registration code text box" +
                        "\r\n" +
                         dr["code"];

                     erMessage = CommunicationHelper.SendEmail((string)dr["Email"], "Invitation to register from " + UserClaim.companyName, message, null);
                }
                catch (Exception ex)
                {
                    er.code = 1;
                    //msg = ex.Message;
                    er.msg = ex.Message;
                }

            }
            ds.Dispose();


            if (er.code == 0)
                er.msg = "Your message has been sent";

            return Json(er);
        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> OpenAddClientModal()
        {
            DataSet ds = new DataSet();
            AddClientModal r = new AddClientModal();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GetClientListForGuardians", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

                r.clients = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {
                    value = Convert.ToString(spR["clsvId"]),
                    name = (string)spR["nm"]
                }).ToList();

                r.relationships = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new SelectOption()
                {
                    value = Convert.ToString(spR["relationshipId"]),
                    name = (string)spR["relationship"]
                }).ToList();

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
            else
                return PartialView("Modal_AddClient", r);

        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> AddClient(int guardianUId, int clientId, int relationshipId)
        {

            GuardianPageData r = new GuardianPageData();
            r.userLevel = UserClaim.userLevel;
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GuardianAddClient", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@guardianUId", guardianUId);
                        cmd.Parameters.AddWithValue("@clientId", clientId);
                        cmd.Parameters.AddWithValue("@relationshipId", relationshipId);


                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.guardianClients = setGuardianClients(ref ds, 0);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            return PartialView("Guardian_Clients", r);
        }

        [HttpGet]
        [AJAXAuthorize]
        public ActionResult OpenDeleteClientModal(GuardianClient r)
        {
            return PartialView("Modal_DeleteClient", r);

        }

        [HttpPost]
        [AJAXAuthorize]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> DeleteClient(int guardianUId, int clientId)
        {

            GuardianPageData r = new GuardianPageData();
            r.userLevel = UserClaim.userLevel;
            DataSet ds = new DataSet();
            try
            {
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_GuardianDeleteClient", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userId", UserClaim.uid);
                        cmd.Parameters.AddWithValue("@guardianUId", guardianUId);
                        cmd.Parameters.AddWithValue("@clientId", clientId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
                r.guardianClients = setGuardianClients(ref ds, 0);
            }
            catch (Exception ex)
            {
                r.er.code = 1;
                r.er.msg = ex.Message;
            }
            return PartialView("Guardian_Clients", r);
        }

        [HttpGet]
        [AJAXAuthorize]
        public async Task<ActionResult> ToggleGuardianMFA(int guardianUserId)
        {
            Er er = new Er();
            DataSet ds = new DataSet();
            bool mfaEnabled = false;
            try
            {
                await Task.Run(() =>
                {

                    using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                    {
                        SqlCommand cmd = new SqlCommand("sp_SetGuardianUserMFA", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@guardianUserId", guardianUserId);
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                    mfaEnabled = (bool)ds.Tables[0].Rows[0].ItemArray[0];
                });
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }

            ds.Dispose();
            if (er.code != 0)
            {
                Response.Write(er.msg);
                Response.StatusCode = 400;
                return null;
            }
            else return Json(mfaEnabled, JsonRequestBehavior.AllowGet);
        }

        private List<Guardian> setGuardianList(ref DataSet ds, int tableIdx)
        {
            return ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new Guardian()
            {
                id = (int)spR["guardianUId"],
                name = (string)spR["nm"],
                deleted = (bool)spR["deleted"],
                registered = (bool)spR["registered"],
                clientCount = Convert.ToInt32(spR["clientCount"])
            }).ToList();
        }

        private void setGuardianProfile(ref GuardianPageData r, ref DataSet ds, int tableIdx)
        {

            DataRow dr = ds.Tables[tableIdx].Rows[0];
            r.guardianProfile.guardianUId = (int)dr["guardianUId"];
            r.guardianProfile.firstName = dr["firstName"].ToString();
            r.guardianProfile.lastName = dr["lastName"].ToString();
            r.guardianProfile.email = dr["email"].ToString();
            r.guardianProfile.phone = dr["phone"].ToString();
            r.guardianProfile.addressLine1 = dr["addressLine1"].ToString();
            r.guardianProfile.addressLine2 = dr["addressLine2"].ToString();
            r.guardianProfile.city = dr["city"].ToString();
            r.guardianProfile.state = dr["state"].ToString();
            r.guardianProfile.postalCode = dr["postalCode"].ToString();
            r.guardianProfile.deleted = (bool)dr["deleted"];
            r.guardianProfile.registered = (bool)dr["registered"];
            r.guardianProfile.mfaEnabled = (bool)dr["mfaEnabled"];

        }


        private List<GuardianClient> setGuardianClients(ref DataSet ds, int tableIdx)
        {
            return ds.Tables[tableIdx].Rows.Cast<DataRow>().Select(spR => new GuardianClient()
            {
                clientId = (int)spR["clsvId"],
                clientName = (string)spR["ln"] + " " + spR["fn"],
                relationship = (string)spR["relationship"]
            }).ToList();
        }



        private string RenderRazorViewToString(string viewName, object model)
        {
            ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }

        public string createCode(int Len)
        {
            int cnt = 0;
            Random r = new Random();
            string nPass = "";
            while (cnt < Len)
            {
                int j = r.Next(48, 122);
                if ((j >= 48 && j <= 57) || (j >= 65 && j <= 90) || (j >= 97 && j <= 122))
                {
                    nPass = nPass + Convert.ToChar(j);
                    cnt = cnt + 1;
                }
            }
            return nPass;
        }


    }
}