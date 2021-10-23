using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.IO;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using DCC.Models;
using CsvHelper;
using DCC.SQLHelpers.Helpers;

namespace DCC.Controllers
{
    public class OIGCheckController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;

        public OIGCheckController()
        {
            sqlHelper = new SQLHelper();
        }


        [Authorize]
        public ActionResult Index()
        {

            OIGPageData r = getOIGData();

            setViewModelBase((ViewModelBase)r);

            return View("Index", r);
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public ActionResult GetFileAndRerunDataCheck()
        {

            Er er = new Er();
            OIG OIG = new OIG();

                OIG.getOIGFile(ref er);
          
            OIG.Dispose();
            OIGPageData r;
 
                r = getOIGData();
           


            return PartialView("OIGResults", r);
        }


        private OIGPageData getOIGData()
        {

            DataSet ds = new DataSet();
            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_checkOIGListAllStaff", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
         
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
            }
            OIGPageData r = new OIGPageData();
            r.OIGFileDate = ((DateTime)ds.Tables[0].Rows[0].ItemArray[0]).ToShortDateString();
            r.OIGMatches = ds.Tables[1].Rows.Cast<DataRow>().Select(spR => new OIGMatch()
            {
                firstName = (string)spR["fn"],
                lastName = (string)spR["ln"],
                address = (string)spR["ad"],
                city = (string)spR["city"],
                state = (string)spR["state"],
                zip = (string)spR["z"],
                dob = spR["dob"] == DBNull.Value ? "" : ((DateTime)spR["dob"]).ToShortDateString()
            }).ToList();


            ds.Dispose();
            return r;
        }



    }

    public class OIG : WebClient
    {
        public void getOIGFile(ref Er er)
        {
            er.code = 0;
            er.msg = "";

            string responseData = "";
            HttpWebResponse response = null;
            StreamReader responseReader;
            CookieContainer container;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            // get login page info
            var request = (HttpWebRequest)WebRequest.Create("https://oig.hhs.gov/exclusions/exclusions_list.asp");
            try
            {
                container = request.CookieContainer = new CookieContainer();
                request.Method = "GET";
                request.ContentType = "application/x-www-form-urlencoded";
                request.AllowAutoRedirect = true;
                response = (HttpWebResponse)request.GetResponse();
                responseReader = new StreamReader(response.GetResponseStream());
                responseData = responseReader.ReadToEnd();
                responseReader.Close();
            }
            catch (WebException ex)
            {
                er.code = 1;
                er.msg = "OIG - Get Page Failed " + ex.Message;
            }

            catch (Exception ex)
            {
                er.code = 1;
                er.msg = "OIG - Get Page Failed " + ex.Message;
            }
            if (er.code == 0)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    er.code = 1;
                    er.msg = "OIG Get Page Failed  " + response.StatusCode + " " + response.StatusDescription;
                }
                else
                {
                    //* Find date of file   
                    int sIndex = responseData.IndexOf("alert-heading\">");
                    if (sIndex != -1)
                    {
                        int eIndex = responseData.Substring(sIndex + "alert-heading\">".Length).IndexOf("</h3>");
                        if (eIndex != -1)
                        {
                            string dtStr = responseData.Substring(sIndex + "alert-heading\">".Length, eIndex).Trim();
                            string[] sP = dtStr.Split('-');
                            int m;
                            int d;
                            int y;
                            if (sP.Length == 3 && int.TryParse(sP[0], out m) && int.TryParse(sP[1], out d) && int.TryParse(sP[2], out y))
                            {
                                dtStr = m + "/" + d + "/" + y;
                                SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
                                SqlCommand cmd = new SqlCommand();
                                cmd.Connection = cn;
                                cmd.CommandText = "SELECT OIGdt FROM Admin WHERE OIGdt<'" + dtStr + "'";
                                SqlDataAdapter da = new SqlDataAdapter(cmd);
                                DataTable dt = new DataTable();
                                try
                                {
                                    da.Fill(dt);
                                }
                                catch (Exception ex)
                                {
                                    er.code = 1;
                                    er.msg = "OIG - Query Fail " + ex.Message + " " + cmd.CommandText;

                                }
                                da.Dispose();
                                if (er.code == 0 && dt.Rows.Count != 0)
                                {
                                    // we need to get the file

                                    request = (HttpWebRequest)WebRequest.Create("https://oig.hhs.gov/exclusions/downloadables/UPDATED.csv");
                                    request.Method = "GET";
                                    request.ContentType = "application/x-www-form-urlencoded";
                                    using (response = (HttpWebResponse)request.GetResponse())
                                    {
                                        if (response.StatusCode == HttpStatusCode.OK)
                                        {
                                            using (Stream strm = response.GetResponseStream())
                                            {
                                                processOIGFile(strm, dtStr, ref er);
                                            }
                                        }
                                        else
                                        {
                                            er.code = 1;
                                            if (response.StatusCode != HttpStatusCode.OK)
                                                er.msg = "File return resulted in HTTP error";
                                        }
                                    }
                                }
                                dt.Dispose();
                                cmd.Dispose();
                            }
                            else
                            {
                                er.code = 1;
                                er.msg = "OIG Get Page Failed Date Not In Correct Format " + "'" + dtStr + "'";
                            }
                        }
                        else
                        {
                            er.code = 1;
                            er.msg = "OIG Get Page Failed End Date String Failed " + "'</h2>'";
                        }
                    }
                    else
                    {
                        er.code = 1;
                        er.msg = "OIG Get Page Failed Start Date String Failed " + "'UPDATED '";
                    }
                }
            }
            if (er.code != 0)
            {
                //someone needs to know
            }
        }
        public void processOIGFile(Stream input, string fileDate, ref Er er)
        {
            MemoryStream memStream = CopyToMemory(input);

            DataSet ds1 = new DataSet();
            // step 1 of file processing read file to table and upload the file
            try
            {
                StreamReader sr = new StreamReader(memStream);
                CsvReader csv = new CsvReader(sr);
                //  CsvReader csv = new CsvReader(File.OpenText(fp));
                csv.Read();
                csv.ReadHeader();

                // Read csv into datatable
                DataTable dataTable = new DataTable();

                dataTable.Columns.Add(new DataColumn("fn", Type.GetType("System.String")));
                dataTable.Columns.Add(new DataColumn("ln", Type.GetType("System.String")));
                dataTable.Columns.Add(new DataColumn("ad", Type.GetType("System.String")));
                dataTable.Columns.Add(new DataColumn("city", Type.GetType("System.String")));
                dataTable.Columns.Add(new DataColumn("state", Type.GetType("System.String")));
                dataTable.Columns.Add(new DataColumn("z", Type.GetType("System.String")));
                dataTable.Columns.Add(new DataColumn("dob", Type.GetType("System.DateTime")));

                while (csv.Read())
                {
                    string ln = csv.GetField("LASTNAME");
                    string fn = csv.GetField("FIRSTNAME");
                    if (ln != "" && fn != "")
                    {
                        DataRow row = dataTable.NewRow();
                        foreach (DataColumn column in dataTable.Columns)
                        {
                            row[0] = fn;
                            row[1] = ln;
                            row[2] = csv.GetField("ADDRESS");
                            row[3] = csv.GetField("CITY");
                            row[4] = csv.GetField("STATE");
                            row[5] = csv.GetField("ZIP");
                            string numericDate = csv.GetField("DOB");
                            if (numericDate != "")
                            {
                                try
                                {
                                    row[6] = Convert.ToDateTime(numericDate.Substring(4, 2) + "/" + numericDate.Substring(6, 2) + "/" + numericDate.Substring(0, 4));
                                }
                                catch (Exception ex)
                                {
                                    var r = ex.Message;
                                }
                            }
                          
                        }

                        dataTable.Rows.Add(row);
                    }
                }

                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("sp_UpLoadOIGTable", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@OIGList", dataTable);
                    cmd.Parameters.AddWithValue("@fileDate", fileDate);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(ds1);

                }
                dataTable.Dispose();
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }
        }
        public static MemoryStream CopyToMemory(Stream input)
        {
            // It won't matter if we throw an exception during this method;
            // we don't *really* need to dispose of the MemoryStream, and the
            // caller should dispose of the input stream
            MemoryStream ret = new MemoryStream();

            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                ret.Write(buffer, 0, bytesRead);
            }
            // Rewind ready for reading (typical scenario)
            ret.Position = 0;

            return ret;
        }



    }




}