using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class Documentation
    {
        public int noteId { get; set; }
        public string noteType { get; set; }
        public string svc { get; set; }
        public string staffName { get; set; }

        public string clientName { get; set; }
        public string dt { get; set; }

        public bool hasAttachment { get; set; }
        public string attachment { get; set; }
        public string fileName { get; set; }

        public string serviceType { get; set; }

    }
}