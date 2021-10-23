using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using DCC.Models;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf.IO;
using System.Configuration;
using DCC.Models.Providers;
using DCC.Helpers;
using DCC.SQLHelpers.Helpers;

namespace DCC.Controllers
{
    public class UnSkilledBillingController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;

        public UnSkilledBillingController()
        {
            sqlHelper = new SQLHelper();
        }


        [Authorize]
        public async Task<ActionResult> Index()
        {
            UnSkilledBillingFileList r = GetFileList();

            setViewModelBase((ViewModelBase)r);
           
            return View("Index", r);
        }


        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [AJAXAuthorize]
        public async Task<ActionResult> GenerateBillingFiles()
        {
            string templateFolder = Server.MapPath("~/Templates/");
            string billingFolder = Server.MapPath("~/Templates/");
            DateTime dt = DateTimeLocal(DateTime.UtcNow).AddMonths(-1);
            string BillingYear = "";
            string BillingMonth = "";
            // state fiscal year
            if (dt.Month > 6)
                BillingYear = Convert.ToString(dt.Year + 1 - 2000);
            else
                BillingYear = Convert.ToString(dt.Year - 2000);
            if (dt.Month > 9)
                BillingMonth = dt.Month.ToString();
            else
                BillingMonth = "0" + dt.Month;

            string BillingMonthAbr = string.Format("{0:MMM}", dt).ToUpper();

            DataSet Company = new DataSet();
            await Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("sp_GetCompanyById", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@coId", UserClaim.coid);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, Company);
                }
            });


            DataSet BillingRecords = new DataSet();
            await Task.Run(() =>
            {
                using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_DDDBillingGetUnskilled", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@endDate", DateTimeLocal(DateTime.UtcNow).ToShortDateString());
                    sqlHelper.ExecuteSqlDataAdapter(cmd, BillingRecords);
                }
            });

            string fileName = templateFolder + "DDDBillingFileNew.xls";

            HSSFWorkbook BillingFile;
            using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                BillingFile = new HSSFWorkbook(file);
            }



            // header sheet
            DataRow dr1 = Company.Tables[0].Rows[0];
            string fileNameRoot = dr1["pCode"] + BillingYear + BillingMonth + "001";


            ISheet HEADER = BillingFile.GetSheet("HEADER");
            IRow HeaderRow = HEADER.GetRow(1);
            HeaderRow.GetCell(0).SetCellValue((string)dr1["TaxId"]);
            HeaderRow.GetCell(1).SetCellValue(BillingMonthAbr);
            HeaderRow.GetCell(2).SetCellValue(BillingYear);
            HeaderRow.GetCell(3).SetCellValue("P2");
            //HeaderRow.Cells[4].SetCellValue(dr["NPI"]); - do not set for none Skilled
            HeaderRow.GetCell(5).SetCellValue((string)dr1["NonSkilledProvAhcccsId"]);


            ISheet DETAILS = BillingFile.GetSheet("DETAILS");
            int DetailRowIndex = 1;
            decimal TotalBilled = 0;
            decimal TotalUnits = 0;
            int TotalRecords = 0;


            foreach (DataRow dr2 in BillingRecords.Tables[0].Rows)
            {
                IRow DetailsRow = DETAILS.GetRow(DetailRowIndex);

                TotalBilled += Math.Round((decimal)dr2["un"] * (decimal)dr2["rate"], 2, MidpointRounding.AwayFromZero);
                TotalRecords += 1;
                TotalUnits += (decimal)dr2["un"];

                DetailsRow.CreateCell(0).SetCellValue((string)dr2["bloc"]);//ProvSvcLocation 
                DetailsRow.GetCell(2).SetCellValue((string)dr2["clid"]); //clientId
                DetailsRow.GetCell(3).SetCellValue((DateTime)dr2["dt"]); // start date
                DetailsRow.GetCell(4).SetCellValue((DateTime)dr2["dt"]); // end date
                DetailsRow.CreateCell(5).SetCellValue((string)dr2["svc"]); // 3 letter service code
                DetailsRow.CreateCell(7).SetCellValue(Convert.ToDouble(dr2["un"])); // delivered units
                DetailsRow.CreateCell(8).SetCellValue(0); // absent units
                DetailsRow.CreateCell(9).SetCellValue(Convert.ToDouble(dr2["rate"])); // rate
                DetailsRow.CreateCell(20).SetCellValue((string)dr2["billingId"]);
                DetailsRow.CreateCell(23).SetCellValue((int)dr2["pos"]);
                if ((string)dr2["svc"] == "ATC" || (string)dr2["svc"] == "HAH" || (string)dr2["svc"] == "HAI" || (string)dr2["svc"] == "RSP" || (string)dr2["svc"] == "RSD")
                    DetailsRow.CreateCell(24).SetCellValue((string)dr2["Modifier1"]); // Time Of Day Modifier
                if ((string)dr2["svc"] == "ATC")
                    DetailsRow.CreateCell(25).SetCellValue((string)dr2["Modifier2"]); // ATC relationship Modifier

                DetailsRow.CreateCell(48).SetCellValue(dr2["dddDiagnosis"].ToString().Replace(".",""));
          



                DetailRowIndex++;
            }
            ISheet FOOTER = BillingFile.GetSheet("FOOTER");

            IRow FooterRow = FOOTER.GetRow(1);



            FooterRow.GetCell(0).SetCellValue(TotalRecords);
            FooterRow.GetCell(1).SetCellValue(Convert.ToDouble(TotalUnits));
            FooterRow.GetCell(2).SetCellValue(Convert.ToDouble(TotalBilled));



            string fileDestination = billingFolder + fileNameRoot + ".xls";

            try
            {

                using (var ms = new NpoiMemoryStream())
                {
                    ms.AllowClose = false;
                    BillingFile.Write(ms);
                    ms.Flush();
                    ms.Seek(0, SeekOrigin.Begin);
                    byte[] data = new byte[ms.Length];
                    ms.Read(data, 0, data.Length);
                    FileData f = new FileData("nonskilledbillingfiles", UserClaim.blobStorage);
                    f.StoreFile(data, fileNameRoot + ".xls");
                    ms.AllowClose = true;
                }




            }
            catch (Exception ex)
            {
                var rx = ex.Message;
            }


            //create cover sheet
            string coverSheetTemplate = templateFolder + "DDDCoverDoc.pdf";
            DateTime stdt = Convert.ToDateTime(dt.Month + "/1/" + (2000 + Convert.ToInt32(BillingYear)));
            PdfDocument PDFDoc = PdfReader.Open(coverSheetTemplate, PdfDocumentOpenMode.Import);
            PdfDocument PDFNewDoc = new PdfDocument();
            PdfPage pp = PDFNewDoc.AddPage(PDFDoc.Pages[0]);
            XGraphics gfx = XGraphics.FromPdfPage(pp);
            XTextFormatter tf = new XTextFormatter(gfx);
            float y = 97;
            float yOffset = 24.5F;
            int x1 = 37;
            int x2 = 308;
            int x3 = 440;
            XFont A12B = new XFont("Arial", 11, XFontStyle.Regular);

            DataRow drx = Company.Tables[0].Rows[0];

            XRect rect = new XRect(x1, y, 200, 13);
            gfx.DrawRectangle(XBrushes.Transparent, rect);
            tf.DrawString((string)drx["name"], A12B, XBrushes.Black, rect, XStringFormats.TopLeft);
            rect = new XRect(x2, y, 200, 13);
            gfx.DrawRectangle(XBrushes.Transparent, rect);
            tf.DrawString((string)drx["pCode"], A12B, XBrushes.Black, rect, XStringFormats.TopLeft);

            y += yOffset;
            rect = new XRect(x1, y, 200, 13);
            gfx.DrawRectangle(XBrushes.Transparent, rect);
            tf.DrawString((string)drx["NonSkilledBillingContact"], A12B, XBrushes.Black, rect, XStringFormats.TopLeft);
            rect = new XRect(x2, y, 200, 13);
            gfx.DrawRectangle(XBrushes.Transparent, rect);
            tf.DrawString((string)drx["TaxId"], A12B, XBrushes.Black, rect, XStringFormats.TopLeft);

            y += yOffset;
            rect = new XRect(x1, y, 200, 13);
            gfx.DrawRectangle(XBrushes.Transparent, rect);
            tf.DrawString((string)drx["NonSkilledBillingPhone"], A12B, XBrushes.Black, rect, XStringFormats.TopLeft);
            rect = new XRect(x2, y, 200, 13);
            gfx.DrawRectangle(XBrushes.Transparent, rect);
            tf.DrawString((string)drx["NonSkilledBillingEmail"], A12B, XBrushes.Black, rect, XStringFormats.TopLeft);

            y += yOffset;
            rect = new XRect(x1, y, 200, 13);
            gfx.DrawRectangle(XBrushes.Transparent, rect);
            tf.DrawString((string)drx["NonSkilledBillingAddress"], A12B, XBrushes.Black, rect, XStringFormats.TopLeft);

            y += yOffset;
            rect = new XRect(x1, y, 200, 13);
            gfx.DrawRectangle(XBrushes.Transparent, rect);
            tf.DrawString((string)drx["NonSkilledBillingCity"], A12B, XBrushes.Black, rect, XStringFormats.TopLeft);
            rect = new XRect(x2, y, 200, 13);
            gfx.DrawRectangle(XBrushes.Transparent, rect);
            tf.DrawString("AZ", A12B, XBrushes.Black, rect, XStringFormats.TopLeft);
            rect = new XRect(x3, y, 200, 13);
            gfx.DrawRectangle(XBrushes.Transparent, rect);
            tf.DrawString((string)drx["NonSkilledBillingZip"], A12B, XBrushes.Black, rect, XStringFormats.TopLeft);

            y += yOffset;
            rect = new XRect(x1, y, 200, 13);
            gfx.DrawRectangle(XBrushes.Transparent, rect);
            tf.DrawString((string.Format("{0:MMM}", stdt)).ToUpper() + " " + stdt.Year, A12B, XBrushes.Black, rect, XStringFormats.TopLeft);
            rect = new XRect(x2, y, 200, 13);
            gfx.DrawRectangle(XBrushes.Transparent, rect);
            tf.DrawString(String.Format("{0:c}", Math.Round(TotalBilled, 2)), A12B, XBrushes.Black, rect, XStringFormats.TopLeft);


            using (var ms = new MemoryStream())
            {
                PDFNewDoc.Save(ms);
                ms.Flush();
                ms.Seek(0, SeekOrigin.Begin);

                byte[] data = new byte[ms.Length];
                ms.Read(data, 0, data.Length);
                FileData f = new FileData("nonskilledbillingfiles", UserClaim.blobStorage);
                f.StoreFile(data, fileNameRoot + ".pdf");

            }

            Company.Dispose();
            BillingRecords.Dispose();
            UnSkilledBillingFileList r = GetFileList();
            return PartialView("UnSkilledBillingFileList", r);
        }


        public class NpoiMemoryStream : MemoryStream
        {
            public NpoiMemoryStream()
            {
                // We always want to close streams by default to
                // force the developer to make the conscious decision
                // to disable it.  Then, they're more apt to remember
                // to re-enable it.  The last thing you want is to
                // enable memory leaks by default.  ;-)
                AllowClose = true;
            }

            public bool AllowClose { get; set; }

            public override void Close()
            {
                if (AllowClose)
                    base.Close();
            }
        }

        private UnSkilledBillingFileList GetFileList()
        {
            string billingFolder = Server.MapPath("~/Templates/");
            DateTime dt = DateTimeLocal(DateTime.UtcNow).AddMonths(-1);
            string BillingYear = "";
            string BillingMonth = "";
            // state fiscal year
            if (dt.Month > 6)
                BillingYear = Convert.ToString(dt.Year + 1 - 2000);
            else
                BillingYear = Convert.ToString(dt.Year - 2000);
            if (dt.Month > 9)
                BillingMonth = dt.Month.ToString();
            else
                BillingMonth = "0" + dt.Month;

            string fileNameRoot = UserClaim.pcode + BillingYear + BillingMonth + "001";

            FileData f = new FileData("nonskilledbillingfiles", UserClaim.blobStorage);
            UnSkilledBillingFileList r = f.GetBillingFileList(fileNameRoot);

            if (r.billingFile.lastModifiedUtc != null)
                r.billingFile.lastModified = DateTimeLocal((DateTime)r.billingFile.lastModifiedUtc).ToString();
            if (r.coverDocument.lastModifiedUtc != null)
                r.coverDocument.lastModified = DateTimeLocal((DateTime)r.coverDocument.lastModifiedUtc).ToString();


            return r;
        }


        [HttpGet]
        [Authorize]
        public ActionResult GetBillingFile(string fileName)
        {
            FileData f = new FileData("nonskilledbillingfiles", UserClaim.blobStorage);
            byte[] data = f.GetFile(fileName);
            Response.AddHeader("Content-Disposition", "attachment;filename=\"" + fileName + "\"");
            return new FileContentResult(data, MimeMapping.GetMimeMapping(fileName));
        }
    }
}