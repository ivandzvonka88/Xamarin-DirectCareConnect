using System;
using System.Linq;
using System.Data.SqlClient;
using System.Data;
using DCC.SQLHelpers.Helpers;
using DCC.Models;


namespace DCC
{
    public class InOutProcessor {

        private readonly SQLHelper sqlHelper;

        private string conStr { get; set; }
        private string timeZone { get; set; }

        public int HCBSEmpHrsId; // id of the provider record
        public int clsvId; // client id change applies too
        public int clsvidId; // client service id
        public int serviceId; // id of the service
        public int prId;


        public DateTime utcPartition1Start;
        public DateTime utcPartition1End;

        public DateTime? utcPartition2Start;
        public DateTime? utcPartition2End;


        public string svc;

        public string date1;
        public string date2;
        public string adjdate1;
        public DateTime? utcIn;
        public DateTime? utcOut;
        public DateTime? adjutcIn;
        public DateTime? adjutcOut;
        public decimal unx = 0; // employee unit change
        public decimal unBillChg = 0; // billing unit change
        public int auid;
        public int locationTypeId;
        public int clientLocationId;// key for client location 

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

      //  public Er er = new Er();



        public InOutProcessor(string _conStr, string _timeZone)
        {
            conStr = _conStr;
            timeZone = _timeZone;
            sqlHelper = new SQLHelper();
        }
        const int STARTTIME = 1;
        const int ENDTIME = 2;


