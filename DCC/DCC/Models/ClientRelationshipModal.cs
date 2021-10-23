using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class ClientRelationshipModal
    {
        public List<SelectOption> atcRelationships { get; set; }
        public List<SelectOption> services { get; set; }
    }
}