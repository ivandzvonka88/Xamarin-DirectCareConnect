using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models
{
    public class ClientProfile
    {
        public int clsvId { get; set; }
        public string name { get; set; }
        public string clId { get; set; }
        public string medicaidId { get; set; }

        public string fn { get; set; }
        public string ln { get; set; }
        public string dob { get; set; }
        public string dobISO { get; set; }
        public string sex { get; set; }

        public bool deleted { get; set; }

        public string clwNm { get; set; }
        public string clwPh { get; set; }
        public string clwEm { get; set; }
        public string physician{ get; set; }
        public string physicianTitle { get; set; }
        public string physicianFirstName { get; set; }
        public string physicianMI { get; set; }
        public string physicianLastName { get; set; }
        public string physicianSuffix { get; set; }
        public string physicianAgency { get; set; }
        public string physicianAddress { get; set; }

        public string physicianCity { get; set; }

        public string physicianState { get; set; }

        public string physicianZip { get; set; }
        public string physicianPhone { get; set; }
        public string physicianFax { get; set; }
        public string physicianEmail { get; set; }
        public string physicianNPI { get; set; }

        public bool selfResponsible { get; set; }

        public string responsiblePersonLn { get; set; }
        public string responsiblePersonFn { get; set; }

        public string responsiblePersonAddress { get; set; }
        public string responsiblePersonAddress2 { get; set; }
        public string responsiblePersonCity { get; set; }

        public string responsiblePersonState { get; set; }

        public string responsiblePersonZip { get; set; }
        public string responsiblePersonPhone { get; set; }
        public string responsiblePersonEmail { get; set; }

        public string responsiblePersonRelationship { get; set; }

        public int responsiblePersonRelationshipId { get; set; }

        public List<SelectOption> relationshipOptions { get; set; }

    }
}