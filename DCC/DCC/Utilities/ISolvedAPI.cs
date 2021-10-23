using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

namespace DCC
{
    public class ISolvedClient
    {
        public int id;
        public string clientName;
    }
    public class ImportType
    {
        public int id;
        public string importTypeName;
    }
    public class ClientListResponse
    {
        public ISolvedClient[] results;
        public string totalItems;
    }
    public class ISolvedPayrollStatus
    {
        public int id;
        public Boolean canCommit;
        public string status;
    }
    public class AccessTokenResponse
    {
        public string access_token;
    }
    public class PayGroup
    {
        public int id;
        public string payGroupName;
    }
    public class LegalResponse
    {
        public PayGroup[] payGroups;
    }
    public static class ISolvedAPI
    {
        private static string apiUrl = "https://www.myisolved.com/rest/api/";
        private static string basicAuth64 = "MjYyZmJjOGI3YjBiNDVjZWJmY2MzYTkxMTQxNTk5MDM6MXFkQjE1R2llTk9SaUd3NTdCK1lraEM1dmRETTZabkRzdjlGV2YxWjFoQWN4N1owRmp0VGpzT3JVRW1lV0RRY3puRU5NcXVrSVJPdEVoYmpIbnNHdGc9PQ==";
        private static int clientId = 11823;
        private static string accessToken;
        
        public static String GetPayRollCode(String payRollCode)
        {
            if (payRollCode == "EHA1")
            {
                return "EHAH1";
            } else if (payRollCode == "EHA2")
            {
                return "EHAH2";
            } else if (payRollCode == "EHA3")
            {
                return "EHAH3";
            } else if (payRollCode == "ESUP")
            {
                return "ESPV";
            }
            return payRollCode;
        }
        private static HttpClient HeadersForAccessTokenGenerate()
        {
            HttpClientHandler handler = new HttpClientHandler() { UseDefaultCredentials = false };
            HttpClient client = new HttpClient(handler);
            try
            {
                client.BaseAddress = new Uri(apiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                client.DefaultRequestHeaders.Add("Authorization", "Basic " + basicAuth64);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return client;
        }
        private async static Task GenerateAccessToken()
        {
            AccessTokenResponse token;

            try
            {
                HttpClient client = HeadersForAccessTokenGenerate();
                string body = "grant_type=client_credentials";
                client.BaseAddress = new Uri(apiUrl);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress);
                request.Content = new StringContent(body,
                                                    Encoding.UTF8,
                                                    "application/x-www-form-urlencoded");//CONTENT-TYPE header  

                List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();

                postData.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));

                request.Content = new FormUrlEncodedContent(postData);
                HttpResponseMessage tokenResponse = client.PostAsync("token", new FormUrlEncodedContent(postData)).Result;

                //var token = tokenResponse.Content.ReadAsStringAsync().Result;    
                token = await tokenResponse.Content.ReadAsAsync<AccessTokenResponse>(new[] { new JsonMediaTypeFormatter() });
            }


            catch (HttpRequestException ex)
            {
                throw ex;
            }
            accessToken = token != null ? token.access_token : null;
        }
        private async static Task<HttpClient> Method_Headers()
        {
            await GenerateAccessToken();
            HttpClientHandler handler = new HttpClientHandler() { UseDefaultCredentials = false };
            HttpClient client = new HttpClient(handler);

            try
            {
                client.BaseAddress = new Uri(apiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return client;
        }

        public async static Task<ClientListResponse> getClientList()
        {
            ClientListResponse clientList = null;
            try
            {
                HttpClient client = await Method_Headers();
                HttpResponseMessage tokenResponse = await client.GetAsync(Uri.EscapeUriString(client.BaseAddress.ToString() + "clients"));
                if (tokenResponse.IsSuccessStatusCode)
                {
                    clientList = tokenResponse.Content.ReadAsAsync<ClientListResponse>(new[] { new JsonMediaTypeFormatter() }).Result;
                }
            }
            catch (HttpRequestException ex)
            {
                throw ex;
            }
            return clientList;
        }
        public async static Task<int> getPaygroupId(int companyId)
        {
            LegalResponse legal = null;
            try
            {
                HttpClient client = await Method_Headers();
                HttpResponseMessage tokenResponse = await client.GetAsync(Uri.EscapeUriString(client.BaseAddress.ToString() + "clients/" + clientId.ToString() + "/legals/" + companyId.ToString()));
                if (tokenResponse.IsSuccessStatusCode)
                {
                    legal = tokenResponse.Content.ReadAsAsync<LegalResponse>(new[] { new JsonMediaTypeFormatter() }).Result;
                }
            }
            catch (HttpRequestException ex)
            {
                throw ex;
            }
            if (legal.payGroups[0].payGroupName == "BW")
                return legal.payGroups[0].id;
            return 0;
        }

        public async static Task<ISolvedPayrollStatus> getStatus(int id)
        {
            ISolvedPayrollStatus imported;
            try
            {
                HttpClient client = await Method_Headers();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress);
                HttpResponseMessage response = client.GetAsync("clients/" + clientId + "/imports/" + id).Result;

                //var token = tokenResponse.Content.ReadAsStringAsync().Result;    
                imported = await response.Content.ReadAsAsync<ISolvedPayrollStatus>(new[] { new JsonMediaTypeFormatter() });
            }
            catch (HttpRequestException ex)
            {
                throw ex;
            }
            return imported;
        }
        public async static Task<ISolvedPayrollStatus> initiateImport(int companyId, string data)
        {
            ISolvedPayrollStatus imported;

            // TODO: should be updated
            int importTypeId = 6;
            int payGroupId = await getPaygroupId(companyId);
            try
            {
                HttpClient client = await Method_Headers();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress);
                List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();

                postData.Add(new KeyValuePair<string, string>("companyId", companyId.ToString()));
                postData.Add(new KeyValuePair<string, string>("data", data));
                postData.Add(new KeyValuePair<string, string>("format", "csv"));
                postData.Add(new KeyValuePair<string, string>("importKey", "EmpNo"));
                postData.Add(new KeyValuePair<string, string>("importTypeId", importTypeId.ToString()));
                postData.Add(new KeyValuePair<string, string>("payGroupId", payGroupId.ToString()));
                postData.Add(new KeyValuePair<string, string>("templateName", "default"));

                request.Content = new FormUrlEncodedContent(postData);
                HttpResponseMessage response = client.PostAsync("clients/" + clientId + "/imports", new FormUrlEncodedContent(postData)).Result;

                //var token = tokenResponse.Content.ReadAsStringAsync().Result;    
                imported = await response.Content.ReadAsAsync<ISolvedPayrollStatus>(new[] { new JsonMediaTypeFormatter() });
            }


            catch (HttpRequestException ex)
            {
                throw ex;
            }
            return imported;
        }

        public async static Task<ISolvedPayrollStatus> submit(int id)
        {
            ISolvedPayrollStatus imported;

            try
            {
                HttpClient client = await Method_Headers();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress);
                List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();
                request.Content = new FormUrlEncodedContent(postData);
                String path = "clients/" + clientId + "/imports/" + id.ToString() + "/commit";
                HttpResponseMessage response = client.PostAsync(path, new FormUrlEncodedContent(postData)).Result;

                //var token = tokenResponse.Content.ReadAsStringAsync().Result;
                imported = await response.Content.ReadAsAsync<ISolvedPayrollStatus>(new[] { new JsonMediaTypeFormatter() });
            }


            catch (HttpRequestException ex)
            {
                throw ex;
            }
            return imported;
        }
    }
}