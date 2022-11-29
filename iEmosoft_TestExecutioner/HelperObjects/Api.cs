﻿using aUI.Automation.Enums;
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
        public HttpClient Client { get; }
        TestExecutioner TE;
        string ApplicationType = "application/json";
        string RootEndpt = "";
        List<int> RetryCode = new List<int> { 502, 503, 504 };
        int RetryWait = 750;
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
            Client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(appType));
        }

        public void SetAuthentication(string authKey, string type = "Bearer")
        {
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(type, authKey);
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
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(acceptType));
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
        public dynamic GetCall(Enum endpt, string query = "", int expectedCode = 200, bool retry = true)
        {
            StartStep(endpt, "Get", expectedCode);
            var ept = $"{RootEndpt}{endpt.Api()}{query}";
            //Console.WriteLine("GET: "+ept);
            RspMsg = Client.GetAsync(ept).Result;
            var a = RspMsg.Content.ReadAsStringAsync();
            //Console.WriteLine("rsp: " + a.Result);
            if(retry && RetryCode.Contains((int)RspMsg.StatusCode))
            {
                TE.Pause(500);
                return GetCall(endpt, query, expectedCode, false);
            }
            if (expectedCode != 0)
            {
                AssertResult(expectedCode, RspMsg);
            }
            //Console.WriteLine(((object)RspMsg.GetRsp()).ToString());
            return RspMsg.GetRsp();
        }

        //post
        public dynamic PostCall(Enum endpt, object body, string vars, int expectedCode = 200, bool retry = true)
        {
            StartStep(endpt, "Post", expectedCode);
            var data = FormatBody(body);
            RspMsg = Client.PostAsync(RootEndpt + endpt.Api() + vars, data).Result;
            var a = RspMsg.Content.ReadAsStringAsync().Result;

            //Console.WriteLine($"URL: {RootEndpt + endpt.Api()}");
            //Console.WriteLine($"VARS: {vars}");
            //Console.WriteLine($"Body: {data.ReadAsStringAsync().Result}");
            //Console.WriteLine("RESULT:");
            //Console.WriteLine(a);
            if (retry && RetryCode.Contains((int)RspMsg.StatusCode))
            {
                TE.Pause(500);
                return PostCall(endpt, body, vars, expectedCode, false);
            }
            if (expectedCode != 0)
            {
                AssertResult(expectedCode, RspMsg);
            }
            return RspMsg.GetRsp();
        }

        //put
        public dynamic PutCall(Enum endpt, object body, string vars, int expectedCode = 200, bool retry = true)
        {
            StartStep(endpt, "Put", expectedCode);
            var data = FormatBody(body);
            RspMsg = Client.PutAsync(RootEndpt + endpt.Api() + vars, data).Result;
            var a = RspMsg.Content.ReadAsStringAsync();
            if (retry && RetryCode.Contains((int)RspMsg.StatusCode))
            {
                TE.Pause(500);
                return PutCall(endpt, body, vars, expectedCode, false);
            }
            if (expectedCode != 0)
            {
                AssertResult(expectedCode, RspMsg);
            }
            return RspMsg.GetRsp();
        }

        //delete
        public dynamic DeleteCall(Enum endpt, string query = "", int expectedCode = 200, bool retry = true)
        {
            StartStep(endpt, "Delete", expectedCode);
            var ept = $"{RootEndpt}{endpt.Api()}{query}";
            RspMsg = Client.DeleteAsync(ept).Result;

            if (retry && RetryCode.Contains((int)RspMsg.StatusCode))
            {
                TE.Pause(500);
                return DeleteCall(endpt, query, expectedCode, false);
            }
            if (expectedCode != 0)
            {
                AssertResult(expectedCode, RspMsg);
            }
            return RspMsg.GetRsp();
        }

        public dynamic DeleteCall(Enum endpt, object body, string query = "", int expectedCode = 200, bool retry = true)
        {
            StartStep(endpt, "Delete", expectedCode);
            var ept = $"{RootEndpt}{endpt.Api()}{query}";

            var request = new HttpRequestMessage(HttpMethod.Delete, ept)
            {
                Content = FormatBody(body)
            };

            RspMsg = Client.SendAsync(request).Result;

            if (retry && RetryCode.Contains((int)RspMsg.StatusCode))
            {
                TE.Pause(500);
                return DeleteCall(endpt, body, query, expectedCode, false);
            }
            if (expectedCode != 0)
            {
                AssertResult(expectedCode, RspMsg);
            }
            return RspMsg.GetRsp();
        }

        //patch
        public dynamic PatchCall(Enum endpt, object body, string vars, int expectedCode = 200, bool retry = true)
        {
            StartStep(endpt, "Patch", expectedCode);
            var data = FormatBody(body);
            RspMsg = Client.PatchAsync(RootEndpt + endpt.Api() + vars, data).Result;
            var a = RspMsg.Content.ReadAsStringAsync();
            if (retry && RetryCode.Contains((int)RspMsg.StatusCode))
            {
                TE.Pause(500);
                return PatchCall(endpt, body, vars, expectedCode, false);
            }
            if (expectedCode != 0)
            {
                AssertResult(expectedCode, RspMsg);
            }
            return RspMsg.GetRsp();
        }

        public HttpContent FormatBody(object body)
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
            if (TE != null && expectedCode != 0)
            {
                TE.BeginTestCaseStep($"API {type} call to {endpt}", expectedCode.ToString());

                if (DateTime.Now >= TE.TestTimeLimit)
                {
                    TE.Assert.Fail($"Test Exceeded Max Time Limit of: {TE.TestTimeLimit.Subtract(TE.StartTime)} (hh:mm:ss)");
                }              
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
