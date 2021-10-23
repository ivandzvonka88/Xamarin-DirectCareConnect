using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class RequiredCredential
    {
        public int credTypeId { get; set; }
        public string credName { get; set; }

    }
    public class CredentialModal
    {
        public int prId { get; set; }
        public int credId { get; set; }
        public int credTypeId { get; set; }
        public string credName { get; set; }
        public string docId { get; set; }
        public string validFrom { get; set; }
        public string validTo { get; set; }
        public string validFromISO { get; set; }
        public string validToISO { get; set; }
        public string docName { get; set; }
        public List<RequiredCredential> requiredCredentials { get; set; }

        public Er er = new Er();
    }

}