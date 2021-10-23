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
using DCC.ModelsApi;
using System.Collections.Specialized;
using DCC.SQLHelpers.Helpers;

namespace DCC.ControllersApi
{
    [RoutePrefix("api/Credentials")]
    public class CredentialsController : DDCMobileController
    {
        private readonly SQLHelper sqlHelper;
        public CredentialsController()
        {
            sqlHelper = new SQLHelper();
        }



        [Authorize]
        public async Task<IHttpActionResult> UpdateCredential()
        {
            CredentialsUpdateResponse r = new CredentialsUpdateResponse();
            var file = HttpContext.Current.Request.Files.Count > 0 ?HttpContext.Current.Request.Files[0] : null;

            var prId = HttpContext.Current.Request.Params["providerId"];
            var coId = HttpContext.Current.Request.Params["coId"];
            var credId = HttpContext.Current.Request.Params["credId"];
            var credTypeId = HttpContext.Current.Request.Form["credTypeId"];
            var validFrom = HttpContext.Current.Request.Form["validFrom"];
            var validTo = HttpContext.Current.Request.Form["validTo"];
            var docId = HttpContext.Current.Request.Form["docId"];
            setTargetCompanyInfo(coId);



            if (credId != null && credTypeId != null && validFrom != null && validTo != null & docId != null)
            {
                string fileName;
                int newCredId = 0;
                string fileExtension = null;
                string contentType = null;

                DataSet ds = null;
                try
                {
                    ds = await addStaffCredential( prId, credId, credTypeId, docId, validFrom, validTo);
                }
                catch (Exception ex)
                {
                    r.er.code = 1;
                    r.er.msg = ex.Message;
                }
                if (r.er.code == 0)
                {
                    if (ds.Tables.Count != 0 && ds.Tables[0].Rows.Count != 0)
                    {
                        newCredId = Convert.ToInt32(ds.Tables[0].Rows[0].ItemArray[0]);
                        fileExtension = Convert.ToString(ds.Tables[0].Rows[0].ItemArray[1]);
                        contentType = Convert.ToString(ds.Tables[0].Rows[0].ItemArray[2]);
                    }
                }
                if (ds != null)
                    ds.Dispose();

                if (r.er.code == 0)
                {
                    if (file != null)
                    {
                        // Verify that the user selected a file
                        if (file != null && file.ContentLength > 0)
                        {
                            fileExtension = Path.GetExtension(file.FileName);
                            fileName = newCredId + fileExtension;
                            contentType = file.ContentType;
                            byte[] data = new byte[file.InputStream.Length];
                            file.InputStream.Read(data, 0, data.Length);
                            try
                            {
                                FileData f = new FileData("credentials", UserClaim.blobStorage);
                                f.StoreFile(data, fileName);
                            }
                            catch (Exception ex)
                            {
                                r.er.code = 1;
                                r.er.msg = ex.Message;
                            }

                        }
                    }
                    if (r.er.code == 0)
                    {
                        if (newCredId != 0 || fileExtension != null)
                        {
                            DataSet ds2 = null;
                            try
                            {
                               ds2 = await updateStaffCredential(prId, newCredId, fileExtension, contentType);
                            }
                            catch (Exception ex)
                            {
                                r.er.code = 1;
                                r.er.msg = ex.Message;
                            }
                            if (r.er.code == 0)
                            {
                                try
                                {
                                    r.credentials = ds2.Tables[0].Rows.Cast<DataRow>().Select(spR => new Credential()
                                    {
                                        // credentials
                                        coId = coId,
                                        providerId = Convert.ToInt32(prId),
                                        credId = spR["credId"] == DBNull.Value || (bool)spR["verified"] ? 0 : (int)spR["credId"],
                                        credTypeId = (int)spR["credTypeId"],
                                        credName = (string)spR["credName"],
                                        validFrom = spR["validFrom"] == DBNull.Value ? "" : ((DateTime)spR["validFrom"]).ToShortDateString(),
                                        validTo = spR["validTo"] == DBNull.Value ? "" : ((DateTime)spR["validTo"]).ToShortDateString(),
                                        docId = spR["docId"] == DBNull.Value ? "" : (string)spR["docId"],
                                        status = (string)spR["status"]
                                    }).ToList();
                                }
                                catch (Exception ex)
                                {
                                    r.er.code = 1;
                                    r.er.msg = ex.Message;
                                }
                            }
                            if (ds2 != null)
                                ds2.Dispose();
                        }
                    }
                }



            }

           

            return Json(r);
        }



     


        private Task<DataSet> addStaffCredential(string providerId, string credId, string credTypeId, string docId, string validFrom, string validTo)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ApiAddCredential", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    
                    cmd.Parameters.AddWithValue("@prId", providerId);
                    cmd.Parameters.AddWithValue("@credId", credId);
                    cmd.Parameters.AddWithValue("@credTypeId", credTypeId);
                    cmd.Parameters.AddWithValue("@docId", docId);
                    cmd.Parameters.AddWithValue("@validFrom", validFrom);
                    cmd.Parameters.AddWithValue("@validTo", validTo);
;
                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
          
        }
    
        private Task<DataSet> updateStaffCredential(string providerId, int credId, string fileExtension, string contentType)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ApiUpdateCredential", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                   
                    cmd.Parameters.AddWithValue("@prId", providerId);
                    cmd.Parameters.AddWithValue("@credId", credId);
                    cmd.Parameters.AddWithValue("@fileExtension", fileExtension);
                    cmd.Parameters.AddWithValue("@contentType", contentType);
                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    return ds;
                }
            });
        }


 





    }
}