        public Int64 updateTherapyRecord(ref Er er)
        {
            DataSet ds = new DataSet();
            int sessionId = 0;
            using (SqlConnection cn = new SqlConnection(conStr))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("sp_ApiInOutSetSessionTherapyHours", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@ClientSessionTherapyID", HCBSEmpHrsId);

                    cmd.Parameters.AddWithValue("@prId", prId);
                    cmd.Parameters.AddWithValue("@clientId", clsvId);
                    cmd.Parameters.AddWithValue("@clientServiceId", clsvidId);
                    cmd.Parameters.AddWithValue("@serviceId", serviceId);
                    cmd.Parameters.AddWithValue("@svc", svc);
                    cmd.Parameters.AddWithValue("@startclientLocationId", startClientLocationId);
                    cmd.Parameters.AddWithValue("@startlocationTypeId", startLocationTypeId);
                    cmd.Parameters.AddWithValue("@startlat", startLat);
                    cmd.Parameters.AddWithValue("@startlon", startLon);
                    cmd.Parameters.AddWithValue("@endclientLocationId", endClientLocationId);
                    cmd.Parameters.AddWithValue("@endlocationTypeId", endLocationTypeId);
                    cmd.Parameters.AddWithValue("@endlat", endLat);
                    cmd.Parameters.AddWithValue("@endlon", endLon);

                    cmd.Parameters.AddWithValue("@date", date1);
                    cmd.Parameters.AddWithValue("@utcIn", utcIn);
                    cmd.Parameters.AddWithValue("@utcOut", utcOut == null ? DBNull.Value : (object)utcOut);
                    
                    
                    cmd.Parameters.AddWithValue("@adjdate", adjdate1);
                    cmd.Parameters.AddWithValue("@adjutcIn", adjutcIn == null ? DBNull.Value : (object)adjutcIn);
                    cmd.Parameters.AddWithValue("@adjutcOut", adjutcOut == null ? DBNull.Value : (object)adjutcOut);


                    cmd.Parameters.AddWithValue("@inCallType", inCallType);
                    cmd.Parameters.AddWithValue("@outCallType", utcOut == null ? DBNull.Value : (object)outCallType);
                    cmd.Parameters.AddWithValue("@callType", callType);
                    cmd.Parameters.AddWithValue("@isEVV", isEVV);



                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    sessionId = Convert.ToInt32(Convert.ToInt64(ds.Tables[0].Rows[0].ItemArray[0]));
                }
                catch (Exception ex)
                {
                    er.code = 1;
                    er.msg = ex.Message;

                }
                ds.Dispose();
                return sessionId;
            }

        }
        public void updateTherapyRecordAdjusted(ref Er er)
        {

            using (SqlConnection cn = new SqlConnection(conStr))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("sp_ApiInOutSetSessionTherapyHoursAdjust", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@ClientSessionTherapyID", HCBSEmpHrsId);
 
                    cmd.Parameters.AddWithValue("@clientLocationId", clientLocationId);
                    cmd.Parameters.AddWithValue("@locationTypeId", locationTypeId);
                
                    cmd.Parameters.AddWithValue("@date", date1);
                    cmd.Parameters.AddWithValue("@utcIn", utcIn);
                    cmd.Parameters.AddWithValue("@utcOut", utcOut == null ? DBNull.Value : (object)utcOut);


                    cmd.Parameters.AddWithValue("@adjdate", adjdate1);
                    cmd.Parameters.AddWithValue("@adjutcIn", adjutcIn == null ? DBNull.Value : (object)adjutcIn);
                    cmd.Parameters.AddWithValue("@adjutcOut", adjutcOut == null ? DBNull.Value : (object)adjutcOut);

                    cn.Open();
                    cmd.ExecuteNonQuery();
                    cn.Close();

                }
                catch (Exception ex)
                {
                    er.code = 1;
                    er.msg = ex.Message;

                }

            }

        }

        public void checkTherapyRecordAgainstAuths(ref Er er)
        {
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection cn = new SqlConnection(conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ApiInOutTherapyCheckOverBilling", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@ClientSessionTherapyID", HCBSEmpHrsId);

                    cmd.Parameters.AddWithValue("@clientServiceId", clsvidId);
                    cmd.Parameters.AddWithValue("@serviceId", serviceId);
                    cmd.Parameters.AddWithValue("@date", date1);

                    cmd.Parameters.AddWithValue("@newUnits", calulateUnits15MinRule((DateTime)utcOut, (DateTime)utcIn));
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }

            if (ds.Tables.Count != 0)
            {
                er.code = Convert.ToInt32(ds.Tables[0].Rows[0].ItemArray[0]);
                er.msg = Convert.ToString(ds.Tables[0].Rows[0].ItemArray[1]);
            }
            ds.Dispose();

        }



        public int updateHCBSRecord(ref Er er, int userPrId)
        {
            int sessionId = 0;
            DataSet ds = new DataSet();

            try
            {
                using (SqlConnection cn = new SqlConnection(conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ApiInOutHCBSEmpUpdate", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@HCBSHrsEmpId", HCBSEmpHrsId);

                    cmd.Parameters.AddWithValue("@prId", prId);
                    cmd.Parameters.AddWithValue("@clsvId", clsvId);
                    cmd.Parameters.AddWithValue("@clsvidId", clsvidId);
                    cmd.Parameters.AddWithValue("@serviceId", serviceId);
                    cmd.Parameters.AddWithValue("@svc", svc);

                    cmd.Parameters.AddWithValue("@startlocationTypeId", startLocationTypeId == null ? 0 : (int)startLocationTypeId);
                    cmd.Parameters.AddWithValue("@startclientLocationId", startClientLocationId == null ? 0 : (int)startClientLocationId);
                    cmd.Parameters.AddWithValue("@startLat", startLat);
                    cmd.Parameters.AddWithValue("@startLon", startLon);

                    cmd.Parameters.AddWithValue("@endLocationTypeId", endLocationTypeId == null ? 0 : (int)endLocationTypeId);
                    cmd.Parameters.AddWithValue("@endClientLocationId", endClientLocationId == null ? 0 : (int)endClientLocationId);

                    cmd.Parameters.AddWithValue("@endLat", endLat == null ? DBNull.Value : (object)endLat);
                    cmd.Parameters.AddWithValue("@endLon", endLon == null ? DBNull.Value : (object)endLon);

                    cmd.Parameters.AddWithValue("@date", date1);
                    
                    cmd.Parameters.AddWithValue("@utcIn", utcIn == null ? DBNull.Value : (object)utcIn);
                    cmd.Parameters.AddWithValue("@utcOut", utcOut == null ? DBNull.Value : (object)utcOut);
                    
                    cmd.Parameters.AddWithValue("@adjutcIn", adjutcIn == null ? DBNull.Value : (object)adjutcIn);
                    cmd.Parameters.AddWithValue("@adjutcOut", adjutcOut == null ? DBNull.Value : (object)adjutcOut);
                    
                    cmd.Parameters.AddWithValue("@inCallType", inCallType);
                    cmd.Parameters.AddWithValue("@outCallType", utcOut == null ? DBNull.Value : (object)outCallType);
                    cmd.Parameters.AddWithValue("@callType", callType);
                    cmd.Parameters.AddWithValue("@utcPartition1Start", utcPartition1Start == null ? DBNull.Value : (object)utcPartition1Start);
                    cmd.Parameters.AddWithValue("@utcPartition1End", utcPartition1End == null ? DBNull.Value : (object)utcPartition1End);
                    cmd.Parameters.AddWithValue("@utcPartition2Start", utcPartition2Start == null ? DBNull.Value : (object)utcPartition2Start);
                    cmd.Parameters.AddWithValue("@utcPartition2End", utcPartition2End == null ? DBNull.Value : (object)utcPartition2End);
                    cmd.Parameters.AddWithValue("@timeZone", timeZone);
                    cmd.Parameters.AddWithValue("@isEVV", isEVV);
                    cmd.Parameters.AddWithValue("@userPrId", userPrId);

                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }

            if (er.code == 0 && ds.Tables.Count != 0)
            {
                sessionId = Convert.ToInt32(ds.Tables[0].Rows[0].ItemArray[0]);

                if (ds.Tables.Count > 1)
                {
                    postProcessHCBSRecord(ref ds, ref er,  prId, serviceId, 0, svc, date1, date2);

                }
            }

            if (ds != null)
                ds.Dispose();
            return sessionId;
        }
        public int updateHCBSRecord2(ref Er er)
        {
            int sessionId = 0;
            DataSet ds = new DataSet();

            try
            {
                using (SqlConnection cn = new SqlConnection(conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ApiInOutHCBSEmpGet", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@HCBSHrsEmpId", HCBSEmpHrsId);
                    cmd.Parameters.AddWithValue("@prId", prId);
                    cmd.Parameters.AddWithValue("@serviceId", serviceId);
                    cmd.Parameters.AddWithValue("@utcPartition1Start", utcPartition1Start == null ? DBNull.Value : (object)utcPartition1Start);
                    cmd.Parameters.AddWithValue("@utcPartition1End", utcPartition1End == null ? DBNull.Value : (object)utcPartition1End);
                    cmd.Parameters.AddWithValue("@utcPartition2Start", utcPartition2Start == null ? DBNull.Value : (object)utcPartition2Start);
                    cmd.Parameters.AddWithValue("@utcPartition2End", utcPartition2End == null ? DBNull.Value : (object)utcPartition2End);
                    cmd.Parameters.AddWithValue("@timeZone", timeZone);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }

            if (er.code == 0 && ds.Tables.Count != 0)
            {
                sessionId = Convert.ToInt32(ds.Tables[0].Rows[0].ItemArray[0]);
                if (ds.Tables.Count > 1)
                    postProcessHCBSRecord(ref ds, ref er, prId, serviceId, 0, svc, date1, date2);
            }

            if (ds != null)
                ds.Dispose();
            return sessionId;
        }
        public void deleteTherapyRecord(int clientSessionTherapyId, ref Er er)
        {
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection cn = new SqlConnection(conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ApiInOutTherapyDelete", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@clientSessionTherapyId", clientSessionTherapyId);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
                if (ds.Tables.Count != 0)
                {
                    er.code = 2;
                    er.msg = ds.Tables[0].Rows[0].ItemArray[0].ToString();

                }

            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }

        }

        public int deleteHCBSRecord(int HCBSHrsEmpId, string date, ref Er er)
        {
            int sessionId = 0;
            var TimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            DateTime dt = Convert.ToDateTime(date);
            var startDayLocal = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Unspecified);
            var startDayUTC = TimeZoneInfo.ConvertTimeToUtc(startDayLocal, TimeZone);
            DateTime utcPartition1Start = startDayUTC;
            DateTime utcPartition1End = utcPartition1Start.AddDays(1);
            string date1 = startDayLocal.ToShortDateString();
            string date2 = null;
            

            DataSet ds = new DataSet();

            try
            {
                using (SqlConnection cn = new SqlConnection(conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ApiInOutHCBSEmpDelete", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@HCBSHrsEmpId", HCBSHrsEmpId);
                    cmd.Parameters.AddWithValue("@utcPartition1Start", utcPartition1Start);
                    cmd.Parameters.AddWithValue("@utcPartition1End", utcPartition1End);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }

            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }

            if (er.code == 0 && ds.Tables.Count != 0)
            {
                DataRow dr = ds.Tables[0].Rows[0];
                sessionId = HCBSHrsEmpId;
                int prId = (int)dr["prId"];
                int serviceId = (int)dr["serviceId"];
                int clientServiceId = (int)dr["clientServiceId"];
                string svc = (string)dr["svc"];

                if (ds.Tables.Count > 1)
                {
                    postProcessHCBSRecord(ref ds, ref er, prId, serviceId, clientServiceId, svc, date1, date2);

                }
            }


            if (ds != null)
                ds.Dispose();

            return sessionId;

        }

        public void checkHCBSRecordIsValid(ref Er er){

            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection cn = new SqlConnection(conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ApiInOutHCBSCheckValid", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@HCBSHrsEmpId", HCBSEmpHrsId);
                    cmd.Parameters.AddWithValue("@timeZone", timeZone);
                    cmd.Parameters.AddWithValue("@prId", prId);
                    cmd.Parameters.AddWithValue("@clientServiceId", clsvidId);
                    cmd.Parameters.AddWithValue("@serviceId", serviceId);
                    cmd.Parameters.AddWithValue("@dt", date1);
                    cmd.Parameters.AddWithValue("@svc", svc);
                    cmd.Parameters.AddWithValue("@utcIn", adjutcIn == null ? (DateTime)utcIn : (DateTime)adjutcIn);
                    cmd.Parameters.AddWithValue("@utcOut", adjutcOut == null ? (DateTime)utcOut : (DateTime)adjutcOut);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message;
            }
            if (er.code != 1)
            {
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count != 0)
                {
                    er.code = 1;
                    er.msg = Convert.ToString(ds.Tables[0].Rows[0].ItemArray[0]);
                }
            }
            ds.Dispose();

        }
        public void checkHCBSRecordAgainstAuths(ref Er er)
        {

           
                DataSet ds = new DataSet();
                try
                {
                    using (SqlConnection cn = new SqlConnection(conStr))
                    {
                        SqlCommand cmd = new SqlCommand("sp_ApiInOutHCBSCheckOverBilling", cn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@HCBSHrsEmpId", HCBSEmpHrsId);

                        cmd.Parameters.AddWithValue("@clientServiceId", clsvidId);
                        cmd.Parameters.AddWithValue("@serviceId", serviceId);
                        cmd.Parameters.AddWithValue("@date", date1);

                        cmd.Parameters.AddWithValue("@newUnits", calulateUnits15MinRule(adjutcOut == null ? (DateTime)utcOut : (DateTime)adjutcOut, adjutcIn == null ? (DateTime)utcIn : (DateTime)adjutcIn));
                        sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                    }
                }
                catch (Exception ex)
                {
                    er.code = 1;
                    er.msg = ex.Message.Replace(". ","<br/>"); 
                }

                if (ds.Tables.Count != 0)
                {
                    er.code = Convert.ToInt32(ds.Tables[0].Rows[0].ItemArray[0]);
                    er.msg = Convert.ToString(ds.Tables[0].Rows[0].ItemArray[1]);
                }
                ds.Dispose();
          

        }

        public void checkTherapyRecordIsValid(ref Er er)
        {

            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection cn = new SqlConnection(conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ApiInOutTherapyCheckValid", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@HCBSHrsEmpId", HCBSEmpHrsId);
                    cmd.Parameters.AddWithValue("@timeZone", timeZone);
                    cmd.Parameters.AddWithValue("@prId", prId);
                    cmd.Parameters.AddWithValue("@clientServiceId", clsvidId);
                    cmd.Parameters.AddWithValue("@serviceId", serviceId);
                    cmd.Parameters.AddWithValue("@dt", date1);
                    cmd.Parameters.AddWithValue("@svc", svc);
                    cmd.Parameters.AddWithValue("@utcIn", adjutcIn == null ? (DateTime)utcIn : (DateTime)adjutcIn);
                    cmd.Parameters.AddWithValue("@utcOut", adjutcOut == null ? (DateTime)utcOut : (DateTime)adjutcOut);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
            }
            catch (Exception ex)
            {
                er.code = 1;
                er.msg = ex.Message.Replace(". ", "<br/>");
            }
            if (er.code != 1)
            {
                if (ds.Tables.Count != 0 && ds.Tables[0].Rows.Count != 0)
                {
                    er.code = 1;
                    er.msg = Convert.ToString(ds.Tables[0].Rows[0].ItemArray[0]);
                }
            }
            ds.Dispose();

        }


        private void postProcessHCBSRecord(ref DataSet ds, ref Er er, int PROVIDERID, int MASTERSERVICEID, int MASTERCLIENTSERVICEID, string MASTERSERVICE, string date1, string date2)
        {


            // create pivot table so we can calc actual client sessions
            DataTable PivotTable = new DataTable();
            PivotTable.Columns.Add("HCBSHrsEmpID", typeof(Int32)); //ZZZZZ
            PivotTable.Columns.Add("prId", typeof(Int32));
            PivotTable.Columns.Add("clsvId", typeof(Int32));
            PivotTable.Columns.Add("clsvidId", typeof(Int32));
            PivotTable.Columns.Add("serviceId", typeof(Int32));
            PivotTable.Columns.Add("svc", typeof(string));
            PivotTable.Columns.Add("callType", typeof(Int32));
            PivotTable.Columns.Add("TimeType", typeof(Int32));
            PivotTable.Columns.Add("dt", typeof(DateTime));
            PivotTable.Columns.Add("TimeStamp", typeof(DateTime));
            PivotTable.Columns.Add("Lat", typeof(decimal));
            PivotTable.Columns.Add("Lon", typeof(decimal));
            PivotTable.Columns.Add("ClientLocationId", typeof(int));
            PivotTable.Columns.Add("LocationTypeId", typeof(int));



            // Create New Table to hold break up the sessions
            DataTable ClientSessionTable = new DataTable();
            ClientSessionTable.Columns.Add("HCBSHrsEmpID", typeof(Int32)); //ZZZZZ
            ClientSessionTable.Columns.Add("prId", typeof(Int32));
            ClientSessionTable.Columns.Add("dt", typeof(DateTime));
            ClientSessionTable.Columns.Add("clsvId", typeof(Int32));
            ClientSessionTable.Columns.Add("clsvidId", typeof(Int32));
            ClientSessionTable.Columns.Add("serviceId", typeof(Int32));
            ClientSessionTable.Columns.Add("svc", typeof(string));
            ClientSessionTable.Columns.Add("utcIn", typeof(DateTime));
            ClientSessionTable.Columns.Add("utcOut", typeof(DateTime));

            ClientSessionTable.Columns.Add("startLat", typeof(decimal));
            ClientSessionTable.Columns.Add("startLon", typeof(decimal));
            ClientSessionTable.Columns.Add("endLat", typeof(decimal));
            ClientSessionTable.Columns.Add("endLon", typeof(decimal));
            ClientSessionTable.Columns.Add("ratio", typeof(Int32));
            ClientSessionTable.Columns.Add("units", typeof(decimal));

            ClientSessionTable.Columns.Add("StartClientLocationId", typeof(int));
            ClientSessionTable.Columns.Add("StartLocationTypeId", typeof(int));
            ClientSessionTable.Columns.Add("EndClientLocationId", typeof(int));
            ClientSessionTable.Columns.Add("EndLocationTypeId", typeof(int));
            ClientSessionTable.Columns.Add("ClientLocationId", typeof(int));
            ClientSessionTable.Columns.Add("LocationTypeId", typeof(int));


            // create billing table to hold records
            DataTable BillingTable = new DataTable();
            BillingTable.Columns.Add("prId", typeof(int));
            BillingTable.Columns.Add("dt", typeof(DateTime));
            BillingTable.Columns.Add("clsvId", typeof(Int32));
            BillingTable.Columns.Add("clsvidId", typeof(Int32));
            BillingTable.Columns.Add("serviceId", typeof(Int32));
            BillingTable.Columns.Add("svc", typeof(string));
            BillingTable.Columns.Add("ratio", typeof(Byte));
            BillingTable.Columns.Add("timeOfDayModifier", typeof(Int32));
            BillingTable.Columns.Add("units", typeof(decimal));
            BillingTable.Columns.Add("onHold", typeof(bool));
            BillingTable.Columns.Add("status", typeof(Int32));
            BillingTable.Columns.Add("billAsGroup", typeof(int));
            BillingTable.Columns.Add("billAsGroupRatio", typeof(byte));


            DataView ClientSessionDataView = null;
            DataView SessionRecords = null;
            DataTable ClientServiceListTable = null;
            DataView PivotDataView = null;
            DataView UniqueBillingRecordKeys = null;
            DataTable UniqueClientServices = null;
            DataTable HCBSHrsClientIsDailyUpdate = null;
            try
            {

                SessionRecords = new DataView(ds.Tables[1]);
                 SessionRecords.RowFilter = "ActualUtcIn<>ActualUtcOut";

                SessionRecords.Sort = "actualutcIn ASC, actualutcOut ASC";
                // need ta list of services to get the all the records for those services
                ClientServiceListTable = SessionRecords.ToTable(true, "clsvidId", "prId", "dt");
                


                // this will happen when we delete a record
                if (ClientServiceListTable.Rows.Count == 0 && MASTERCLIENTSERVICEID != 0)
                {
                    var newRow = ClientServiceListTable.NewRow();
                    newRow["clsvidId"] = MASTERCLIENTSERVICEID;
                    newRow["prId"] = PROVIDERID;
                    newRow["dt"] = date1;
                    ClientServiceListTable.Rows.Add(newRow);
                }
               
                foreach (DataRowView drv in SessionRecords)
                {
                    var newRow = PivotTable.NewRow();
                    newRow["HCBSHrsEmpID"] = (int)drv["HCBSHrsEmpID"];  //ZZZZZ
                    newRow["prId"] = (int)drv["prId"];
                    newRow["clsvId"] = (int)drv["clsvId"];
                    newRow["clsvidId"] = (int)drv["clsvidId"];
                    newRow["serviceId"] = (int)drv["serviceId"];
                    newRow["svc"] = (string)drv["svc"];
                    newRow["TimeType"] = STARTTIME;
                    newRow["dt"] = (DateTime)drv["dt"];
                    newRow["TimeStamp"] = (DateTime)drv["ActualutcIn"];
                    newRow["Lat"] = (decimal)drv["startLat"];
                    newRow["Lon"] = (decimal)drv["startLon"];
                    newRow["ClientLocationId"] = Convert.ToInt32(drv["StartClientLocationId"]);
                    newRow["LocationTypeId"] = Convert.ToInt32(drv["StartLocationTypeId"]);
                    PivotTable.Rows.Add(newRow);
                    newRow = PivotTable.NewRow();
                    newRow["HCBSHrsEmpID"] = (int)drv["HCBSHrsEmpID"];
                    newRow["prId"] = (int)drv["prId"];
                    newRow["clsvId"] = (int)drv["clsvId"];
                    newRow["clsvidId"] = (int)drv["clsvidId"];
                    newRow["serviceId"] = (int)drv["serviceId"];
                    newRow["svc"] = (string)drv["svc"];
                    newRow["TimeType"] = ENDTIME;
                    newRow["dt"] = (DateTime)drv["dt"];
                    newRow["TimeStamp"] = drv["ActualutcOut"] != DBNull.Value ? (DateTime)drv["ActualutcOut"] : (DateTime)drv["ActualutcIn"];
                    newRow["Lat"] = drv["endLat"];
                    newRow["Lon"] = drv["endLon"];
                    newRow["ClientLocationId"] = Convert.ToInt32(drv["EndClientLocationId"]);
                    newRow["LocationTypeId"] = Convert.ToInt32(drv["EndLocationTypeId"]);

                    PivotTable.Rows.Add(newRow);
                }

                // SessionRecords.Dispose();
                PivotDataView = new DataView(PivotTable);
                PivotDataView.Sort = "dt ASC, TimeStamp ASC,TimeType DESC";

                DateTime? PreviousStartTime = null;


                // break up sessions for each client and get the ratio
                foreach (DataRowView drv in PivotDataView)
                {
                    if ((int)drv["TimeType"] == STARTTIME)
                    {
                        DataRow newRow;
                        DataRow[] OpenSessions = ClientSessionTable.Select("utcOut IS NULL");
                        int ratio = OpenSessions.Count() + 1;


                        // Add new session moved



                        // if the start time does not equal previous start time close all sessions still open
                        // and create new sessions ZZZZ
                        if (PreviousStartTime != (DateTime)drv["TimeStamp"])
                        {
                            foreach (DataRow dr in ClientSessionTable.Rows)
                            {
                                if (dr["utcOut"] == DBNull.Value)
                                {
                                    dr["utcOut"] = (DateTime)drv["TimeStamp"];
                                    dr["endLat"] = (decimal)drv["lat"];
                                    dr["endLon"] = (decimal)drv["lon"];
                                    dr["EndClientLocationId"] = Convert.ToInt32(drv["ClientLocationId"]);
                                    dr["EndLocationTypeId"] = Convert.ToInt32(drv["LocationTypeId"]);
                                    // all locations OK set default location to end Location
                                    if ((int)dr["startClientLocationId"] != 0 && (int)dr["EndClientLocationId"] != 0)
                                        dr["ClientLocationId"] = (int)dr["EndClientLocationId"];
                                    if ((int)dr["startLocationTypeId"] != 0 && (int)dr["EndLocationTypeId"] != 0)
                                        dr["LocationTypeId"] = (int)dr["EndLocationTypeId"];

                                    dr["units"] = calulateUnits15MinRule((DateTime)dr["utcOut"], (DateTime)dr["utcIn"]);
                                }
                            }
                            // create new sessions from the ones we closed
                            foreach (DataRow dr in OpenSessions)
                            {
                                newRow = ClientSessionTable.NewRow();
                                newRow["HCBSHrsEmpID"] = (int)dr["HCBSHrsEmpID"];  //ZZZZZ

                                newRow["prId"] = (int)dr["prId"];
                                newRow["dt"] = (DateTime)dr["dt"];
                                newRow["clsvId"] = (int)dr["clsvId"];
                                newRow["clsvidId"] = (int)dr["clsvidId"];
                                newRow["serviceId"] = (int)dr["serviceId"];
                                newRow["svc"] = (string)dr["svc"];
                                newRow["ratio"] = ratio;
                                newRow["utcIn"] = (DateTime)drv["TimeStamp"];
                                newRow["startlat"] = (decimal)drv["lat"];
                                newRow["startLon"] = (decimal)drv["lon"];
                                newRow["StartClientLocationId"] = Convert.ToInt32(drv["ClientLocationId"]);
                                newRow["StartLocationTypeId"] = Convert.ToInt32(drv["LocationTypeId"]);
                                newRow["ClientLocationId"] = 0;
                                newRow["LocationTypeId"] = 0;
                                ClientSessionTable.Rows.Add(newRow);
                            }

                        }
                        // else just adjust the ratio ZZZZ
                        else
                        {
                            foreach (DataRow dr in ClientSessionTable.Rows)
                            {
                                if (dr["utcOut"] == DBNull.Value)
                                    dr["ratio"] = ratio;
                            }

                        }





                        // Add new session moved here
                        newRow = ClientSessionTable.NewRow();
                        newRow["HCBSHrsEmpID"] = (int)drv["HCBSHrsEmpID"];  //ZZZZZ
                        newRow["prId"] = (int)drv["prId"];
                        newRow["dt"] = (DateTime)drv["dt"];
                        newRow["clsvId"] = (int)drv["clsvId"];
                        newRow["clsvidId"] = (int)drv["clsvidId"];
                        newRow["serviceId"] = (int)drv["serviceId"];
                        newRow["svc"] = (string)drv["svc"];
                        newRow["ratio"] = ratio;

                        newRow["utcIn"] = (DateTime)drv["TimeStamp"];
                        newRow["startlat"] = (decimal)drv["lat"];
                        newRow["startLon"] = (decimal)drv["lon"];
                        newRow["StartClientLocationId"] = Convert.ToInt32(drv["ClientLocationId"]);
                        newRow["StartLocationTypeId"] = Convert.ToInt32(drv["LocationTypeId"]);
                        newRow["ClientLocationId"] = 0;
                        newRow["LocationTypeId"] = 0;
                        ClientSessionTable.Rows.Add(newRow);

                        PreviousStartTime = (DateTime)drv["TimeStamp"];

                    }
                    else  // this is an end time
                    {
                        // close current session
                        foreach (DataRow dr in ClientSessionTable.Rows)
                        {
                            if ((int)dr["clsvidId"] == (int)drv["clsvidId"] && dr["utcOut"] == DBNull.Value)
                            {
                                dr["utcOut"] = (DateTime)drv["TimeStamp"];
                                dr["endLat"] = (decimal)drv["lat"];
                                dr["endLon"] = (decimal)drv["lon"];
                                dr["EndClientLocationId"] = Convert.ToInt32(drv["ClientLocationId"]);
                                dr["EndLocationTypeId"] = Convert.ToInt32(drv["LocationTypeId"]);
                                // all locations OK set default location to end Location
                                if ((int)dr["startClientLocationId"] != 0 && (int)dr["EndClientLocationId"] != 0)
                                    dr["ClientLocationId"] = (int)dr["EndClientLocationId"];
                                if ((int)dr["startLocationTypeId"] != 0 && (int)dr["EndLocationTypeId"] != 0)
                                    dr["LocationTypeId"] = (int)dr["EndLocationTypeId"];

                                dr["units"] = calulateUnits15MinRule((DateTime)dr["utcOut"], (DateTime)dr["utcIn"]);
                                // calculate units
                            }
                        }
                        // get remain sessions
                        DataRow[] OpenSessions = ClientSessionTable.Select("utcOut IS NULL");
                        // close the remaining sessions
                        foreach (DataRow dr in ClientSessionTable.Rows)
                        {

                            if (dr["utcOut"] == DBNull.Value)
                            {
                                dr["utcOut"] = (DateTime)drv["TimeStamp"];
                                dr["endLat"] = (decimal)drv["lat"];
                                dr["endLon"] = (decimal)drv["lon"];
                                dr["EndClientLocationId"] = Convert.ToInt32(drv["ClientLocationId"]);
                                dr["EndLocationTypeId"] = Convert.ToInt32(drv["LocationTypeId"]);
                                // all locations OK set default location to end Location
                                if ((int)dr["startClientLocationId"] != 0 && (int)dr["EndClientLocationId"] != 0)
                                    dr["ClientLocationId"] = (int)dr["EndClientLocationId"];
                                if ((int)dr["startLocationTypeId"] != 0 && (int)dr["EndLocationTypeId"] != 0)
                                    dr["LocationTypeId"] = (int)dr["EndLocationTypeId"];

                                dr["units"] = calulateUnits15MinRule((DateTime)dr["utcOut"], (DateTime)dr["utcIn"]);
                            }
                        }
                        // if any open session existed recreate them
                        foreach (DataRow dr in OpenSessions)
                        {
                            var newRow = ClientSessionTable.NewRow();
                            newRow["HCBSHrsEmpID"] = (int)dr["HCBSHrsEmpID"];  //ZZZZZ
                            newRow["prId"] = (int)dr["prId"];
                            newRow["dt"] = (DateTime)dr["dt"];
                            newRow["clsvId"] = (int)dr["clsvId"];
                            newRow["clsvidId"] = (int)dr["clsvidId"];
                            newRow["serviceId"] = (int)dr["serviceId"];
                            newRow["svc"] = (string)dr["svc"];
                            newRow["ratio"] = OpenSessions.Count();
                            newRow["utcIn"] = (DateTime)dr["utcOut"];

                            newRow["startlat"] = (decimal)dr["endlat"];
                            newRow["startLon"] = (decimal)dr["endLon"];
                            newRow["StartClientLocationId"] = Convert.ToInt32(dr["ClientLocationId"]);
                            newRow["StartLocationTypeId"] = Convert.ToInt32(dr["LocationTypeId"]);
                            newRow["ClientLocationId"] = 0;
                            newRow["LocationTypeId"] = 0;
                            ClientSessionTable.Rows.Add(newRow);
                        }
                    }
                }
                // cleanup noise
                for (int i = ClientSessionTable.Rows.Count - 1; i >= 0; i--)
                {
                    DataRow dr = ClientSessionTable.Rows[i];
                    if ((DateTime)dr["utcIn"] == (DateTime)dr["utcOut"])        // (decimal)dr["units"] == 0M
                        dr.Delete();
                }


                ClientSessionTable.AcceptChanges();





                ds = new DataSet();
                using (SqlConnection cn = new SqlConnection(conStr))
                {

                    SqlCommand cmd = new SqlCommand("sp_ApiInOutHCBSClientUpdate", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@providerId", PROVIDERID);
                    cmd.Parameters.AddWithValue("@date1", date1);
                    cmd.Parameters.AddWithValue("@date2", date2 == null ? DBNull.Value : (object)date2);
                    cmd.Parameters.AddWithValue("@serviceId", MASTERSERVICEID);
                    cmd.Parameters.AddWithValue("@svc", MASTERSERVICE);
                    cmd.Parameters.AddWithValue("@HCBSHrsClient", ClientSessionTable);
                    cmd.Parameters.AddWithValue("@ClientServiceIds", ClientServiceListTable);
                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }



                // Now we need to update the billing records
                ClientSessionDataView = new DataView(ds.Tables[0]);
                UniqueBillingRecordKeys = new DataView(ClientSessionDataView.ToTable(true, "prId", "dt", "clsvId", "clsvidId", "serviceId", "svc", "ratio"));
                UniqueClientServices = ClientSessionDataView.ToTable(true, "clsvidId", "dt");

                // We need to go back and update client sessions just in case some are daily for Sandata Api
                // so create
                HCBSHrsClientIsDailyUpdate = new DataTable();
                HCBSHrsClientIsDailyUpdate.Columns.Add("clsvidId", typeof(Int32));
                HCBSHrsClientIsDailyUpdate.Columns.Add("billAsDaily", typeof(bool));
                HCBSHrsClientIsDailyUpdate.Columns.Add("dt", typeof(DateTime));


                foreach (DataRow UniqueClientService in UniqueClientServices.Rows)
                {

                    // Add update row and initialize to false for sandata API - not xls billing file
                    DataRow newUpdateRow = HCBSHrsClientIsDailyUpdate.NewRow();
                    if (MASTERSERVICE == "RSP")
                    {
                        // rsp is the only service that can go to daily
                        newUpdateRow["clsvidId"] = (int)UniqueClientService["clsvidId"];
                        newUpdateRow["billAsDaily"] = false;
                        newUpdateRow["dt"] = (DateTime)UniqueClientService["dt"];
                        HCBSHrsClientIsDailyUpdate.Rows.Add(newUpdateRow);
                    }


                    int TimeOfDayModifier = 0;
                    decimal TotalUnitsForService = 0; // For Calculating if RSP => RSD
                    byte HighestRatio = 0;             // If we have RSP with different ratios & RSP=>RSD we need this

                    UniqueBillingRecordKeys.RowFilter = "clsvidId=" + UniqueClientService["clsvidId"] + " AND dt='" + UniqueClientService["dt"] + "'";


                    // provider/clientservice/ratio
                    foreach (DataRowView UniqueBillingRecordKeyRow in UniqueBillingRecordKeys)
                    {
                        DataRow newRow = BillingTable.NewRow();
                        newRow["prId"] = (int)UniqueBillingRecordKeyRow["prId"];
                        newRow["dt"] = (DateTime)UniqueBillingRecordKeyRow["dt"];
                        newRow["clsvId"] = (int)UniqueBillingRecordKeyRow["clsvId"];
                        newRow["clsvidId"] = (int)UniqueBillingRecordKeyRow["clsvidId"];
                        newRow["serviceId"] = (int)UniqueBillingRecordKeyRow["serviceId"];
                        newRow["svc"] = (string)UniqueBillingRecordKeyRow["svc"];
                        newRow["ratio"] = Convert.ToByte(UniqueBillingRecordKeyRow["ratio"]);
                        if ((byte)newRow["Ratio"] > HighestRatio)
                            HighestRatio = (byte)newRow["Ratio"];
                        newRow["timeOfDayModifier"] = TimeOfDayModifier;
                        newRow["status"] = 0;
                        newRow["units"] = 0M;
                        newRow["onHold"] = false;
                        newRow["status"] = 0;


                        foreach (DataRowView ClientSessionRow in ClientSessionDataView)
                        {

                            if ((DateTime)ClientSessionRow["dt"] == (DateTime)newRow["dt"] && (int)ClientSessionRow["prId"] == (int)newRow["prId"] && (int)ClientSessionRow["clsvidId"] == (int)newRow["clsvidId"] && (byte)ClientSessionRow["ratio"] == (byte)newRow["ratio"])
                            {
                                TotalUnitsForService += (decimal)ClientSessionRow["units"];

                                newRow["units"] = (decimal)newRow["units"] + (decimal)ClientSessionRow["units"]; ;
                                newRow["onHold"] = false;
                            }
                        }
                        BillingTable.Rows.Add(newRow);
                        TimeOfDayModifier++;
                    }


                    // for the client service in question
                    if (MASTERSERVICE == "RSP" && TotalUnitsForService > 11.75M)
                    {

                        // RSP turns into RSD at > 11.75
                        int BillAsGroup = 0;
                        var defaultSelectedRow = ds.Tables[0].AsEnumerable().Where(x => x.Field<int>("clsvidId") == (int)UniqueClientService["clsvidId"] && x.Field<DateTime>("dt") == (DateTime)UniqueClientService["dt"]).OrderBy(y => y.Field<int>("HCBSHrsClientId")).FirstOrDefault();///zxxxxxxx

                        // update billing records for DDD file
                        foreach (DataRow BillingRecord in BillingTable.Rows)
                        {
                            if ((DateTime)BillingRecord["dt"] == (DateTime)UniqueClientService["dt"] && (int)BillingRecord["clsvidId"] == (int)UniqueClientService["clsvidId"])
                            {
                                // we need some type of id
                     ///zzzzzzz           var defaultSelectedRow = ds.Tables[0].AsEnumerable().Where(x => x.Field<int>("clsvidId") == (int)BillingRecord["clsvidId"]).OrderBy(y => y.Field<int>("HCBSHrsClientId")).FirstOrDefault();
                                BillAsGroup = (int)defaultSelectedRow["HCBSHrsClientId"];
                            }
                            BillingRecord["billAsGroup"] = BillAsGroup;
                            BillingRecord["billAsGroupRatio"] = HighestRatio;
                        }
                        // We need to update the actual client sessions for sandata
                        newUpdateRow["billAsDaily"] = true;


                    }
                }

                //     UniqueClientServices.Dispose();
                //      UniqueBillingRecordKeys.Dispose();





                ds = new DataSet();
                using (SqlConnection cn = new SqlConnection(conStr))
                {
                    SqlCommand cmd = new SqlCommand("sp_ApiInOutHCBSBillUpdateNew", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@date1", date1);
                    cmd.Parameters.AddWithValue("@date2", date2 == null ? DBNull.Value : (object)date2);
                    cmd.Parameters.AddWithValue("@ClientServiceIds", ClientServiceListTable);
                    cmd.Parameters.AddWithValue("@HCBSHrsBill", BillingTable);
                    cmd.Parameters.AddWithValue("@HCBSHrsClientIsDailyUpdate", HCBSHrsClientIsDailyUpdate);

                    sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                }
                //   HCBSHrsClientIsDailyUpdate.Dispose();


            }
            catch (Exception ex)
            {
                er.msg = ex.Message;
                er.code = 1;
            }



            PivotTable.Dispose();
            ClientSessionTable.Dispose();
            BillingTable.Dispose();

            if (SessionRecords != null)
                SessionRecords.Dispose();
            if (ClientServiceListTable != null)
                ClientServiceListTable.Dispose();
            if (PivotDataView != null)
                PivotDataView.Dispose();
            if (UniqueBillingRecordKeys != null)
                UniqueBillingRecordKeys.Dispose();
            if (UniqueClientServices != null)
                UniqueClientServices.Dispose();
            if (HCBSHrsClientIsDailyUpdate != null)
                HCBSHrsClientIsDailyUpdate.Dispose();
            if (ClientSessionDataView != null)
                ClientSessionDataView.Dispose();





        }



        private decimal calulateUnits15MinRule(DateTime End, DateTime Start)
        {

            TimeSpan timespan = (End - Start);
        decimal units = Convert.ToDecimal(timespan.Hours);
        double minutes = timespan.Minutes;
        if (minutes >= 53)
            units += 1;
        else if (minutes >= 38)
            units += 0.75M;
        else if (minutes >= 23)
            units += 0.5M;
        else if (minutes >= 8)
            units += 0.25M;

        return units;

    }



    }
}