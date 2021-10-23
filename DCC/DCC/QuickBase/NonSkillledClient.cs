using System;
using System.Configuration;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DCC.SQLHelpers.Helpers;
using DCC.Helpers;
using DCCHelper;
using DCC.Models.QuickBase;


namespace DCC.QuickBase
{

    public class QBNonSkilledClient
    {
        private readonly SQLHelper sqlHelper;
        Dictionary<string, string> StateList;

        //   const int QBRecordCountMax = 5000;
        const int QBRecordCountMax = 10000;  // for debug
        public QBData qbData;


        List<int> NonSkilledClientFields = new List<int>() {
        2,  // date modiied
        3, // QBRecordId
        
        6, // first name
        7, // Last Name
        43, // middle   
        
        8,  // dob

        10, // street 1
        11, // street 2
        12, // city
        13, // state
        14, // zip

        79, // assist id
        80, // medicaid Id
        84, // assistId length check 'True'

        88, // status 'Active'
        243, // status new 'Active'
        246, // gender Male/Female



        29 // Type




       

        };

        public QBNonSkilledClient()
        {
            sqlHelper = new SQLHelper();
            StateList = new Dictionary<string, string>();

            StateList.Add("Alaska", "AK");
            StateList.Add("Alabama", "AL");
            StateList.Add("Arkansas", "AR");
            StateList.Add("American Samoa", "AS");
            StateList.Add("Arizona", "AZ");
            StateList.Add("California", "CA");
            StateList.Add("Colorado", "CO");
            StateList.Add("Connecticut", "CT");
            StateList.Add("District of Columbia", "DC");
            StateList.Add("Deleware", "DE");
            StateList.Add("Federated States of Micronesia", "FM");
            StateList.Add("Florida", "FL");
            StateList.Add("Georgia", "GA");
            StateList.Add("Guam", "Gu");
            StateList.Add("Hawaii", "HI");
            StateList.Add("Iowa", "IA");
            StateList.Add("Idaho", "ID");
            StateList.Add("Illinois", "IL");
            StateList.Add("Indiana", "IN");
            StateList.Add("Kansas", "KS");
            StateList.Add("Kentucky", "KY");
            StateList.Add("Louisiana", "LA");
            StateList.Add("Massachusetts", "MA");
            StateList.Add("Maryland", "MD");
            StateList.Add("Maine", "ME");
            StateList.Add("Marshall Island", "MH");
            StateList.Add("Michigan", "MI");
            StateList.Add("Minnesota", "MN");
            StateList.Add("Missouri", "MO");
            StateList.Add("Northern Mariana Islands", "MP");
            StateList.Add("Mississippi", "MS");
            StateList.Add("Montana", "MT");
            StateList.Add("North Carolina", "NC");
            StateList.Add("North Dakota", "ND");
            StateList.Add("Nebraska", "NE");
            StateList.Add("Nevada", "NV");
            StateList.Add("New Hamphisre", "NH");
            StateList.Add("New Jersey", "NJ");
            StateList.Add("New Mexico", "NM");
            StateList.Add("New York", "NY");
            StateList.Add("Ohio", "OH");
            StateList.Add("Oklahoma", "OK");
            StateList.Add("Oregon", "OR");
            StateList.Add("Pennsylvania", "PA");
            StateList.Add("Puerto Rico", "PR");
            StateList.Add("Palau", "PW");
            StateList.Add("Rhode Island", "RI");
            StateList.Add("South Carolina", "SC");
            StateList.Add("South Dakota", "SD");
            StateList.Add("Tennessee", "TN");
            StateList.Add("Texas", "TX");
            StateList.Add("Utah", "Ut");
            StateList.Add("Virginia", "VA");
            StateList.Add("Virgin Islands", "VI");
            StateList.Add("Vermont", "VT");
            StateList.Add("Washington", "WA");
            StateList.Add("West Virginia", "WV");
        }


