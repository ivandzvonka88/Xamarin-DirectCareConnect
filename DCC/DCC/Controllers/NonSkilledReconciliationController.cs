using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using DCC.Models;
using DCC.SQLHelpers.Helpers;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using CsvHelper;
namespace DCC.Controllers
{
    public class NonSkilledReconciliationController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;

        public NonSkilledReconciliationController()
        {
            sqlHelper = new SQLHelper();
        }

        [Authorize]
        public ActionResult Index()
        {
            NonSkilledRebillPage r = new NonSkilledRebillPage();

            setViewModelBase((ViewModelBase)r);

            return View("Index", r);
        }
       

        [HttpPost]
        [Authorize]
        public ActionResult UploadReconcileFile(IEnumerable<HttpPostedFileBase> files)
        {
            Er er = new Er();
            if (files != null)
            {
                var file = files.FirstOrDefault();
                // Verify that the user selected a file
                if (file != null && file.ContentLength > 0)
                {
                    try
                    {

                        byte[] fileData = new byte[file.InputStream.Length];
                        file.InputStream.Read(fileData, 0, fileData.Length);
                        processReconcilliationFile(fileData, ref er);
                    }
                    catch (Exception ex)
                    {
                        er.code = 1;
                        er.msg = ex.Message;
                    }


                }

            }
            return Json(er);
        }
        private List<string> CsvParser(string csvText)
        {
            List<string> tokens = new List<string>();

            int last = -1;
            int current = 0;
            bool inText = false;

            while (current < csvText.Length)
            {
                switch (csvText[current])
                {
                    case '"':
                        inText = !inText; break;
                    case ',':
                        if (!inText)
                        {
                            tokens.Add(csvText.Substring(last + 1, (current - last)).Trim(' ', ',').Replace("\"", ""));
                            last = current;
                        }
                        break;
                    default:
                        break;
                }
                current++;
            }

            if (last != csvText.Length - 1)
            {
                tokens.Add(csvText.Substring(last + 1).Trim().Replace("\"", ""));
            }

            return tokens;
        }

        private void processReconcilliationFile(byte[] fileData, ref Er er)
        {

           

         

            // Read csv into datatable
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn("dt", Type.GetType("System.DateTime")));
            dataTable.Columns.Add(new DataColumn("status", Type.GetType("System.Int32")));
            dataTable.Columns.Add(new DataColumn("clId", Type.GetType("System.String")));
            dataTable.Columns.Add(new DataColumn("loc", Type.GetType("System.String")));
            dataTable.Columns.Add(new DataColumn("svc", Type.GetType("System.String")));
            dataTable.Columns.Add(new DataColumn("units", Type.GetType("System.Decimal")));
            dataTable.Columns.Add(new DataColumn("rate", Type.GetType("System.Decimal")));
            dataTable.Columns.Add(new DataColumn("totalPaid", Type.GetType("System.Decimal")));
            dataTable.Columns.Add(new DataColumn("transactionId", Type.GetType("System.String")));
            dataTable.Columns.Add(new DataColumn("errorMsg", Type.GetType("System.String")));
            dataTable.Columns.Add(new DataColumn("tblType", Type.GetType("System.String")));
            dataTable.Columns.Add(new DataColumn("id", Type.GetType("System.Int32")));

     

            int headerPosition = 0;
            using (var reader = new StreamReader(new MemoryStream(fileData)))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line.Length >= 12 && line.Replace("\"", "").Substring(0, 12) != "CLAIM_STATUS")
                        headerPosition++;
                    else
                        break;
                }
            }

            using (var reader = new StreamReader(new MemoryStream(fileData)))
            {
                while (headerPosition != 0)
                {
                   var x = reader.ReadLine();
                    headerPosition--;
                }

             

                using (var csv = new CsvReader(reader))
                {
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        // temporary if because of error in RSD billing on first run (billed both Statewide and Flagstaff rate)
                        if (Convert.ToDecimal(csv.GetField("UNIT_RATE")) != 287.16M)
                        {
                            int status;
                            if (csv.GetField("CLAIM_STATUS") == "PAID")
                                status = 2;
                            else
                                status = 1;
                            string clId = "0000000000".Substring(0, 10 - csv.GetField("CLIENT_ID").Length) + csv.GetField("CLIENT_ID");
                            string vendorControlNum = csv.GetField("VENDOR_CONTROL_NUMBER");
                            string tableType = vendorControlNum.Substring(0, 1);
                            int billingId = Convert.ToInt32(vendorControlNum.Substring(1));

                            DataRow row = dataTable.NewRow();
                            row[0] = Convert.ToDateTime(csv.GetField("START_DATE"));
                            row[1] = status;
                            row[2] = clId;
                            row[3] = csv.GetField("ASSISTS_PROV_LOC");
                            row[4] = csv.GetField("SVC");
                            row[5] = Convert.ToDecimal(csv.GetField("BILLED_UNITS"));
                            row[6] = Convert.ToDecimal(csv.GetField("UNIT_RATE"));
                            row[7] = Convert.ToDecimal(csv.GetField("PAID_AMOUNT"));
                            row[8] = csv.GetField("TRANS_NUMBER");
                            row[9] = csv.GetField("ERROR_DESCRIPTION_LIST");
                            row[10] = tableType;
                            row[11] = billingId;
                            dataTable.Rows.Add(row);


                        }

                        




                    }
                    


                    using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_DDDReconcileNonskilled", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@paymentsTbl", dataTable);
                        cn.Open();
                        cmd.ExecuteNonQuery();
                        cmd.Dispose();

                    }


                }
            }




            dataTable.Dispose();
        }
    }
}