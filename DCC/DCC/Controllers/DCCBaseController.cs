using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using DCC.Models;

using DCC.SQLHelpers.Helpers;


namespace DCC.Controllers
{
    public class DCCBaseController : Controller
    {
        protected UserClaims UserClaim = new UserClaims();
        private readonly SQLHelper sqlHelper;
        public DCCBaseController()
        {
            sqlHelper = new SQLHelper();
        }


        [ChildActionOnly]
        public ActionResult UserName()
        {
            return new ContentResult { Content = "Mark ZuckerBerg" };
        }


        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            base.Initialize(requestContext);
        
            if (requestContext.HttpContext.User.Identity.IsAuthenticated)
            {
                getUserClaims((ClaimsIdentity)User.Identity);
            }
        }
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            if (Request.Headers["__CompanyId"] != null && Request.Headers["__CompanyId"] != Convert.ToString(UserClaim.coid))
            {
                //   string oldCompany = getOldCompany(Request.Headers["__CompanyId"]);
                Response.Write("You are currently logged into " + UserClaim.companyName);
                filterContext.Result = new HttpStatusCodeResult(403, "Forbidden");

            }

        }

        protected void setViewModelBase(ViewModelBase Vmb)
        {

            Vmb.userLevel = UserClaim.userLevel;
            Vmb.staffname = UserClaim.staffname;
            Vmb.userPrid = UserClaim.prid;
            Vmb.companies = UserClaim.companies;
            Vmb.companyName = UserClaim.companyName;
            Vmb.CompanyID = UserClaim.coid;
            Vmb.dcwRole = UserClaim.dcwRole;
            Vmb.sendBirdUserId = UserClaim.sendBirdUserId;

        }



        protected DateTime DateTimeLocal()
        {
            TimeZoneInfo TimeZone;
            DateTime LocalTime;
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById(UserClaim.timeZone);
            LocalTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone);
            return LocalTime;
        }

        protected DateTime DateTimeLocal(DateTime dt)
        {
            TimeZoneInfo TimeZone;
            DateTime LocalTime;
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById(UserClaim.timeZone);
            LocalTime = TimeZoneInfo.ConvertTimeFromUtc(dt, TimeZone);
            return LocalTime;
        }

        protected DateTime ConvertToUTC(DateTime dt)
        { 
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(UserClaim.timeZone);
            DateTime dateTimeUnspec = DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(dateTimeUnspec, timeZone);
        }


        private async Task getUserClaims(ClaimsIdentity identity)
        {
            IEnumerable<Claim> claims = identity.Claims;
            foreach (var item in claims)
            {
                if (((Claim)item).Type == "userLevel")
                    UserClaim.userLevel = ((Claim)item).Value;
                else if (((Claim)item).Type == ClaimTypes.Name)
                    UserClaim.staffname = ((Claim)item).Value;
                else if (((Claim)item).Type == "dcwRole")
                    UserClaim.dcwRole = ((Claim)item).Value;
                else if (((Claim)item).Type == "userGuid")
                    UserClaim.userGuid = ((Claim)item).Value;
                else if (((Claim)item).Type == "userName")
                    UserClaim.userName = ((Claim)item).Value;
                else if (((Claim)item).Type == "uid")
                    UserClaim.uid = Convert.ToInt32(((Claim)item).Value);
                else if (((Claim)item).Type == "prid")
                    UserClaim.prid = Convert.ToInt32(((Claim)item).Value);
                else if (((Claim)item).Type == "pCode")
                    UserClaim.pcode = ((Claim)item).Value;
                else if (((Claim)item).Type == "coid")
                    UserClaim.coid = Convert.ToInt32(((Claim)item).Value);
                else if (((Claim)item).Type == "npi")
                    UserClaim.npi = ((Claim)item).Value;
                else if (((Claim)item).Type == "staffNPI")
                    UserClaim.staffnpi = ((Claim)item).Value;
                else if (((Claim)item).Type == "staffTitle")
                    UserClaim.stafftitle = ((Claim)item).Value;
                else if (((Claim)item).Type == "conStr")
                    UserClaim.conStr = ((Claim)item).Value;
                else if (((Claim)item).Type == "companyName")
                    UserClaim.companyName = ((Claim)item).Value;
                else if (((Claim)item).Type == "state")
                    UserClaim.state = ((Claim)item).Value;
                else if (((Claim)item).Type == "timeZone")
                    UserClaim.timeZone = ((Claim)item).Value;
                else if (((Claim)item).Type == "blobStorage")
                    UserClaim.blobStorage = ((Claim)item).Value;
                else if (((Claim)item).Type == "supervisoryLevel")
                    UserClaim.supervisoryLevel = Convert.ToInt32(((Claim)item).Value);
                else if (((Claim)item).Type == "sendBirdUserId")
                    UserClaim.sendBirdUserId = ((Claim)item).Value;



                else if (((Claim)item).Type == "companies")
                {
                    string[] s = ((Claim)item).Value.Split('|');
                    UserClaim.companies.Add(new Company { name = s[1], coid = s[0] });
                }
               
                  
            }

            DataSet Company = new DataSet();
            await Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("sp_GetCompanyInfoById", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@coId", UserClaim.coid);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, Company);
                }
            });
            DataRow CompanyRow = Company.Tables[0].Rows[0];
            UserClaim.iSolvedCompanyId = (int)CompanyRow["iSolvedCompanyId"];

            /*
             UserClaim.userLevel = "Supervisor";
             UserClaim.prid = 1040;
             UserClaim.uid = 1639;
            */
        }

        protected string ConvertToHtml(string text)
        {
            return text.Replace("&", "&amp;").Replace("  ", "&nbsp; ").Replace("\r\n", "<br />").Replace("\r", "<br />").Replace("\n", "<br />").Replace("\t", "&nbsp;&nbsp; &nbsp;&nbsp;").Replace("'", "&apos;").Replace("\"", "&quot;");
        }




        protected Task<DataSet> SqlGetDataMainDB(string cmdText)
        {
            return Task.Run(() =>
            {


                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(cmdText))
                    {
                        cmd.Connection = cn;
                        DataSet ds = new DataSet();
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        return ds;
                    }
                }
            });
        }

        protected Task<DataSet> SqlGetData(string cmdText)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    using (SqlCommand cmd = new SqlCommand(cmdText))
                    {
                        cmd.Connection = cn;
                        DataSet ds = new DataSet();
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        return ds;
                    }
                }
            });
        }

        protected Task<bool> SqlExecuteCmd(string cmdText)
        {
            return Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    using (SqlCommand cmd = new SqlCommand(cmdText))
                    {
                        cmd.Connection = cn;
                        cn.Open();
                        cmd.ExecuteNonQuery();
                        cn.Close();
                        return true;
                    }
                }
            });
        }

        protected object SqlExecuteScalar(string cmdText)
        {
            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                using (SqlCommand cmd = new SqlCommand(cmdText))
                {
                    cmd.Connection = cn;
                    cn.Open();
                    var result = cmd.ExecuteScalar();
                    cn.Close();
                    return result;
                }
            }
        }

        protected class FileData
        {

            CloudBlobContainer _credentialBlobContainer;
            CloudBlobClient _blobClient;
            public FileData(string blobStorage)
            {
                var storageAccount = CloudStorageAccount.Parse(blobStorage);
                _blobClient = storageAccount.CreateCloudBlobClient();
            }
            public FileData(string _container, string blobStorage)
            {
                var storageAccount = CloudStorageAccount.Parse(blobStorage);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                _credentialBlobContainer = blobClient.GetContainerReference(_container);
                _credentialBlobContainer.CreateIfNotExists();
 
            }

            public void StoreFile(byte[] data, string fileName)
            {
                var blob = _credentialBlobContainer.GetBlockBlobReference(fileName);
                blob.DeleteIfExists();
                blob.UploadFromByteArray(data, 0, data.Length);
            }

            public void DeleteFile(string fileName)
            {
                if (!string.IsNullOrEmpty(fileName))
                {
                    var blob = _credentialBlobContainer.GetBlockBlobReference(fileName);
                    blob.DeleteIfExists();
                }
            }


            public UnSkilledBillingFileList GetBillingFileList(string fileName = null)
            {
                UnSkilledBillingFileList r = new UnSkilledBillingFileList();
                var list = _credentialBlobContainer.ListBlobs(fileName, true);

                List<CloudBlockBlob> blobNames = list.OfType<CloudBlockBlob>().ToList();
                if (blobNames.Count == 2)
                {
                    
                    foreach ( var blob in blobNames)
                    {
                        if (blob.Name.IndexOf(".pdf") != -1)
                        {
                            r.coverDocument.fileName = blob.Name;
                            if (blob.Properties.LastModified != null)
                                r.coverDocument.lastModifiedUtc = ((DateTimeOffset)blob.Properties.LastModified).UtcDateTime;
                        }
                        else
                        {
                            r.billingFile.fileName = blob.Name;
                            if (blob.Properties.LastModified != null)
                                r.billingFile.lastModifiedUtc = ((DateTimeOffset)blob.Properties.LastModified).UtcDateTime;
                        }
                    }
                }
                return r;
            }

           

            public byte[] GetFile(string fileName)
            {
                var blob = _credentialBlobContainer.GetBlockBlobReference(fileName);
                using (var ms = new MemoryStream())
                {
                    blob.DownloadToStream(ms);
                    return ms.ToArray();
                }
            }

            public bool CreateContainer(string containerName)
            {
                CloudBlobContainer container = _blobClient.GetContainerReference(containerName);

                try
                {
                    // Create the container if it does not already exist.
                    bool result = container.CreateIfNotExists();
                    return result;
                }
                catch { return false; }
            }
        }
    }




}