        public async Task<QBError> QBProcessNonSkilledClient(QuickBaseCompanyInfo Q, string QBRecordId)
        {
            QBError er = new QBError();
            HttpStatusCode statusCode;
            string json;
            // create query for records
            QBQuery query = new QBQuery();
            query.from = Q.QBClientNonskilledTbl;
            //    query.select = fieldList.ToArray();
            query.select = NonSkilledClientFields.ToArray();


           query.options = new Option { skip = 0, top = QBRecordCountMax };

       //     query.where = "({29.EX.'Active'}OR{29.EX.'Client'}OR{88.EX.'Active'})AND{84.EX.'True'}";
               query.where = "{84.EX.'True'}"; // has a DDD client ID



            if (QBRecordId != null)
            {
                if (query.where != null)
                    query.where += "AND{3.EX.'" + QBRecordId + "'}";
                else
                    query.where = "{3.EX.'" + QBRecordId + "'}";

            }
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("user-agent", "Therapy Corner");
                client.DefaultRequestHeaders.Add("QB-Realm-Hostname", Q.quickbaseDomain);
                client.DefaultRequestHeaders.Add("Authorization", "QB-USER-TOKEN " + Q.userToken);
                var content = new StringContent(JsonConvert.SerializeObject(query), Encoding.UTF8, "application/json");
                using (var result = await client.PostAsync("https://api.quickbase.com/v1/records/query", content))
                {
                    statusCode = result.StatusCode;
                    json = await result.Content.ReadAsStringAsync();
                }
            }
            if (statusCode == HttpStatusCode.OK)
            {
                JObject rss = JObject.Parse(json);
                // get field list
                qbData = JsonConvert.DeserializeObject<QBData>(json);

                DataTable NonSkilledClientList = new DataTable();
                NonSkilledClientList.Columns.Add("QBRecordId", Type.GetType("System.Int32"));
                NonSkilledClientList.Columns.Add("QBDateModified");
                NonSkilledClientList.Columns.Add("clID");
                NonSkilledClientList.Columns.Add("medicaidID");
                NonSkilledClientList.Columns.Add("fn");
                NonSkilledClientList.Columns.Add("ln");
                NonSkilledClientList.Columns.Add("dob", Type.GetType("System.DateTime"));
                NonSkilledClientList.Columns.Add("Sex");
                NonSkilledClientList.Columns.Add("deleted");

                NonSkilledClientList.Columns.Add("physicianAgency");
                NonSkilledClientList.Columns.Add("physicianAddress");
                NonSkilledClientList.Columns.Add("physicianCity");

                NonSkilledClientList.Columns.Add("physicianState");
                NonSkilledClientList.Columns.Add("physicianZip");
                NonSkilledClientList.Columns.Add("physicianTelephone");
                NonSkilledClientList.Columns.Add("physicianEmail");
                NonSkilledClientList.Columns.Add("physicianNPI");

                NonSkilledClientList.Columns.Add("responsiblePersonLn");
                NonSkilledClientList.Columns.Add("responsiblePersonFn");
                NonSkilledClientList.Columns.Add("responsiblePersonRelationship");
                NonSkilledClientList.Columns.Add("responsiblePersonAddress");
                NonSkilledClientList.Columns.Add("responsiblePersonAddress2");
                NonSkilledClientList.Columns.Add("responsiblePersonCity");
                NonSkilledClientList.Columns.Add("responsiblePersonState");
                NonSkilledClientList.Columns.Add("responsiblePersonZip");
                NonSkilledClientList.Columns.Add("responsiblePersonTelephone");
                NonSkilledClientList.Columns.Add("responsiblePersonEmail");

                NonSkilledClientList.Columns.Add("relationshipId");
                NonSkilledClientList.Columns.Add("physicianFax");
                NonSkilledClientList.Columns.Add("physicianTitle");
                NonSkilledClientList.Columns.Add("physicianLastName");
                NonSkilledClientList.Columns.Add("physicianFirstName");
                NonSkilledClientList.Columns.Add("physicianMI");
                NonSkilledClientList.Columns.Add("physicianSuffix");
                foreach (var dataItem in rss["data"])
                {
                    DataRow newRow = NonSkilledClientList.NewRow();

                    newRow["QBRecordId"] = Convert.ToInt32(dataItem["3"]["value"]);
                    newRow["QBDateModified"] = Convert.ToDateTime(dataItem["2"]["value"]);

                    newRow["fn"] = dataItem["6"]["value"].ToString().Trim();
                    newRow["ln"] = dataItem["7"]["value"].ToString().Trim();

                    if (dataItem["88"]["value"].ToString().Trim() != dataItem["243"]["value"].ToString().Trim())
                    {
                        var x = dataItem["29"]["value"].ToString().Trim();
                    }

                    /*
                    if (dataItem["29"]["value"].ToString().Trim() == "Active" || dataItem["29"]["value"].ToString().Trim() == "Client")
                        newRow["deleted"] = false;
                    else
                        newRow["deleted"] = true;
                    */
                    if (dataItem["88"]["value"].ToString().Trim() == "Active")
                        newRow["deleted"] = false;
                    else
                        newRow["deleted"] = true;

                    newRow["clId"] = dataItem["79"]["value"].ToString().Trim();
            
                    newRow["medicaidId"] = dataItem["80"]["value"].ToString().Trim();


                    newRow["sex"] = dataItem["246"]["value"].ToString().Trim();

                    if (DateTime.TryParse(dataItem["8"]["value"].ToString(), out DateTime dateValue))
                        newRow["dob"] = Convert.ToDateTime(dateValue);

                    NonSkilledClientList.Rows.Add(newRow);
                }
                DataSet ds = new DataSet();
                try
                {
                   
                    // send to server
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(Q.companyConnection))
                        {
                            SqlCommand cmd = new SqlCommand("sp_QBUnskilledClientsSet", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@coId", Q.coId);
                            cmd.Parameters.AddWithValue("@QBClients", NonSkilledClientList);
                            sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
                        }
                    });
                }
                catch (Exception ex)
                {
                    er.msg = ex.Message;
                    er.code = 1;
                }
                if (er.code == 0 && ds.Tables[0].Rows.Count != 0)
                {
                    
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        string updateQuery =
                            "{" +
                                "\"to\": \"bp8kqvgvy\"," +
                                "\"data\": [" +
                                    "{" +
                                    "\"3\": { \"value\": " + dr["QBRecordId"] + " }," +
                                    "\"6\": { \"value\": \"" + dr["fn"] + "\" }," +
                                    "\"7\": { \"value\": \"" + dr["ln"] + "\" }," +
                                    "\"79\": {\"value\": \"" + dr["clId"] + "\" }," +
                                    "\"80\": {\"value\": \"" + dr["medicaidId"] + "\"}" +
                                    "}" +
                                "]," +
                                "\"fieldsToReturn\": [3]" +
                            "}";
                        using (HttpClient client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Accept.Clear();
                            client.DefaultRequestHeaders.Add("user-agent", "Therapy Corner");
                            client.DefaultRequestHeaders.Add("QB-Realm-Hostname", Q.quickbaseDomain);
                            client.DefaultRequestHeaders.Add("Authorization", "QB-USER-TOKEN " + Q.userToken);
                            var content = new StringContent(updateQuery, Encoding.UTF8, "application/json");
                            using (var result = await client.PostAsync("https://api.quickbase.com/v1/records", content))
                            {
                                statusCode = result.StatusCode;
                                json = await result.Content.ReadAsStringAsync();
                            }
                        }
                        if (statusCode == HttpStatusCode.OK)
                        {
                        }
                    }
                    


                }



                ds.Dispose();
            }

            else
            {
                er.code = 100;
                er.msg = "HttpStatusCode " + statusCode + " Failed Http Call to Quickbase";
            }
            return er;
        }
    }
}