using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models.CredentialSettings
{
    public class RequiredCredentials
    {

        public List<Role> roles{get; set;}
        public List<CredentialType> credentialTypes { get; set; }
        public Er er = new Er();

        

    }

    public class Role
    {
        public int roleId { get; set; }
        public string roleName { get; set; }
        public List<CredentialSetting> credentialSettings { get; set; }
    }


    public class CredentialType
    {
        public string credName { get; set; }
        public int credTypeId { get; set; }

        public bool roleSpecific { get; set; }
    }
    public class CredentialSetting
    {
        public int roleId { get; set; }
        public int credId { get; set;}
        public bool blocking { get; set; }
        public bool required { get; set; }
    }

    public class CredentialTableResp
    {
        public List<CredentialRow> credentialRows { get; set; }
    }


    public class CredentialRow
    {
        public int roleId { get; set; }
        public int credTypeId { get; set; }
        public bool blocking { get; set; }
        public bool required { get; set; }
    }

    public class NewCredential
    {
        public string credName { get; set; }
        public bool roleSpecific { get; set; }
    }
}