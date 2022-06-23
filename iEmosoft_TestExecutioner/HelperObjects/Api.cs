using aUI.Automation.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace aUI.Automation.HelperObjects
{
    public class Api : IDisposable
    {
        //constructor would create the primary object and any needed default headers + auth tokens
        public HttpClient Client { get; }
        TestExecutioner TE;
        string ApplicationType = "application/json";
        string RootEndpt = "";
        public HttpResponseMessage RspMsg;

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
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ApplicationType));
        }

        public Api(TestExecutioner te, HttpClientHandler handler, string baseUrl)
        {
            TE = te;
            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = Config.GetConfigSetting("ApiUrl", "");
            }

            Client = new(handler);
            Client.BaseAddress = new Uri(baseUrl);
        }

        public void SetAuthentication(string authKey, string type = "Bearer")
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(type, authKey);
        }

        public void AddHeader(string name, string value)
        {
            Client.DefaultRequestHeaders.Add(name, value);
        }

        public void UpdateRequestType(string appType, string acceptType = "")
        {
            if (string.IsNullOrEmpty(acceptType))
            {
                acceptType = appType;
            }
            //TOOD may need to remove other first
            //Client.DefaultRequestHeaders.Accept
            ApplicationType = appType;
            Client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(appType));
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
            RspMsg = Client.GetAsync(ept).Result;
            var a = RspMsg.Content.ReadAsStringAsync();
            //Console.WriteLine("rsp: " + a.Result);
            AssertResult(expectedCode, RspMsg);

            return RspMsg.GetRsp();
        }

        //post
        public dynamic PostCall(Enum endpt, object body, string vars, int expectedCode = 200)
        {
            StartStep(endpt, "Post", expectedCode);
            var data = FormatBody(body);
            RspMsg = Client.PostAsync(RootEndpt + endpt.Api() + vars, data).Result;
            var a = RspMsg.Content.ReadAsStringAsync().Result;
            AssertResult(expectedCode, RspMsg);

            return RspMsg.GetRsp();
        }

        //put
        public dynamic PutCall(Enum endpt, object body, string vars, int expectedCode = 200)
        {
            StartStep(endpt, "Put", expectedCode);
            var data = FormatBody(body);
            RspMsg = Client.PutAsync(RootEndpt + endpt.Api() + vars, data).Result;
            var a = RspMsg.Content.ReadAsStringAsync();
            AssertResult(expectedCode, RspMsg);

            return RspMsg.GetRsp();
        }

        //delete
        public dynamic DeleteCall(Enum endpt, string query = "", int expectedCode = 200)
        {
            StartStep(endpt, "Delete", expectedCode);
            var ept = $"{RootEndpt}{endpt.Api()}{query}";
            RspMsg = Client.DeleteAsync(ept).Result;

            AssertResult(expectedCode, RspMsg);

            return RspMsg.GetRsp();
        }

        public dynamic DeleteCall(Enum endpt, object body, string query = "", int expectedCode = 200)
        {
            StartStep(endpt, "Delete", expectedCode);
            var ept = $"{RootEndpt}{endpt.Api()}{query}";

            var request = new HttpRequestMessage(HttpMethod.Delete, ept)
            {
                Content = FormatBody(body)
            };

            RspMsg = Client.SendAsync(request).Result;

            AssertResult(expectedCode, RspMsg);

            return RspMsg.GetRsp();
        }

        //patch
        public dynamic PatchCall(Enum endpt, object body, string vars, int expectedCode = 200)
        {
            StartStep(endpt, "Patch", expectedCode);
            var data = FormatBody(body);
            RspMsg = Client.PatchAsync(RootEndpt + endpt.Api() + vars, data).Result;
            var a = RspMsg.Content.ReadAsStringAsync();
            AssertResult(expectedCode, RspMsg);

            return RspMsg.GetRsp();
        }

        private HttpContent FormatBody(object body)
        {
            return ApplicationType switch
            {
                "application/x-www-form-urlencoded" => new FormUrlEncodedContent((IEnumerable<KeyValuePair<string, string>>)body),
                "application/xml" => new StringContent(body.ToString(), Encoding.UTF8, ApplicationType),
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
