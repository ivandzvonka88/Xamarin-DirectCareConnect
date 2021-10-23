using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Web;
using Newtonsoft.Json;
using DCC.Models;
using DCC.ModelsApi;
using DCC.Models.SessionNotes;
using PdfSharp.Pdf;
using PdfSharp.Drawing.Layout;
using PdfSharp;
using PdfSharp.Drawing;
using TheArtOfDev.HtmlRenderer.PdfSharp;
using DCC.SQLHelpers.Helpers;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;

namespace DCC.ControllersApi
{
    [RoutePrefix("api/Notes")]
    public class NotesController : DDCMobileController
    {

        private readonly SQLHelper sqlHelper;
        public NotesController()
        {
            sqlHelper = new SQLHelper();
        }


        [Authorize]
        public async Task<IHttpActionResult> GetNote(string coId, string docId, string docType)
        {

            string input = "coid=" + @coId + ", docId=" + docId + ", docType=" + docType;
            string output ="";
            setTargetCompanyInfo(coId);
            ClientNote r = new ClientNote();
            r.coId = coId;
            r.docType = docType;
            DataSet ds = new DataSet();
            if (docType == "RSPServiceNote")
            {
                try
                {
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskGetSessionRSPNote", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@staffSessionHcbsId", docId);
                            SqlDataAdapter da = new SqlDataAdapter(cmd);
                            da.Fill(ds);
                        }
                    });

                    DataRow dr = ds.Tables[0].Rows[0];
                    //      r.signee = UserClaim.staffname;
                    //      r.signeeCredentials = UserClaim.stafftitle + (UserClaim.staffnpi != "" ? " (NPI: " + UserClaim.staffnpi + ")" : "");
                    r.clientId = (int)dr["clsvId"];
                    r.serviceId = (int)dr["serviceId"];

                    r.clientName = (string)dr["cnm"];
                    r.svc = (string)dr["svc"];
                    r.note = dr["note"] == DBNull.Value ? "" : (string)dr["note"];
                    r.dt = ((DateTime)dr["dt"]).ToShortDateString();
                    r.providerId = (int)dr["prId"];
                    r.docId = (int)dr["staffSessionHcbsId"];
                    r.teletherapy = (bool)dr["teletherapy"];
                    r.noShow = (bool)dr["noShow"];
                    // XXXX new stuff
                    r.clientRefusedService = (bool)dr["clientRefusedService"];
                    r.designeeUnableToSign = (bool)dr["designeeUnableToSign"];
                    r.designeeRefusedToSign = (bool)dr["designeeRefusedToSign"];
                    r.unsafeToWork = (bool)dr["unsafeToWork"];
                    r.guardianId = (int)dr["guardianId"];
                    r.designeeId = (int)dr["designeeId"];
                    r.designeeLat = dr["designeeLat"] != DBNull.Value ? (decimal)dr["designeeLat"] : 0M;
                    r.designeeLon = dr["designeeLon"] != DBNull.Value ? (decimal)dr["designeeLon"] : 0M;
                    r.designeeLocationId = (int)dr["designeeId"];
                    r.designeeLocationTypeId = (int)dr["designeeLocationTypeId"];
                    // XXXX end new Stuff

                    r.hasAttachment = (bool)dr["hasAttachment"];

