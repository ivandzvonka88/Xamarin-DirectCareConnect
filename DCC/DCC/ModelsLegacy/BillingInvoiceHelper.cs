using DCC.Models.Providers;
using DCC.SQLHelpers.Helpers;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web;

namespace DCC.ModelsLegacy
{
    public class BillingInvoiceHelper
    {
        private Document _document;
        private CompanyInfoDTO _company;
        private string _connStr;
        private SQLHelper _sqlHelper;

        public BillingInvoiceHelper(CompanyInfoDTO company, string connStr)
        {
            this._connStr = connStr;
            this._company = company;
        }

        public byte[] GenerateClientInvoices(string clientIDs, int statusCode = -2)
        {
            List<SourceFile> invFiles = new List<SourceFile>();
            DataSet ds = GetClientClaimDetails(clientIDs, statusCode);

            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                this._document = new Document();
                this._document.Info.Title = this._company.Name + " Invoice";

                string filter = "ClientId = " + dr["clsvID"];
                var matchedRows = ds.Tables[1].Select(filter, "ClaimDate");

                CreatePage(dr, matchedRows);

                var ms = new MemoryStream();
                var renderer = new PdfDocumentRenderer(false)
                {
                    Document = this._document
                };
                renderer.RenderDocument();
                renderer.PdfDocument.Save(ms);
                byte[] invBytes = new byte[ms.Length];
                ms.Read(invBytes, 0, invBytes.Length);

                var filename = this._company.Name + "_" + dr["ln"] + "_" + dr["fn"];
                invFiles.Add(new SourceFile { Extension = "pdf", FileBytes = invBytes, Name = filename });
            }

            using(MemoryStream zipWriter = new MemoryStream())
            {
                using(ZipArchive zip = new ZipArchive(zipWriter, ZipArchiveMode.Create, true))
                {
                    foreach(var f in invFiles)
                    {
                        ZipArchiveEntry zipItem = zip.CreateEntry(f.Name + "." + f.Extension);
                        
                        // add the item bytes to the zip entry by opening the original file and copying the bytes 
                        using (MemoryStream originalFileMemoryStream = new MemoryStream(f.FileBytes))
                        {
                            using (Stream entryStream = zipItem.Open())
                            {
                                originalFileMemoryStream.CopyTo(entryStream);
                            }
                        }
                    }
                }

                return zipWriter.ToArray();
            }
        }

