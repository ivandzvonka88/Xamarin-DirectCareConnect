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
    public class QBSkilledClient
    {
        private readonly SQLHelper sqlHelper;
        Dictionary<string, string> StateList;

        //   const int QBRecordCountMax = 5000;
        const int QBRecordCountMax = 10000;  // for debug
        public QBData qbData;


        List<int> SkilledClientFields1 = new List<int>() { // AA/Achievement
        2,  // date modiied
        3, // QBRecordId
        7, // assist id
        8,  // company field
        9, // DOB
        
        87, // first name
        88, // Last Name

        49, // clientTYpe

         11, // street 1
        12, // street 2
        13, // city
        14, // state
        15, // zip
        191 // medicaid Id
        };

        List<int> SkilledClientFields2 = new List<int>() { // Theracare Heartland
        2,  // date modiied
        3, // QBRecordId
        };



        public QBSkilledClient()
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


        public async Task<QBError> QBProcessSkilledClient(QuickBaseCompanyInfo Q, string QBRecordId)
        {
            QBError er = new QBError();
            HttpStatusCode statusCode;
            string json;
            // create query for records
            QBQuery query = new QBQuery();
            query.from = Q.QBClientSkilledTbl;
            if (Q.QBCompanySkilledAppType == 1)
                query.select = SkilledClientFields1.ToArray();
            else
                query.select = SkilledClientFields2.ToArray();

            query.options = new Option { skip = 0, top = QBRecordCountMax };




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

                DataTable SkilledClientList = new DataTable();
                SkilledClientList.Columns.Add("QBRecordId", Type.GetType("System.Int32"));
                SkilledClientList.Columns.Add("QBDateModified");
                SkilledClientList.Columns.Add("clID");
                SkilledClientList.Columns.Add("medicaidID");
                SkilledClientList.Columns.Add("fn");
                SkilledClientList.Columns.Add("ln");
                SkilledClientList.Columns.Add("dob", Type.GetType("System.DateTime"));
                SkilledClientList.Columns.Add("Sex");
                SkilledClientList.Columns.Add("deleted");

                SkilledClientList.Columns.Add("physicianAgency");
                SkilledClientList.Columns.Add("physicianAddress");
                SkilledClientList.Columns.Add("physicianCity");

                SkilledClientList.Columns.Add("physicianState");
                SkilledClientList.Columns.Add("physicianZip");
                SkilledClientList.Columns.Add("physicianTelephone");
                SkilledClientList.Columns.Add("physicianEmail");
                SkilledClientList.Columns.Add("physicianNPI");

                SkilledClientList.Columns.Add("responsiblePersonLn");
                SkilledClientList.Columns.Add("responsiblePersonFn");
                SkilledClientList.Columns.Add("responsiblePersonRelationship");
                SkilledClientList.Columns.Add("responsiblePersonAddress");
                SkilledClientList.Columns.Add("responsiblePersonAddress2");
                SkilledClientList.Columns.Add("responsiblePersonCity");
                SkilledClientList.Columns.Add("responsiblePersonState");
                SkilledClientList.Columns.Add("responsiblePersonZip");
                SkilledClientList.Columns.Add("responsiblePersonTelephone");
                SkilledClientList.Columns.Add("responsiblePersonEmail");

                SkilledClientList.Columns.Add("relationshipId");
                SkilledClientList.Columns.Add("physicianFax");
                SkilledClientList.Columns.Add("physicianTitle");
                SkilledClientList.Columns.Add("physicianLastName");
                SkilledClientList.Columns.Add("physicianFirstName");
                SkilledClientList.Columns.Add("physicianMI");
                SkilledClientList.Columns.Add("physicianSuffix");

                if (Q.QBCompanySkilledAppType == 1)
                {
                    foreach (var dataItem in rss["data"])
                    {
                        DataRow newRow = SkilledClientList.NewRow();

                        newRow["QBRecordId"] = Convert.ToInt32(dataItem["3"]["value"]);
                        newRow["QBDateModified"] = Convert.ToDateTime(dataItem["2"]["value"]);

                        newRow["fn"] = dataItem["87"]["value"].ToString().Trim();
                        newRow["ln"] = dataItem["88"]["value"].ToString().Trim();

                        string companyField = dataItem["8"]["value"].ToString().Trim();

                        if (companyField.ToLower().IndexOf(Q.QBCompanyNameSkilled.ToLower()) != -1)
                            newRow["deleted"] = dataItem["49"]["value"].ToString().Trim() == "Active" ? false : true;
                        else
                            newRow["deleted"] = true;



                        newRow["clId"] = dataItem["7"]["value"].ToString().Trim();

                        newRow["medicaidId"] = dataItem["191"]["value"].ToString().Trim();


                      //  newRow["sex"] = dataItem["246"]["value"].ToString().Trim();

                        if (DateTime.TryParse(dataItem["9"]["value"].ToString(), out DateTime dateValue))
                            newRow["dob"] = Convert.ToDateTime(dateValue);

                        SkilledClientList.Rows.Add(newRow);
                    }
                }
                else
                {

                }
             
                DataSet ds = new DataSet();
                try
                {

                    // send to server
                    await Task.Run(() =>
                    {
                        using (SqlConnection cn = new SqlConnection(Q.companyConnection))
                        {
                            SqlCommand cmd = new SqlCommand("sp_QBSkilledClientsSet", cn)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmd.Parameters.AddWithValue("@coId", Q.coId);
                            cmd.Parameters.AddWithValue("@QBClients", SkilledClientList);
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
                    /*
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        string updateQuery =
                            "{" +
                                "\"to\": \"bp8r7bvam\"," +
                                "\"data\": [" +
                                    "{" +
                                    "\"3\": { \"value\": " + dr["QBRecordId"] + " }," +
                                       "\"87\": {\"value\": \"" + dr["fn"] + "\" }," +
                                        "\"88\": {\"value\": \"" + dr["ln"] + "\" }," +
                                    "\"7\": {\"value\": \"" + dr["clId"] + "\" }," +
                                    "\"191\": {\"value\": \"" + dr["medicaidId"] + "\"}" +
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
                  

                    */
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