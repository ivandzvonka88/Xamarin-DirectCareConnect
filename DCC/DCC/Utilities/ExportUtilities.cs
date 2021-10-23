using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Reporting.WebForms;

namespace DCC
{
    public class ExportUtilities
    {

        public static byte[] ToPDF(LocalReport report, out string mimeType, out string fileNameExtension)
        {
            string deviceInfo = "<DeviceInfo>" +
      "  <OutputFormat>PDF</OutputFormat>" +
      "</DeviceInfo>";
            Warning[] warnings;
            string[] streams;
            byte[] renderedBytes;
            string encoding;

            //Render the report           
            renderedBytes = report.Render("PDF", deviceInfo, out mimeType, out encoding, out fileNameExtension, out streams, out warnings);

            return renderedBytes;
        }

        public static byte[] ToExcel(LocalReport report, out string mimeType, out string fileNameExtension)
        {
            string deviceInfo = "<DeviceInfo>" +
      "  <OutputFormat>EXCEL</OutputFormat>" +
      "</DeviceInfo>";
            Warning[] warnings;
            string[] streams;
            byte[] renderedBytes;
            string encoding;

            //Render the report           
            renderedBytes = report.Render("EXCEL", deviceInfo, out mimeType, out encoding, out fileNameExtension, out streams, out warnings);

            return renderedBytes;
        }
    }
}
