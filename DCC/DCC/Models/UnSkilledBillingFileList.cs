using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{

    public class FileData
    {
        public string fileName { get; set; }
        public string lastModified{ get; set; }

        public DateTime? lastModifiedUtc { get; set; }
        public FileData()
        {
            fileName = "";
            lastModified = "";
           
        }
    }

    public class UnSkilledBillingFileList : ViewModelBase
    {
        public FileData billingFile = new FileData();
        public FileData coverDocument = new FileData();
    }
}