using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class NonSkilledRebillPage : ViewModelBase
    {
        public int pgCnt { get; set; }
        public int pg { get; set; }
        public string[] pageInfo { get; set; }
        public billItem[] b { get; set; }
        public Er er = new Er();
    }


 
    public class billItem
    {
        public string tbl { get; set; }
        public int id { get; set; }
        public int year { get; set; }
        public int day { get; set; }
        public string month { get; set; }
        public string fn { get; set; }
        public string ln { get; set; }
        public string svc { get; set; }
        public string loc { get; set; }
        public decimal un { get; set; }
        public decimal ajun { get; set; }
        public string er { get; set; }
        public string au { get; set; }
        public int rat { get; set; }
        public int chg { get; set; }
    }



}