        private DataSet GetClientClaimDetails(string clientIDs, int statusCode = -2)
        {
            using(SqlConnection conn = new SqlConnection(this._connStr))
            {
                SqlCommand cmd = new SqlCommand("sp_BillingGetInvoiceDetail", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@clientIDs", clientIDs);
                cmd.Parameters.AddWithValue("@statusCode", statusCode);

                DataSet ds = new DataSet();
                this._sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                return ds;
            }
        }

        private void CreatePage(DataRow clientInfoRow, DataRow[] claims)
        {
            Font A13B = new Font("Arial", 13) { Bold = true };
            Font A11B = new Font("Arial", 11) { Bold = true };

            Section section = this._document.AddSection();

            var invText = section.AddTextFrame();
            invText.Height = "1.25cm";
            invText.Width = "7.0cm";
            invText.Left = ShapePosition.Left;
            invText.RelativeHorizontal = RelativeHorizontal.Margin;
            invText.Top = "0.5cm";
            invText.RelativeVertical = RelativeVertical.Page;

            var invTextPara = invText.AddParagraph("INVOICE");
            invTextPara.Format.Font = A13B;

            //Company Address
            var companyAddress = section.AddTextFrame();
            companyAddress.Height = "3.0cm";
            companyAddress.Width = "7.0cm";
            companyAddress.Left = ShapePosition.Left;
            companyAddress.RelativeHorizontal = RelativeHorizontal.Margin;
            companyAddress.Top = "1.5cm";
            companyAddress.RelativeVertical = RelativeVertical.Page;

            var companyAddressPara = companyAddress.AddParagraph();
            companyAddressPara.AddFormattedText(this._company.Name, A11B);
            companyAddressPara.AddLineBreak();
            companyAddressPara.AddText(this._company.Address.Line1);
            companyAddressPara.AddLineBreak();

            if (!string.IsNullOrEmpty(this._company.Address.Line2))
            {
                companyAddressPara.AddText(this._company.Address.Line2);
                companyAddressPara.AddLineBreak();
            }

            string cityStateZip = this._company.Address.City + ", " + this._company.Address.State + " " + this._company.Address.PostalCode;
            companyAddressPara.AddText(cityStateZip);

            //Client Address
            var clientAddress = section.AddTextFrame();
            clientAddress.Height = "3.0cm";
            clientAddress.Width = "7.0cm";
            clientAddress.Left = ShapePosition.Left;
            clientAddress.RelativeHorizontal = RelativeHorizontal.Margin;
            clientAddress.Top = "4.0cm";
            clientAddress.RelativeVertical = RelativeVertical.Page;

            var clientAddressPara = clientAddress.AddParagraph();
            clientAddressPara.AddText(this._company.Name);
            clientAddressPara.AddLineBreak();
            clientAddressPara.AddText(this._company.Address.Line1);
            clientAddressPara.AddLineBreak();

            if (!string.IsNullOrEmpty(this._company.Address.Line2))
            {
                clientAddressPara.AddText(this._company.Address.Line2);
                clientAddressPara.AddLineBreak();
            }

            cityStateZip = this._company.Address.City + ", " + this._company.Address.State + " " + this._company.Address.PostalCode;
            clientAddressPara.AddText(cityStateZip);

            //Client Summary
            var clientSummary = section.AddTextFrame();
            clientSummary.Height = "3.5cm";
            clientSummary.Width = "7.0cm";
            clientSummary.Left = ShapePosition.Right;
            clientSummary.RelativeHorizontal = RelativeHorizontal.Margin;
            clientSummary.Top = "2.0cm";
            clientSummary.RelativeVertical = RelativeVertical.Page;
            clientSummary.LineFormat.Width = Unit.FromCentimeter(0.005);
            clientSummary.LineFormat.Color = Colors.Black;
            clientSummary.FillFormat.Color = Colors.LightGray;

            var claimTable = section.AddTable();
            claimTable.Borders.Color = Colors.Black;
            claimTable.Borders.Width = 0.05;

            //define columns
            // col 0 - DOS
            Column col = claimTable.AddColumn("2cm");
            col.Format.Alignment = ParagraphAlignment.Center;

            // col 1 - Provider
            col = claimTable.AddColumn("5cm");
            col.Format.Alignment = ParagraphAlignment.Left;

            // col 2 - Service
            col = claimTable.AddColumn("3cm");
            col.Format.Alignment = ParagraphAlignment.Left;

            // col 3 - Total Amt
            col = claimTable.AddColumn("1.5cm");
            col.Format.Alignment = ParagraphAlignment.Right;

            // col 4 - Ins Pymt/Adj
            col = claimTable.AddColumn("1.5cm");
            col.Format.Alignment = ParagraphAlignment.Right;

            // col 5 - Patient Pymt
            col = claimTable.AddColumn("1.5cm");
            col.Format.Alignment = ParagraphAlignment.Right;

            // col 6 - Amt Due
            col = claimTable.AddColumn("1.5cm");
            col.Format.Alignment = ParagraphAlignment.Right;

            //create table header
            Row row = claimTable.AddRow();
            row.HeadingFormat = true;
            row.Format.Alignment = ParagraphAlignment.Center;
            row.Format.Font.Bold = true;
            row.Shading.Color = Colors.LightBlue;
            row.Cells[0].AddParagraph("Service Date");
            row.Cells[0].Format.Font.Bold = true;
            row.Cells[0].Format.Alignment = ParagraphAlignment.Center;
            row.Cells[0].VerticalAlignment = VerticalAlignment.Bottom;
            row.Cells[1].AddParagraph("Provider");
            row.Cells[1].Format.Font.Bold = true;
            row.Cells[1].Format.Alignment = ParagraphAlignment.Center;
            row.Cells[1].VerticalAlignment = VerticalAlignment.Bottom;
            row.Cells[2].AddParagraph("Service");
            row.Cells[2].Format.Font.Bold = true;
            row.Cells[2].Format.Alignment = ParagraphAlignment.Center;
            row.Cells[2].VerticalAlignment = VerticalAlignment.Bottom;
            row.Cells[3].AddParagraph("Total Amount");
            row.Cells[3].Format.Font.Bold = true;
            row.Cells[3].Format.Alignment = ParagraphAlignment.Center;
            row.Cells[3].VerticalAlignment = VerticalAlignment.Bottom;
            row.Cells[4].AddParagraph("Insurance Pymt/Adj");
            row.Cells[4].Format.Font.Bold = true;
            row.Cells[4].Format.Alignment = ParagraphAlignment.Center;
            row.Cells[4].VerticalAlignment = VerticalAlignment.Bottom;
            row.Cells[5].AddParagraph("Patient Pymt");
            row.Cells[5].Format.Font.Bold = true;
            row.Cells[5].Format.Alignment = ParagraphAlignment.Center;
            row.Cells[5].VerticalAlignment = VerticalAlignment.Bottom;
            row.Cells[6].AddParagraph("Amount Due");
            row.Cells[6].Format.Font.Bold = true;
            row.Cells[6].Format.Alignment = ParagraphAlignment.Center;
            row.Cells[6].VerticalAlignment = VerticalAlignment.Bottom;

            foreach (var claim in claims)
            {

            }
        }

        public class SourceFile
        {
            public string Name { get; set; }
            public string Extension { get; set; }
            public Byte[] FileBytes { get; set; }
        }
    }
}