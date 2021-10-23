using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class CommentHistory
    {
        public int commentId { get; set; }
        public string comment { get; set; }
        public string commentator { get; set; }
        public string subject { get; set; }
        public string cmtDt { get; set; }
        public string cmtType { get; set; }
    }
}