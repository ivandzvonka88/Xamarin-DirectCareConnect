using System;

using DCC.Models;
namespace DCC
{

   

    public class HCBSResult
    {
        public string timeZone;

        public int HCBSEmpHrsId; // id of the provider record
        public int clsvId; // client id change applies too
        public int clsvidId; // client service id
        public int serviceId; // id of the service
        public int prId;
      
        public string date1;
        public string date2;

        public DateTime utcPartition1Start;
        public DateTime utcPartition1End;

        public DateTime? utcPartition2Start;
        public DateTime? utcPartition2End;


        public string svc;

        public DateTime? utcIn;
        public DateTime? utcOut;
        public DateTime? adjutcIn;
        public DateTime? adjutcOut;
        public decimal unx = 0; // employee unit change
        public decimal unBillChg = 0; // billing unit change
        public int auid;
        public int locationTypeId;
        public int clientLocationId;// key for client location 
        public bool onHold = false;
        public bool onHoldNoOutHours = false;
        public bool onHoldLateNote = false;
        public bool onHoldProviderOverlap = false;
        public bool onHoldNoCredential= false;
        public int callType;
        public int inCallType;
        public int outCallType;

        public int? startLocationTypeId;
        public int? startClientLocationId;// key for client location 
        public decimal startLat;
        public decimal startLon;


        public int? endLocationTypeId;
        public int? endClientLocationId;// key for client location 
        public decimal? endLat;
        public decimal? endLon;
        public bool isEVV;

        public string LocalPeriodStart;
        public string LocalPeriodEnd;

        public Er er = new Er();

    }


  
}