                    r.attachmentName = (string)dr["attachmentName"].ToString();
                    r.extension = (string)dr["fileExtension"];
                }
                catch (Exception ex)
                {
                    r.er.code = 1;
                    r.er.msg = ex.Message;
                    saveTransaction(input, output, ex.Message, ex.StackTrace);
                    throw ex;
                }


            }
            else if (docType == "ATCServiceNote")
            {
                try
                {
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskGetSessionAtcNote", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@staffSessionHcbsId", docId);
                            SqlDataAdapter da = new SqlDataAdapter(cmd);
                            da.Fill(ds);
                        }
                    });

                    DataRow dr = ds.Tables[0].Rows[0];

                    //    r.signee = UserClaim.staffname;
                    //     r.signeeCredentials = UserClaim.stafftitle + (UserClaim.staffnpi != "" ? " (NPI: " + UserClaim.staffnpi + ")" : "");

                    r.clientId = (int)dr["clsvId"];
                    r.serviceId = (int)dr["serviceId"];
                    r.clientName = (string)dr["cnm"];
                    r.svc = (string)dr["svc"];
                    r.note = dr["note"] == DBNull.Value ? "" : (string)dr["note"];
                    r.dt = ((DateTime)dr["dt"]).ToShortDateString();

                    r.docId = (int)dr["staffSessionHcbsId"];
                    r.teletherapy = (bool)dr["teletherapy"];
                    r.noShow = (bool)dr["noShow"];

                    // XXXX new stuff
                    r.completed = (bool)dr["completed"];
                    r.clientRefusedService = (bool)dr["clientRefusedService"];
                    r.designeeUnableToSign = (bool)dr["designeeUnableToSign"];
                    r.designeeRefusedToSign = (bool)dr["designeeRefusedToSign"];
                    r.unsafeToWork = (bool)dr["unsafeToWork"];
                    r.guardianId = (int)dr["guardianId"];
                    r.designeeId = (int)dr["designeeId"];
                    r.designeeLat = dr["designeeLat"] != DBNull.Value ? (decimal)dr["designeeLat"] : 0M;
                    r.designeeLon = dr["designeeLon"] != DBNull.Value ? (decimal)dr["designeeLon"] : 0M;
                    r.designeeLocationId = (int)dr["designeeId"];
                    r.designeeLocationTypeId = (int)dr["designeeLocationTypeId"];
                    // XXXX end new Stuff


                    r.hasAttachment = (bool)dr["hasAttachment"];

                    r.attachmentName = (string)dr["attachmentName"].ToString();
                    r.extension = (string)dr["fileExtension"];
                    r.careAreas = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new CareArea()
                    {
                        careId = (int)spR["careId"],
                        careArea = (string)spR["careArea"],
                        score = spR["score"] == DBNull.Value ? "" : (string)spR["score"],
                        lastDate = spR["lastDate"] == DBNull.Value ? "Never" : ((DateTime)spR["lastDate"]).ToShortDateString()

                    }).ToList();

                    r.scoring = ds.Tables[2].Rows.Cast<DataRow>().Select(spR => new Scoring()
                    {
                        value = (string)spR["scoreValue"],
                        name = (string)spR["scoreName"],

                    }).ToList();

                }
                catch (Exception ex)
                {

                    r.er.code = 1;
                    r.er.msg = ex.Message;
                    saveTransaction(input, output, ex.Message, ex.StackTrace);
                    throw ex;
                }

            }
            else if (docType == "HAHServiceNote")
            {
                try
                {
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskGetSessionHabilitationNote", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@staffSessionHcbsId", docId);
                            SqlDataAdapter da = new SqlDataAdapter(cmd);
                            da.Fill(ds);
                        }
                    });

                    DataRow dr = ds.Tables[0].Rows[0];

                    //    r.signee = UserClaim.staffname;
                    //     r.signeeCredentials = UserClaim.stafftitle + (UserClaim.staffnpi != "" ? " (NPI: " + UserClaim.staffnpi + ")" : "");
                    r.clientId = (int)dr["clsvId"];
                    r.serviceId = (int)dr["serviceId"];
                    r.clientName = (string)dr["cnm"];

                    r.providerId = (int)dr["prId"];
                    r.svc = (string)dr["svc"];
                    r.note = dr["note"] == DBNull.Value ? "" : (string)dr["note"];
                    r.dt = ((DateTime)dr["dt"]).ToShortDateString();

                    r.docId = (int)dr["staffSessionHcbsId"];
                    r.noShow = (bool)dr["noShow"];

                    // XXXX new stuff
                    r.completed = (bool)dr["completed"];
                    r.clientRefusedService = (bool)dr["clientRefusedService"];
                    r.designeeUnableToSign = (bool)dr["designeeUnableToSign"];
                    r.designeeRefusedToSign = (bool)dr["designeeRefusedToSign"];
                    r.unsafeToWork = (bool)dr["unsafeToWork"];
                    r.guardianId = (int)dr["guardianId"];
                    r.designeeId = (int)dr["designeeId"];
                    r.designeeLat = dr["designeeLat"] != DBNull.Value ? (decimal)dr["designeeLat"] : 0M;
                    r.designeeLon = dr["designeeLon"] != DBNull.Value ? (decimal)dr["designeeLon"] : 0M;
                    r.designeeLocationId = (int)dr["designeeId"];
                    r.designeeLocationTypeId = (int)dr["designeeLocationTypeId"];
                    // XXXX end new Stuff


                    r.teletherapy = (bool)dr["teletherapy"];
                    r.hasAttachment = (bool)dr["hasAttachment"];

                    r.attachmentName = (string)dr["attachmentName"].ToString();
                    r.extension = (string)dr["fileExtension"];

                    DataView dv = new DataView(ds.Tables[1]);

                    r.longTermObjectives = dv.ToTable(true, "objectiveId", "longTermVision", "longTermGoal").Rows.Cast<DataRow>().Select(spR => new LongTermObjective()
                    {
                        objectiveId = (int)spR["objectiveId"],
                        longTermVision = (string)spR["longTermVision"],
                        longTermGoal = (string)spR["longTermGoal"],
                    }).ToList();

                    foreach (LongTermObjective o in r.longTermObjectives)
                    {
                        dv.RowFilter = "objectiveId=" + o.objectiveId;
                        o.shortTermGoals = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new ShortTermGoal()
                        {
                            goalId = (int)spR["goalId"],
                            shortTermGoal = (string)spR["shortTermGoal"],
                            teachingMethod = (string)spR["teachingMethod"],
                            score = (string)spR["score"],
                            trialPct = spR["trialPct"] == DBNull.Value || (string)spR["trialPct"] == "" ? "0" : (string)spR["trialPct"]
                        }).ToList();
                    }

                    r.scoring = ds.Tables[2].Rows.Cast<DataRow>().Select(spR => new Scoring()
                    {
                        value = (string)spR["scoreValue"],
                        name = (string)spR["scoreName"],

                    }).ToList();
                }
                catch (Exception ex)
                {
                    r.er.code = 1;
                    r.er.msg = ex.Message;
                    saveTransaction(input, output, ex.Message, ex.StackTrace);
                    throw ex;

                }
            }
            else if (docType == "TherapyServiceNote")
            {
                try
                {
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                        {
                            SqlCommand cmd = new SqlCommand("sp_TaskGetSessionTherapyNote", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@clientSessionTherapyId", docId);
                            SqlDataAdapter da = new SqlDataAdapter(cmd);
                            da.Fill(ds);
                        }
                    });
                    DataRow dr = ds.Tables[0].Rows[0];

                    if ((bool)dr["completed"] && !(bool)dr["verified"])
                        r.verification = true;
                    else
                        r.verification = false;

                    //        r.signee = UserClaim.staffname;
                    //        r.signeeCredentials = UserClaim.stafftitle + (UserClaim.staffnpi != "" ? " (NPI: " + UserClaim.staffnpi + ")" : "");
                    r.clientId = (int)dr["clsvId"];
                    r.serviceId = (int)dr["serviceId"];
                    r.clientName = (string)dr["cnm"];
                    r.svc = (string)dr["svc"];
                    r.note = dr["note"] == DBNull.Value ? "" : (string)dr["note"];
                    r.dt = ((DateTime)dr["dt"]).ToShortDateString();

                    r.docId = (int)dr["clientSessionTherapyID"];
                    r.teletherapy = (bool)dr["teletherapy"];
                    r.noShow = (bool)dr["noShow"];

                    // XXXX new stuff
                    r.completed = (bool)dr["completed"];
                    r.clientRefusedService = false;
                    r.designeeUnableToSign = false;
                    r.designeeRefusedToSign = false;
                    r.unsafeToWork = false;
                    r.guardianId = (int)dr["guardianId"];
                    r.designeeId = 0;
                    r.designeeLat = 0;
                    r.designeeLon = 0;
                    r.designeeLocationId = 0;
                    r.designeeLocationTypeId = 0;
                    // XXXX end new Stuff

                    r.hasAttachment = (bool)dr["hasAttachment"];

                    r.attachmentName = (string)dr["attachmentName"].ToString();
                    r.extension = (string)dr["fileExtension"];

                    DataView dv = new DataView(ds.Tables[1]);

                    r.longTermObjectives = dv.ToTable(true, "objectiveId", "longTermVision", "longTermGoal").Rows.Cast<DataRow>().Select(spR => new LongTermObjective()
                    {
                        objectiveId = (int)spR["objectiveId"],
                        longTermVision = (string)spR["longTermVision"],
                        longTermGoal = (string)spR["longTermGoal"],
                    }).ToList();

                    foreach (LongTermObjective o in r.longTermObjectives)
                    {
                        dv.RowFilter = "objectiveId=" + o.objectiveId;
                        o.shortTermGoals = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new ShortTermGoal()
                        {
                            goalId = (int)spR["goalId"],
                            shortTermGoal = (string)spR["shortTermGoal"],
                            teachingMethod = (string)spR["teachingMethod"],
                            frequency = (string)spR["frequency"],
                            score = (string)spR["score"],
                            trialPct = spR["trialPct"] == DBNull.Value || (string)spR["trialPct"] == "" ? "0" : (string)spR["trialPct"]
                        }).ToList();
                    }

                    r.scoring = ds.Tables[2].Rows.Cast<DataRow>().Select(spR => new Scoring()
                    {
                        value = (string)spR["scoreValue"],
                        name = (string)spR["scoreName"],

                    }).ToList();
                }
                catch (Exception ex)
                {
                    r.er.code = 1;
                    r.er.msg = ex.Message;

                    saveTransaction(input, output, ex.Message,ex.StackTrace);

                    throw ex;
                }
            }
            else
            {
                r.er.code = 1;
                r.er.msg = "Unknown Document Type: " + docType;
                saveTransaction(input, output, "Unknown Document Type", docType);
            }

            ds.Dispose();



            if (r.er.code == 0)
            {
              
                output = new JavaScriptSerializer().Serialize(r);
                saveTransaction(input, output, null, null);
                return Json(r);

            }
            else
            {
                output = new JavaScriptSerializer().Serialize(r.er);
                saveTransaction(input, output, null, null);
                return Json(r.er);
            }
        }






        [Authorize]
        public async Task<IHttpActionResult> SetNote()
        {
            Er er = new Er();

            var _sessionNote = HttpContext.Current.Request.Params["_sessionNote"];
            string input = JObject.Parse(_sessionNote).ToString();
            string output = "";
            ClientNote sNote = JsonConvert.DeserializeObject<ClientNote>(_sessionNote);
            var file = HttpContext.Current.Request.Files.Count > 0 ? HttpContext.Current.Request.Files[0] : null;
            setTargetCompanyInfo(sNote.coId);
          
            //   PendingDocumentation p = new PendingDocumentation();

            DataSet ds = null;
            bool hasAttachment = false;
            string fileExtension = "";
            string fileName = null;


            if (sNote.docId== 0)
            {
                // need to look up document
                DataSet docInfo = new DataSet();
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskSessionNoteFind", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@docType", sNote.docType);
                        cmd.Parameters.AddWithValue("@providerId", sNote.providerId);
                        cmd.Parameters.AddWithValue("@clientServiceId", sNote.clientServiceId);
                        cmd.Parameters.AddWithValue("@startUTC", sNote.startUTC);

                        cn.Open();
                        cmd.ExecuteNonQuery();
                        cn.Close();
                    }
                });
                if (ds.Tables[0].Rows.Count != 0)
                {
                    DataRow dr = ds.Tables[0].Rows[0];
                    sNote.docId = (int)dr["docId"];
                }
                else
                {
                    er.code = 1;
                    er.msg = "Could Not Find Document";
                    saveTransaction(input, output, "Could Not Find Document", "");
                }

            }


            if (er.code == 0)
            {
                if (file != null)
                {
                    // Verify that the user selected a file
                    if (file != null && file.ContentLength > 0)
                    {
                        fileExtension = Path.GetExtension(file.FileName);
                        fileName = sNote.attachmentName;
                        byte[] data = new byte[file.InputStream.Length];
                        file.InputStream.Read(data, 0, data.Length);
                        try
                        {
                            FileData f = new FileData("attachments", UserClaim.blobStorage);
                            f.StoreFile(data, fileName + fileExtension);
                            hasAttachment = true;
                        }
                        catch (Exception ex)
                        {
                            er.code = 1;
                            er.msg = ex.Message;
                            saveTransaction(input, output, ex.Message, ex.StackTrace);
                            throw ex;
                        }


                    }

                }

            }


            if (er.code == 0)
            {
                try
                {
                    sNote.hasAttachment = hasAttachment;
                    sNote.extension = fileExtension;
                    if (sNote.docType == "RSPServiceNote")
                    {
                        ds = await setRSPNote(sNote);

                    }
                    else if (sNote.docType == "ATCServiceNote")
                    {
                        ds = await SetAtcNote(sNote);
                    }
                    else if (sNote.docType == "HAHServiceNote")
                    {
                        ds = await SetHahNote(sNote);
                    }
                    else if (sNote.docType == "TherapyServiceNote")
                    {
                        ds = await SetTherapyNote(sNote);
                    }
                    DataView PendingNotes = new DataView(ds.Tables[0]);
                    // List<PendingDocumentation> pendingDocumentation = ds.Tables[0].Rows.Cast<DataRow>().Select(spR => new PendingDocumentation()
                    PendingNotes.RowFilter = "noteType<>'Progress Report' AND noteType<>'Plan Of Care' AND noteType<>'MonitorReport'";
                    List <PendingDocumentation> pendingDocumentation = PendingNotes.ToTable().Rows.Cast<DataRow>().Select(spR => new PendingDocumentation()
                 {

                        docId = (int)spR["docId"],
                        docType = (string)spR["docType"],
                        clientId = (int)spR["clsvId"],
                        clientName = (string)spR["cfn"] + ' ' + (string)spR["cln"],
                        clientServiceId = (int)spR["clientServiceId"],
                        serviceId = (int)spR["serviceId"],
                        completed = (bool)spR["completed"],
                        approved = Convert.ToBoolean(spR["verified"]),
                        dueDt = ((DateTime)spR["dueDt"]).ToShortDateString(),
                        status = (string)spR["status"],
                        noteType = (string)spR["noteType2"],
                        svc = (string)spR["svc"],
                        lostSession = Convert.ToBoolean(spR["lostSession"])
                    }).ToList();
                    PendingNotes.Dispose();
                    output = new JavaScriptSerializer().Serialize(pendingDocumentation);
                    saveTransaction(input, output, null, null);
                    return Json(pendingDocumentation);
                }
                catch (Exception ex)
                {
                    er.code = 1;
                    er.msg = ex.Message;
                    saveTransaction(input, output, ex.Message, ex.StackTrace);
                    throw ex;
                }
              
            }

            return Json(er);

          



           


        }

        private async Task<DataSet> setRSPNote(ClientNote sNote)
        {


            DataSet ds = new DataSet();
            await Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_TaskSetSessionRspNote", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                    cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                    cmd.Parameters.AddWithValue("@prId", sNote.providerId);
                    cmd.Parameters.AddWithValue("@staffSessionHcbsId", sNote.docId);
                    cmd.Parameters.AddWithValue("@noShow", sNote.noShow);

                    // XXXX New
                    cmd.Parameters.AddWithValue("@clientRefusedService", sNote.clientRefusedService);
                    cmd.Parameters.AddWithValue("@designeeRefusedToSign", sNote.designeeRefusedToSign);
                    cmd.Parameters.AddWithValue("@designeeUnableToSign", sNote.designeeUnableToSign);
                    cmd.Parameters.AddWithValue("@unsafeToWork", sNote.unsafeToWork);
                    cmd.Parameters.AddWithValue("@guardianId", sNote.guardianId);
                    cmd.Parameters.AddWithValue("@designeeId", sNote.designeeId);
                    cmd.Parameters.AddWithValue("@designeeLat", sNote.designeeLat);
                    cmd.Parameters.AddWithValue("@designeeLon", sNote.designeeLon);
                    cmd.Parameters.AddWithValue("@designeeLocationId", sNote.designeeLocationId);
                    cmd.Parameters.AddWithValue("@designeeLocationTypeId", sNote.designeeLocationTypeId);
                    // XXX End New

                    cmd.Parameters.AddWithValue("@note", sNote.note);
                    cmd.Parameters.AddWithValue("@hasAttachment", sNote.hasAttachment);
                    cmd.Parameters.AddWithValue("@fileExtension", sNote.extension);
                    cmd.Parameters.AddWithValue("@completed", sNote.completed);
                    cmd.Parameters.AddWithValue("@TimeZone", UserClaim.timeZone);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
            });
            if (sNote.completed)
            {
                string fileName2 = await saveRspNoteAsFile(sNote.docId);
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskSetSessionRspNoteApprove", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@prId", sNote.providerId);
                        cmd.Parameters.AddWithValue("@staffSessionHcbsId", sNote.docId);
                        cmd.Parameters.AddWithValue("@fileName", fileName2);
                        cmd.Parameters.AddWithValue("@completedDt", DateTimeLocal(DateTime.UtcNow).ToShortDateString());
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });

            }
            return ds;
           
        }

        private async Task<string> saveRspNoteAsFile(int docId)
        {
            string fileName = "";
            ClientNotePdf rpr = new ClientNotePdf();
            try
            {
                await Task.Run(() =>
                {
                    getRSPNotePdf(Convert.ToString(docId), ref rpr);
                });
            }
            catch (Exception ex)
            {
                var r = ex.Message;
                throw ex;
            };

            var docStr = docId.ToString();
            fileName = "HCBS_" + ("0000000000000000".Substring(0, 16 - docStr.Length) + docStr) + ".pdf";

            PdfGenerateConfig config = new PdfGenerateConfig();
            config.PageSize = PageSize.Letter;
            config.MarginBottom = 90;
            config.MarginTop = 40;
            config.MarginRight = 30;
            config.MarginLeft = 30;
            config.PageOrientation = PageOrientation.Portrait;
            PdfDocument pdf = null; ;
            try
            {

                var t = RenderRazorViewToString("Document_NoteRsp", rpr);

                 pdf = PdfGenerator.GeneratePdf(RenderRazorViewToString("Document_NoteRsp", rpr), config);
            }
            catch (Exception ex)
            {

                var k = ex.Message;
                throw ex;
            }
                
                
             XFont A8 = new XFont("Arial", 8);
            XRect rect = new XRect(20, 31, 100, 13);
            for (int i = 0; i < pdf.PageCount; i++)
            {
                string footer = rpr.clientName + " (" + rpr.dt + ") Page " + (i + 1) + " of " + pdf.PageCount;
                PdfPage page = pdf.Pages[i];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XTextFormatter tf = new XTextFormatter(gfx);
                rect.X = 200;
                rect.Y = 750;
                rect.Width = 300;
                rect.Height = 10;
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(footer, A8, XBrushes.DarkSlateGray, rect, XStringFormats.TopLeft);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                pdf.Save(ms, false);
                ms.Seek(0, SeekOrigin.Begin);

                FileData f = new FileData("sessionnotes", UserClaim.blobStorage);
                f.StoreFile(ms.ToArray(), fileName);
            }
            return fileName;
        }

        private void getRSPNotePdf(string docId, ref ClientNotePdf r)
        {
            DataSet ds = new DataSet();

            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_TaskGetSessionRspNotePdf", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@StaffSessionHcbsId", docId);
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
            }
            DataRow dr = ds.Tables[0].Rows[0];
            r.completedBy = dr["completedByFn"] + " " + dr["completedByLn"];
            r.completedByCredentials = dr["completedByTitle"] + ((string)dr["completedByNpi"] != "" ? " (NPI: " + dr["completedByNpi"] + ")" : "");

            r.timeOfService = DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString() + " - " + DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString();
            r.agency = UserClaim.companyName;
            r.npi = UserClaim.npi;
            r.clientName = (string)dr["cnm"];
            r.svc = (string)dr["svc"];
            r.serviceName = (string)dr["serviceName"];
            r.dt = ((DateTime)dr["dt"]).ToShortDateString();

            r.note = dr["note"] == DBNull.Value ? "" : (string)dr["note"];
            r.noShow = (bool)dr["noShow"];
            // XXXX New statuses to be used with note
            r.designeeUnableToSign = (bool)dr["designeeUnableToSign"];
            r.designeeRefusedToSign = (bool)dr["designeeRefusedToSign"];
            r.clientRefusedService = (bool)dr["clientRefusedService"];
            r.unsafeToWork = (bool)dr["unsafeToWork"];
            r.guardianId = (int)dr["guardianId"];
            r.designeeId = (int)dr["designeeId"];
            r.IsEVV = (bool)dr["IsEvv"];
            // XXXX end new Stuff
            r.dob = dr["dob"] == DBNull.Value ? "" : ((DateTime)dr["dob"]).ToShortDateString();
            r.clId = dr["clId"] == DBNull.Value ? "" : (string)dr["clId"];
            r.clientWorker = dr["clwNm"] == DBNull.Value ? "" : (string)dr["clwNm"];

            ds.Dispose();
        }

        private async Task<DataSet> SetAtcNote(ClientNote sNote)
        {
           
            DataSet ds = new DataSet();

            DataTable dt = new DataTable();
            dt.Clear();
            dt.Columns.Add("goalId");
            dt.Columns.Add("score");
            if (sNote.careAreas != null)
            {
                for (int i = 0; i < sNote.careAreas.Count; i++)
                {
                    if (sNote.careAreas[i].score != null && sNote.careAreas[i].score != "" && sNote.careAreas[i].score != "NA")
                    {
                        DataRow nRow = dt.NewRow();
                        nRow["goalId"] = sNote.careAreas[i].careId;
                        nRow["score"] = sNote.careAreas[i].score;
                        dt.Rows.Add(nRow);
                    }
                }

            }
           
            await Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_TaskSetSessionAtcNote", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                    cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                    cmd.Parameters.AddWithValue("@prId", sNote.providerId);
                    cmd.Parameters.AddWithValue("@note", sNote.note);
                    cmd.Parameters.AddWithValue("@supervisorPresent", sNote.supervisorPresent);
                    cmd.Parameters.AddWithValue("@noShow", sNote.noShow);

                    // XXXX New
                    cmd.Parameters.AddWithValue("@clientRefusedService", sNote.clientRefusedService);
                    cmd.Parameters.AddWithValue("@designeeRefusedToSign", sNote.designeeRefusedToSign);
                    cmd.Parameters.AddWithValue("@designeeUnableToSign", sNote.designeeUnableToSign);
                    cmd.Parameters.AddWithValue("@unsafeToWork", sNote.unsafeToWork);
                    cmd.Parameters.AddWithValue("@guardianId", sNote.guardianId); 
                    cmd.Parameters.AddWithValue("@designeeId", sNote.designeeId);
                    cmd.Parameters.AddWithValue("@designeeLat", sNote.designeeLat);
                    cmd.Parameters.AddWithValue("@designeeLon", sNote.designeeLon);
                    cmd.Parameters.AddWithValue("@designeeLocationId", sNote.designeeLocationId);
                    cmd.Parameters.AddWithValue("@designeeLocationTypeId", sNote.designeeLocationTypeId);
                    // XXX End New

                    cmd.Parameters.AddWithValue("@staffSessionHcbsId", sNote.docId);
                    cmd.Parameters.AddWithValue("@careAreas", dt);
                    cmd.Parameters.AddWithValue("@hasAttachment", sNote.hasAttachment);
                    cmd.Parameters.AddWithValue("@fileExtension", sNote.extension);
                    cmd.Parameters.AddWithValue("@completed", sNote.completed);
                    cmd.Parameters.AddWithValue("@TimeZone", UserClaim.timeZone);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
            });
            if (sNote.completed)
            {
                string fileName2 = await saveAtcNoteAsFile(sNote.docId);

                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskSetSessionAtcNoteApprove", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@prId", sNote.providerId);
                        cmd.Parameters.AddWithValue("@staffSessionHcbsId", sNote.docId);
                        cmd.Parameters.AddWithValue("@fileName", fileName2);
                        cmd.Parameters.AddWithValue("@completedDt", DateTimeLocal(DateTime.UtcNow).ToShortDateString());
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
            }
            return ds;
        }

        private async Task<string> saveAtcNoteAsFile(int docId)
        {
            string fileName = "";
            ClientNotePdf rpr = new ClientNotePdf();

            await Task.Run(() =>
            {
                getAtcNotePdf(Convert.ToString(docId), ref rpr);
            });

            var docStr = docId.ToString();
            fileName = "HCBS_" + ("0000000000000000".Substring(0, 16 - docStr.Length) + docStr) + ".pdf";

            PdfGenerateConfig config = new PdfGenerateConfig();
            config.PageSize = PageSize.Letter;
            config.MarginBottom = 90;
            config.MarginTop = 40;
            config.MarginRight = 30;
            config.MarginLeft = 30;
            config.PageOrientation = PageOrientation.Portrait;
            PdfDocument pdf = PdfGenerator.GeneratePdf(RenderRazorViewToString("Document_NoteAtc", rpr), config);
            XFont A8 = new XFont("Arial", 8);
            XRect rect = new XRect(20, 31, 100, 13);
            for (int i = 0; i < pdf.PageCount; i++)
            {
                string footer = rpr.clientName + " (" + rpr.dt + ") Page " + (i + 1) + " of " + pdf.PageCount;
                PdfPage page = pdf.Pages[i];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XTextFormatter tf = new XTextFormatter(gfx);
                rect.X = 200;
                rect.Y = 750;
                rect.Width = 300;
                rect.Height = 10;
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(footer, A8, XBrushes.DarkSlateGray, rect, XStringFormats.TopLeft);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                pdf.Save(ms, false);
                ms.Seek(0, SeekOrigin.Begin);

                FileData f = new FileData("sessionnotes", UserClaim.blobStorage);
                f.StoreFile(ms.ToArray(), fileName);
            }

            return fileName;
        }

        private void getAtcNotePdf(string docId, ref ClientNotePdf r)
        {
            DataSet ds = new DataSet();

            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_TaskGetSessionAtcNotePdf", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@staffSessionHcbsId", docId);
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
            }
            DataRow dr = ds.Tables[0].Rows[0];
            r.completedBy = dr["completedByFn"] + " " + dr["completedByLn"];
            r.completedByCredentials = dr["completedByTitle"] + ((string)dr["completedByNpi"] != "" ? " (NPI: " + dr["completedByNpi"] + ")" : "");

            r.timeOfService = DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString() + " - " + DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString();
            r.agency = UserClaim.companyName;
            r.npi = UserClaim.npi;
            r.clientName = (string)dr["cnm"];
            r.svc = (string)dr["svc"];
            r.serviceName = (string)dr["serviceName"];
            r.dt = ((DateTime)dr["dt"]).ToShortDateString();

            r.note = (string)dr["note"];

            r.noShow = (bool)dr["noShow"];
            // XXXX New statuses to be used with note
            r.designeeUnableToSign = (bool)dr["designeeUnableToSign"];
            r.designeeRefusedToSign = (bool)dr["designeeRefusedToSign"];
            r.clientRefusedService = (bool)dr["clientRefusedService"];
            r.unsafeToWork = (bool)dr["unsafeToWork"];
            r.guardianId = (int)dr["guardianId"];
            r.designeeId = (int)dr["designeeId"]; 
            r.IsEVV = (bool)dr["IsEvv"];

            // XXXX end new Stuff

            r.supervisorPresent = (bool)dr["supervisorPresent"];
            r.dob = dr["dob"] == DBNull.Value ? "" : ((DateTime)dr["dob"]).ToShortDateString();
            r.clId = dr["clId"] == DBNull.Value ? "" : (string)dr["clId"];
            r.clientWorker = dr["clwNm"] == DBNull.Value ? "" : (string)dr["clwNm"];


            r.careAreas = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new CareArea()
            {
                careId = (int)spR["careId"],
                careArea = (string)spR["careArea"],
                score = (string)spR["scoreName"]
            }).ToList();



            ds.Dispose();
        }

        private async Task<DataSet> SetHahNote(ClientNote sNote)
        {


            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            dt.Clear();
            dt.Columns.Add("goalId");
            dt.Columns.Add("score");
            dt.Columns.Add("trialPct");
            if (sNote.longTermObjectives != null)
            {
                for (int i = 0; i < sNote.longTermObjectives.Count; i++)
                {
                    for (int j = 0; j < sNote.longTermObjectives[i].shortTermGoals.Count; j++)
                    {
                        if (sNote.longTermObjectives[i].shortTermGoals[j].score != null && sNote.longTermObjectives[i].shortTermGoals[j].score != "" && sNote.longTermObjectives[i].shortTermGoals[j].score != "NA")
                        {
                            DataRow nRow = dt.NewRow();
                            nRow["goalId"] = sNote.longTermObjectives[i].shortTermGoals[j].goalId;
                            nRow["score"] = sNote.longTermObjectives[i].shortTermGoals[j].score;
                            nRow["trialPct"] = sNote.longTermObjectives[i].shortTermGoals[j].trialPct;
                            dt.Rows.Add(nRow);
                        }
                    }
                }
            }

            await Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_TaskSetSessionHabilitationNote", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                    cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                    cmd.Parameters.AddWithValue("@prId", sNote.providerId);
                    cmd.Parameters.AddWithValue("@staffSessionHcbsId", sNote.docId);
                    cmd.Parameters.AddWithValue("@goalScores", dt);
                    cmd.Parameters.AddWithValue("@note", sNote.note);
                    cmd.Parameters.AddWithValue("@teletherapy", sNote.teletherapy);
                    cmd.Parameters.AddWithValue("@noShow", sNote.noShow);

                    // XXXX New
                    cmd.Parameters.AddWithValue("@clientRefusedService", sNote.clientRefusedService);
                    cmd.Parameters.AddWithValue("@designeeRefusedToSign", sNote.designeeRefusedToSign);
                    cmd.Parameters.AddWithValue("@designeeUnableToSign", sNote.designeeUnableToSign);
                    cmd.Parameters.AddWithValue("@unsafeToWork", sNote.unsafeToWork);
                    cmd.Parameters.AddWithValue("@guardianId", sNote.guardianId); 
                    cmd.Parameters.AddWithValue("@designeeId", sNote.designeeId);
                    cmd.Parameters.AddWithValue("@designeeLat", sNote.designeeLat);
                    cmd.Parameters.AddWithValue("@designeeLon", sNote.designeeLon);
                    cmd.Parameters.AddWithValue("@designeeLocationId", sNote.designeeLocationId);
                    cmd.Parameters.AddWithValue("@designeeLocationTypeId", sNote.designeeLocationTypeId);
                    // XXX End New

                    cmd.Parameters.AddWithValue("@hasAttachment", sNote.hasAttachment);
                    cmd.Parameters.AddWithValue("@fileExtension", sNote.extension);
                    cmd.Parameters.AddWithValue("@completed", sNote.completed);
                    cmd.Parameters.AddWithValue("@TimeZone", UserClaim.timeZone);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
            });
            if (sNote.completed)
            {
                string fileName2 = await saveHahNoteAsFile(sNote.docId);
                await Task.Run(() =>
                {
                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_TaskSetSessionHabilitationNoteApprove", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                        cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                        cmd.Parameters.AddWithValue("@prId", sNote.providerId);
                        cmd.Parameters.AddWithValue("@staffSessionHcbsId", sNote.docId);
                        cmd.Parameters.AddWithValue("@fileName", fileName2);
                        cmd.Parameters.AddWithValue("@completedDt", DateTimeLocal(DateTime.UtcNow).ToShortDateString());
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                });
            }
            return ds;
        }

        private async Task<string> saveHahNoteAsFile(int docId)
        {
            string fileName = "";
            ClientNotePdf rpr = new ClientNotePdf();

            await Task.Run(() =>
            {
                getHahNotePdf(Convert.ToString(docId), ref rpr);
            });

            var docStr = docId.ToString();
            fileName = "HCBS_" + ("0000000000000000".Substring(0, 16 - docStr.Length) + docStr) + ".pdf";

            PdfGenerateConfig config = new PdfGenerateConfig();
            config.PageSize = PageSize.Letter;
            config.MarginBottom = 90;
            config.MarginTop = 40;
            config.MarginRight = 30;
            config.MarginLeft = 30;
            config.PageOrientation = PageOrientation.Portrait;
            PdfDocument pdf = PdfGenerator.GeneratePdf(RenderRazorViewToString("Document_NoteHabilitation", rpr), config);
            XFont A8 = new XFont("Arial", 8);
            XRect rect = new XRect(20, 31, 100, 13);
            for (int i = 0; i < pdf.PageCount; i++)
            {
                string footer = rpr.clientName + " (" + rpr.dt + ") Page " + (i + 1) + " of " + pdf.PageCount;
                PdfPage page = pdf.Pages[i];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XTextFormatter tf = new XTextFormatter(gfx);
                rect.X = 200;
                rect.Y = 750;
                rect.Width = 300;
                rect.Height = 10;
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(footer, A8, XBrushes.DarkSlateGray, rect, XStringFormats.TopLeft);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                pdf.Save(ms, false);
                ms.Seek(0, SeekOrigin.Begin);

                FileData f = new FileData("sessionnotes", UserClaim.blobStorage);
                f.StoreFile(ms.ToArray(), fileName);
            }
            return fileName;
        }

        private void getHahNotePdf(string docId, ref ClientNotePdf r)
        {
            DataSet ds = new DataSet();

            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_TaskGetSessionHabilitationNotePdf", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@StaffSessionHcbsId", docId);
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
            }
            DataRow dr = ds.Tables[0].Rows[0];
            r.completedBy = dr["completedByFn"] + " " + dr["completedByLn"];
            r.completedByCredentials = dr["completedByTitle"] + ((string)dr["completedByNpi"] != "" ? " (NPI: " + dr["completedByNpi"] + ")" : "");

            r.timeOfService = DateTimeLocal((DateTime)dr["utcIn"]).ToShortTimeString() + " - " + DateTimeLocal((DateTime)dr["utcOut"]).ToShortTimeString();
            r.agency = UserClaim.companyName;
            r.npi = UserClaim.npi;
            r.clientName = (string)dr["cnm"];
            r.svc = (string)dr["svc"];
            r.serviceName = (string)dr["serviceName"];
            r.dt = ((DateTime)dr["dt"]).ToShortDateString();

            r.note = dr["note"] == DBNull.Value ? "" : (string)dr["note"];
            r.noShow = (bool)dr["noShow"];
            // XXXX New statuses to be used with note
            r.designeeUnableToSign = (bool)dr["designeeUnableToSign"];
            r.designeeRefusedToSign = (bool)dr["designeeRefusedToSign"];
            r.clientRefusedService = (bool)dr["clientRefusedService"];
            r.unsafeToWork = (bool)dr["unsafeToWork"];
            r.guardianId = (int)dr["guardianId"];
            r.designeeId = (int)dr["designeeId"];
            r.IsEVV = (bool)dr["IsEvv"];

            // XXXX end new Stuff
            r.dob = dr["dob"] == DBNull.Value ? "" : ((DateTime)dr["dob"]).ToShortDateString();
            r.clId = dr["clId"] == DBNull.Value ? "" : (string)dr["clId"];
            r.clientWorker = dr["clwNm"] == DBNull.Value ? "" : (string)dr["clwNm"];
            DataView dv = new DataView(ds.Tables[1]);

            r.longTermObjectives = dv.ToTable(true, "objectiveId", "longTermVision", "longTermGoal").Rows.Cast<DataRow>().Select(spR => new LongTermObjective()
            {
                objectiveId = (int)spR["objectiveId"],
                longTermVision = (string)spR["longTermVision"],
                longTermGoal = (string)spR["longTermGoal"],
            }).ToList();

            foreach (LongTermObjective o in r.longTermObjectives)
            {
                dv.RowFilter = "objectiveId=" + o.objectiveId;
                o.shortTermGoals = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new ShortTermGoal()
                {
                    goalId = (int)spR["goalId"],
                    shortTermGoal = (string)spR["shortTermGoal"],
                    teachingMethod = (string)spR["teachingMethod"],
                    score = (string)spR["score"],
                    trialPct = spR["trialPct"] == DBNull.Value || (string)spR["trialPct"] == "" ? "0" : (string)spR["trialPct"]

                }).ToList();
            }

            ds.Dispose();
        }
        private async Task<DataSet> SetTherapyNote(ClientNote sNote)
        {
    
            bool autoVerify = false;
            if (UserClaim.userLevel == "TherapySupervisor" /*|| UserClaim.dcwRole == "TherapySupervisor" */)
                autoVerify = true;

            DataSet ds = new DataSet();
            bool hasAttachment = false;
            string fileExtension = "";


            DataTable dt = new DataTable();
            dt.Clear();
            dt.Columns.Add("goalId");
            dt.Columns.Add("score");
            dt.Columns.Add("trialPct");
            if (sNote.longTermObjectives != null)
            {

                for (int i = 0; i < sNote.longTermObjectives.Count; i++)
                {
                    if (sNote.longTermObjectives[i].shortTermGoals != null)
                    {
                        for (int j = 0; j < sNote.longTermObjectives[i].shortTermGoals.Count; j++)
                        {
                            if (sNote.longTermObjectives[i].shortTermGoals[j].score != null && sNote.longTermObjectives[i].shortTermGoals[j].score != "" )
                            {
                                DataRow nRow = dt.NewRow();
                                nRow["goalId"] = sNote.longTermObjectives[i].shortTermGoals[j].goalId;
                                nRow["score"] = sNote.longTermObjectives[i].shortTermGoals[j].score;
                                nRow["trialPct"] = sNote.longTermObjectives[i].shortTermGoals[j].trialPct;
                                dt.Rows.Add(nRow);
                            }
                        }
                    }
                }
            }

            await Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_TaskSetSessionTherapyNote", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                    cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                    cmd.Parameters.AddWithValue("@prId", sNote.providerId);
                    cmd.Parameters.AddWithValue("@clientSessionTherapyId", sNote.docId);
                    cmd.Parameters.AddWithValue("@goalScores", dt);
                    cmd.Parameters.AddWithValue("@note", sNote.note);
                    cmd.Parameters.AddWithValue("@noShow", sNote.noShow);
                    cmd.Parameters.AddWithValue("@hasAttachment", hasAttachment);
                    cmd.Parameters.AddWithValue("@fileExtension", fileExtension);
                    cmd.Parameters.AddWithValue("@completedDt", DateTimeLocal(DateTime.UtcNow).ToShortDateString());
                    cmd.Parameters.AddWithValue("@getPendingDocumentation", autoVerify ? false : true);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
            });

            if (autoVerify)
            {
                string fileName2 = await saveTherapyNoteAsFile(sNote.docId);

                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_TaskSetSessionTherapyNoteApprove", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                    cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                    cmd.Parameters.AddWithValue("@clientSessionTherapyId", sNote.docId);
                    cmd.Parameters.AddWithValue("@prId", sNote.providerId);
                    cmd.Parameters.AddWithValue("@rejected", 0);
                    cmd.Parameters.AddWithValue("@rejectedreason", "");
                    cmd.Parameters.AddWithValue("@filename", fileName2);
                    cmd.Parameters.AddWithValue("@verifiedDt", DateTimeLocal(DateTime.UtcNow).ToShortDateString());
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(ds);
                }


            }





            return ds;
        }

        private async Task<string> saveTherapyNoteAsFile(int docId)
        {
            string fileName = "";
            ClientNotePdf rpr = new ClientNotePdf();

            await Task.Run(() =>
            {
                getTherapyNotePdf(Convert.ToString(docId), ref rpr);
            });

            var docStr = docId.ToString();
            fileName = "Therapy_" + ("0000000000000000".Substring(0, 16 - docStr.Length) + docStr) + ".pdf";

            PdfGenerateConfig config = new PdfGenerateConfig();
            config.PageSize = PageSize.Letter;
            config.MarginBottom = 90;
            config.MarginTop = 40;
            config.MarginRight = 30;
            config.MarginLeft = 30;
            config.PageOrientation = PageOrientation.Portrait;
            PdfDocument pdf = PdfGenerator.GeneratePdf(RenderRazorViewToString("Document_NoteTherapy", rpr), config);
            XFont A8 = new XFont("Arial", 8);
            XRect rect = new XRect(20, 31, 100, 13);
            for (int i = 0; i < pdf.PageCount; i++)
            {
                string footer = rpr.clientName + " (" + rpr.dt + ") Page " + (i + 1) + " of " + pdf.PageCount;
                PdfPage page = pdf.Pages[i];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XTextFormatter tf = new XTextFormatter(gfx);
                rect.X = 200;
                rect.Y = 750;
                rect.Width = 300;
                rect.Height = 10;
                gfx.DrawRectangle(XBrushes.Transparent, rect);
                tf.DrawString(footer, A8, XBrushes.DarkSlateGray, rect, XStringFormats.TopLeft);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                pdf.Save(ms, false);
                ms.Seek(0, SeekOrigin.Begin);

                FileData f = new FileData("sessionnotes", UserClaim.blobStorage);
                f.StoreFile(ms.ToArray(), fileName);
            }

            return fileName;
        }

        private void getTherapyNotePdf(string docId, ref ClientNotePdf r)
        {
            DataSet ds = new DataSet();

            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_TaskGetSessionTherapyNotePdf", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@userLevel", UserClaim.userLevel);
                cmd.Parameters.AddWithValue("@userprId", UserClaim.prid);
                cmd.Parameters.AddWithValue("@ClientSessionTherapyId", docId);
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
            }
            DataRow dr = ds.Tables[0].Rows[0];
            r.completedBy = dr["completedByFn"] + " " + dr["completedByLn"];
            r.completedByCredentials = dr["completedByTitle"] + ((string)dr["completedByNpi"] != "" ? " (NPI: " + dr["completedByNpi"] + ")" : "");
            r.approvedBy = dr["approvedByFn"] + " " + dr["approvedByLn"];
            r.approvedByCredentials = dr["approvedByTitle"] + ((string)dr["approvedByNpi"] != "" ? " (NPI: " + dr["approvedByNpi"] + ")" : "");

            r.timeOfService = DateTimeLocal((DateTime)dr["startAt"]).ToShortTimeString() + " - " + DateTimeLocal((DateTime)dr["endAt"]).ToShortTimeString();
            r.agency = UserClaim.companyName;
            r.npi = UserClaim.npi;
            r.clientName = (string)dr["cnm"];
            r.svc = (string)dr["svc"];
            r.serviceName = (string)dr["serviceName"];
            r.dt = ((DateTime)dr["dt"]).ToShortDateString();

            r.note = (string)dr["note"];
            r.noShow = (bool)dr["noShow"];
            // XXXX New statuses to be used with note
            r.designeeUnableToSign = (bool)dr["designeeUnableToSign"];
            r.designeeRefusedToSign = (bool)dr["designeeRefusedToSign"];
            r.clientRefusedService = (bool)dr["clientRefusedService"];
            r.unsafeToWork = (bool)dr["unsafeToWork"];
            r.guardianId = (int)dr["guardianId"];
            r.designeeId = (int)dr["designeeId"];
            r.IsEVV = (bool)dr["IsEvv"];
            // XXXX end new Stuff
            r.dob = dr["dob"] == DBNull.Value ? "" : ((DateTime)dr["dob"]).ToShortDateString();
            r.clId = dr["clId"] == DBNull.Value ? "" : (string)dr["clId"];
            r.clientWorker = dr["clwNm"] == DBNull.Value ? "" : (string)dr["clwNm"];
            DataView dv = new DataView(ds.Tables[1]);

            r.longTermObjectives = dv.ToTable(true, "objectiveId", "longTermVision", "longTermGoal").Rows.Cast<DataRow>().Select(spR => new LongTermObjective()
            {
                objectiveId = (int)spR["objectiveId"],
                longTermVision = (string)spR["longTermVision"],
                longTermGoal = (string)spR["longTermGoal"],
            }).ToList();

            foreach (LongTermObjective o in r.longTermObjectives)
            {
                dv.RowFilter = "objectiveId=" + o.objectiveId;
                o.shortTermGoals = dv.ToTable().Rows.Cast<DataRow>().Select(spR => new ShortTermGoal()
                {
                    goalId = (int)spR["goalId"],
                    shortTermGoal = (string)spR["shortTermGoal"],
                    teachingMethod = (string)spR["teachingMethod"],
                    score = (string)spR["score"],
                    trialPct = (string)spR["trialPct"]

                }).ToList();
            }

            ds.Dispose();
        }

    }
}
