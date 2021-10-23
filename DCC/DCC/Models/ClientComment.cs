using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class ClientComment
    {
        public int id { get; set; }
        public string comment { get; set; }
        public string name { get; set; }
        public string date { get; set; }
        public int clsvId { get; set; }
    }
}