using aUI.Automation.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Web;

namespace aUI.Automation.HelperObjects
{
    public class Api : IDisposable
    {
        //constructor would create the primary object and any needed default headers + auth tokens
        HttpClient Client;
        TestExecutioner TE;
        string ApplicationType = "application/json";
        string RootEndpt = "";

        public Api(TestExecutioner te, string baseUrl = "", string appType = "application/json")
        {
            TE = te;
            ApplicationType = appType;
            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = Config.GetConfigSetting("ApiUrl", "");
            }

            Client = new();
            Client.BaseAddress = new Uri(baseUrl);
            Client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(appType));
        }

        public void SetAuthentication(string authKey, string type = "Bearer")
        {
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(type, authKey);
        }

        public void UpdateRequestType(string type)
        {
            //TOOD may need to remove other first
            //Client.DefaultRequestHeaders.Accept
            ApplicationType = type;
            Client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(type));
        }

        public void SetRootEndpt(string root)
        {
            if (root.StartsWith('/'))
            {
                RootEndpt = root;
            } else
            {
                RootEndpt = $"/{root}";
            }
        }

        //get
        public dynamic GetCall(Enum endpt, string query = "", int expectedCode = 200)
        {
            StartStep(endpt, "Get", expectedCode);
            var ept = $"{RootEndpt}{endpt.Api()}{query}";
            //Console.WriteLine("GET: "+ept);
            var rspMsg = Client.GetAsync(ept).Result;
            var a = rspMsg.Content.ReadAsStringAsync();
            AssertResult(expectedCode, rspMsg);

            return rspMsg.GetRsp();
        }

        //post
        public dynamic PostCall(Enum endpt, object body, string vars, int expectedCode = 200)
        {
            StartStep(endpt, "Post", expectedCode);
            var data = FormatBody(body);
            var rspMsg = Client.PostAsync(RootEndpt + endpt.Api() + vars, data).Result;
            var a = rspMsg.Content.ReadAsStringAsync().Result;
            AssertResult(expectedCode, rspMsg);

            return rspMsg.GetRsp();
        }

        //put
        public dynamic PutCall(Enum endpt, object body, string vars, int expectedCode = 200)
        {
            StartStep(endpt, "Put", expectedCode);
            var data = FormatBody(body);
            var rspMsg = Client.PutAsync(RootEndpt + endpt.Api() + vars, data).Result;
            var a = rspMsg.Content.ReadAsStringAsync();
            AssertResult(expectedCode, rspMsg);

            return rspMsg.GetRsp();
        }

        //delete
        public dynamic DeleteCall(Enum endpt, string query = "", int expectedCode = 200)
        {
            StartStep(endpt, "Delete", expectedCode);
            var ept = $"{RootEndpt}{endpt.Api()}{query}";
            var rspMsg = Client.DeleteAsync(ept).Result;

            AssertResult(expectedCode, rspMsg);

            return rspMsg.GetRsp();
        }

        //patch
        public dynamic PatchCall(Enum endpt, object body, string vars, int expectedCode = 200)
        {
            StartStep(endpt, "Patch", expectedCode);
            var data = FormatBody(body);
            var rspMsg = Client.PatchAsync(RootEndpt + endpt.Api() + vars, data).Result;
            var a = rspMsg.Content.ReadAsStringAsync();
            AssertResult(expectedCode, rspMsg);

            return rspMsg.GetRsp();
        }

        private HttpContent FormatBody(object body)
        {
            return ApplicationType switch
            {
                "application/x-www-form-urlencoded" => new FormUrlEncodedContent((IEnumerable<KeyValuePair<string, string>>)body),
                _ => new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, ApplicationType),
            };
        }

        private void AssertResult(int expectedCode, HttpResponseMessage rspMsg)
        {
            if (TE != null)
            {
                TE.Assert.AreEqual(expectedCode, (int)rspMsg.StatusCode, "Verify response code matches expected");
            }
        }

        private void StartStep(Enum endpt, string type, int expectedCode)
        {
            if (TE != null)
            {
                TE.BeginTestCaseStep($"API {type} call to {endpt}", expectedCode.ToString());
            }
        }

        public void Dispose()
        {
            if(Client != null)
            {
                Client.Dispose();
            }
        }
    }

    public static class ApiHelper
    {
        public static dynamic GetRsp(this HttpResponseMessage response)
        {
            try
            {
                return JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync().Result);
            }
            catch
            {
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        public static List<dynamic> GetRspList(dynamic rsp)
        {
            try
            {
                return rsp.ToObject<List<dynamic>>();
            }
            catch
            {
                return new List<dynamic>() { rsp };
            }
        }
    }
}
