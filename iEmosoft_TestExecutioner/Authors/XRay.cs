using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.ExtendedClient;
using aUI.Automation.Enums;
using aUI.Automation.HelperObjects;
using aUI.Automation.ModelObjects;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Amazon.S3;

namespace aUI.Automation.Authors
{
    public class XRay
    {
        enum Endpts
        {
            [Api("/api/v2/authenticate")] Authenticate,
            [Api("/api/v2/graphql")] Graph,
            [Api("/api/v2/import/execution")] ImportResults,
            [Api("")] EmptyAPI,
        }

        /* Config settings needed:
         * XRayToken
         * XRayBase
         * XRayProject
         * 
         * XRayRetiredFolder
         * XRayTestFolder
         */

        int MinuteThreshold = 40;
        int ApiWaitTime = 0;
        private int EditTestThreshold = 150;
        string Project = Config.GetConfigSetting("XRayProject");
        string TestFolder = Config.GetConfigSetting("XRayTestFolder");
        string RetiredFolder = Config.GetConfigSetting("XRayRetiredFolder");
        string Env = Config.GetEnvironment();
        string ExecutionName = Config.GetConfigSetting("TestRunName", "Automation Test Execution");
        string Queue = Config.GetConfigSetting("XRayQueue", "");
        private string Auth = "";
        
        string ExecutionId = "";
        string ExecutionKey = "";
        private List<object> TestResults = new();
        private object Lock2;
        List<string> TestEnvs = new List<string>();
        List<string> Folders = new List<string>();
        string ProjectId = "";
        Api ApiObj;
        Api ApiQueue;
        int TestGroups = 3;
        AmazonS3Client S3Client;
        IAmazonSQS SqsQueue;
        AmazonSQSExtendedClient SqsExtend;

        List<XRayTest> Tests = new List<XRayTest>();
        object Locker;
        private DateTime LastCallTime;

        #region ApiRestrictions
        private void AddResultToList(object obj)
        {
            lock (Lock2)
            {
                TestResults.Add(obj);
            }
        }

        private List<object> GetResultsFromList(bool restrict = true)
        {
            lock (Lock2)
            {
                if (!string.IsNullOrEmpty(Queue) && restrict)
                {
                    var temp = new List<object>()
                    {
                        TestResults[0]
                    };
                    TestResults.RemoveAt(0);
                    return temp;
                }
                if (TestResults.Count >= 3 && restrict)
                {
                    var temp = new List<object>()
                    {
                        TestResults[0],
                        TestResults[1],
                        TestResults[2]
                    };

                    TestResults.RemoveRange(0, 2);
                    return temp;
                } else if (!restrict && TestResults.Count > 0)
                {
                    var temp = new List<object>();
                    TestResults.ForEach(x => temp.Add(x));

                    TestResults = new List<object>();
                    return temp;
                }

                var temp3 = new List<object>()
                    {
                        TestResults[0]
                    };
                TestResults.RemoveAt(0);
                return temp3;
            }

            return null;
        }

        private void Wait()
        {
            try
            {
                var wait = ApiWaitTime - DateTime.Now.Subtract(LastCallTime).Milliseconds;
                if (wait > 0)
                {
                    Thread.Sleep(wait);
                }
            }
            catch
            {

            }

            LastCallTime = DateTime.Now;
        }
        #endregion
        public XRay(string testFolder = "")
        {
            LastCallTime = DateTime.Now;
            ApiWaitTime = ((60/MinuteThreshold) * 1000) + 2;
            Locker = new object();
            Lock2 = new object();
            ApiObj = new Api(null, Config.GetConfigSetting("XRayBase"));
            Auth = GetAuthentication();
            ApiObj.SetAuthentication(Auth);
            if (!string.IsNullOrEmpty(Queue))
            {
                var bucket = Config.GetConfigSetting("QueueBucket");
                SqsQueue = new AmazonSQSClient();
                S3Client = new AmazonS3Client();
                SqsExtend = new AmazonSQSExtendedClient(SqsQueue, new ExtendedClientConfiguration().WithLargePayloadSupportEnabled(S3Client, bucket));
                ApiQueue = new Api(null, Config.GetConfigSetting("XRayQueueBase"), "application/x-www-form-urlencoded");
            }
            ProjectId = GetProjectSettings();

            if (!string.IsNullOrEmpty(testFolder))
            {
                TestFolder = testFolder;
            }

            GetFolders(ProjectId);
            GetAllTestCases();
        }
        #region Public methods
        public void CreateTestRun(List<string> testCases = null)
        {
            if (testCases == null)
            {
                testCases = new List<string>();
                Tests.ForEach(x => testCases.Add(x.IssueId));
            }

            var time = $" {DateTime.Now:MM/dd/yy H:mm:ss} {TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).Hours}";
            try
            {
                DateTime timeUtc = DateTime.UtcNow;
                var cstTime = timeUtc.AddHours(-6);
                var zone = "CDT";
                if (IsCST())
                {
                    cstTime = cstTime.AddHours(1);
                    zone = "CST";
                }

                time = $" {cstTime:MM/dd/yy H:mm:ss} {zone}";
            }
            catch{}

            ExecutionName += time;

            var cases = string.Join(", ", testCases.Select(x => string.Format("\"{0}\"", x)));
            var query = "mutation {createTestExecution(testIssueIds: [" + cases + "] testEnvironments: [\"" + Env + "\"] jira: {fields: { summary: \"" + ExecutionName + "\", project: {key: \"" + Project + "\"} }}) {testExecution {issueId jira(fields: [\"key\"])} warnings createdTestEnvironments}}";

            dynamic rsp;
            lock (Locker)
            {
                Wait();
            }
            rsp = ApiObj.PostCall(Endpts.Graph, new { query }, "", 0);
            CheckCall(rsp);

            ExecutionKey = (string)rsp.data.createTestExecution.testExecution.jira.key;
            ExecutionId = (string)rsp.data.createTestExecution.testExecution.issueId;
        }

        private bool IsCST()
        {
            var currDate = DateTime.Now;
            var start = new DateTime(currDate.Year, 3, 1);
            start = start.DayOfWeek == DayOfWeek.Sunday ? start : start.AddDays(DayOfWeek.Sunday - start.DayOfWeek);
            start.AddDays(7);

            var end = new DateTime(currDate.Year, 11, 1);
            end = end.DayOfWeek == DayOfWeek.Sunday ? end : end.AddDays(DayOfWeek.Sunday - end.DayOfWeek);

            return currDate > start && currDate < end;
        }

        public void CloseTestRun()
        {
            SubmitResults(false);
            //looks like 1-2 jira calls
        }

        public void AddTestResult(TestExecutioner te, string testName)
        {
            te.FailLastStepIfFailureNotTriggered();

            var testCase = FindTestCase(testName, te);

            if (te.NUnitResult.Outcome.Status.ToString().Equals("Skipped"))
            {
                AddSkippedResult(te, testCase.IssueKey);
                return;
            }

            AddTestToTestRun(testCase.IssueId);
            AddTestResults(te, testCase.IssueKey);
        }

        public void SetupFailures(List<string> testNames, string error = "")
        {
            var start = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ssK");
            var finish = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ssK");
            var status = "FAILED";

            var comment = $"*Test failure during setup* {Environment.NewLine}Failed due to: {error}";

            var info = new
            {
                finishDate = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ssK"),
            };

            //generate this from each test object we get
            var tests = new List<object>();

            foreach (var test in testNames)
            {
                var testCase = Tests.FirstOrDefault(x => x.Name.Trim().Equals(test.Trim()));

                if (testCase != null)
                {
                    tests.Add(new
                    {
                        testKey = testCase.IssueKey,
                        start,
                        finish,
                        comment,
                        status,
                        steps = new object[0]
                    });
                }
            }

            //submit results
            if(tests.Count > 0)
            {
                var body = new
                {
                    testExecutionKey = ExecutionKey,
                    info,
                    tests = tests.ToArray()
                };

                dynamic rsp;
                if (string.IsNullOrEmpty(Queue))
                {
                    lock (Locker)
                    {
                        Wait();
                    }

                    rsp = ApiObj.PostCall(Endpts.ImportResults, body, "", 0);
                    CheckCall(rsp);
                }
                else
                {
                    ImportCall(body);
                }
            }
        }
        #endregion

        private void ImportCall(object body)
        {
            var url = Config.GetConfigSetting("XRayQueueBase") + Queue;

            var rand = new Random();
            var num = rand.Next(0, rand.Next(100, 999999999));
            var bodyFormatted = ApiObj.FormatBody(body).ReadAsStringAsync().Result;

            var attribute = new Dictionary<string, MessageAttributeValue>
            {
                { "URL", new MessageAttributeValue() { DataType = "String", StringValue = $"{Config.GetConfigSetting("XRayBase")}{Endpts.ImportResults.Api()}" }},
                { "Auth", new MessageAttributeValue() { DataType = "String", StringValue = Auth } },
                { "Body", new MessageAttributeValue() { DataType = "String", StringValue = bodyFormatted } },
                { "Type", new MessageAttributeValue() { DataType = "String", StringValue = "standard" } }
            };

            var rq = new SendMessageRequest()
            {
                MessageAttributes = attribute,
                QueueUrl = url,
                MessageBody = "Import Test Results",
                MessageDeduplicationId = num.ToString(),
                MessageGroupId = "2"
            };

            var bodyCount = ASCIIEncoding.ASCII.GetBytes(bodyFormatted);
            var authCount = ASCIIEncoding.ASCII.GetBytes(Auth);
            var dIdCount = ASCIIEncoding.ASCII.GetBytes(num.ToString());
            var urlCount = ASCIIEncoding.ASCII.GetBytes($"{Config.GetConfigSetting("XRayBase")}{Endpts.ImportResults.Api()}");
            var b = "";
            var c = bodyCount.Length;
            b = "";

            var rsp = SqsQueue.SendMessageAsync(rq).Result;
        }

        private void QueueCall(string query)
        {
            var url = Config.GetConfigSetting("XRayQueueBase") + Queue;
            var rand = new Random();
            var num = rand.Next(0, rand.Next(100, 999999999));
            var type = query.StartsWith("query") ? "query" : "mutation";

            var attribute = new Dictionary<string, MessageAttributeValue>
            {
                { "URL", new MessageAttributeValue() { DataType = "String", StringValue = $"{Config.GetConfigSetting("XRayBase")}{Endpts.Graph.Api()}" }},
                { "Auth", new MessageAttributeValue() { DataType = "String", StringValue = Auth } },
                { "Body", new MessageAttributeValue() { DataType = "String", StringValue = query } },
                { "Type", new MessageAttributeValue() { DataType = "String", StringValue = "query" } }
            };

            var rq = new SendMessageRequest()
            {
                MessageAttributes = attribute,
                QueueUrl = url,
                MessageBody = "GraphQL Call",
                MessageDeduplicationId = num.ToString(),
                MessageGroupId = "2"
            };

            var rsp = SqsQueue.SendMessageAsync(rq).Result;
        }

        private string GetProjectSettings()
        {
            var query = "query {getProjectSettings (projectIdOrKey: \"" + Project + "\") {projectId, testEnvironments}}";

            dynamic rsp;
            lock (Locker)
            {
                Wait();
            }

            rsp = ApiObj.PostCall(Endpts.Graph, new { query }, "", 0);
            CheckCall(rsp);

            foreach (var env in ApiHelper.GetRspList(rsp.data.getProjectSettingstestEnvironments))
            {
                TestEnvs.Add((string)env);
            }

            return (string)rsp.data.getProjectSettings.projectId;
        }

        private void GetFolders(string projectId)
        {
            var query = "query {getFolder(projectId: \"" + projectId + "\", path: \"/\") {path folders}}";

            dynamic rsp;
            lock (Locker)
            {
                Wait();
            }

            rsp = ApiObj.PostCall(Endpts.Graph, new { query }, "", 0);
            CheckCall(rsp);

            //deal with nested folders later
            foreach (var folder in ApiHelper.GetRspList(rsp.data.getFolder.folders))
            {
                var path = (string)folder.path;
                if (path.Contains(TestFolder))
                {
                    TestFolder = path;
                }
                else if (path.Contains(RetiredFolder))
                {
                    RetiredFolder = path;
                }

                Folders.Add(path);
            }
        }

        private void GetAllTestCases()
        {
            var testReturnLimit = 100;
            var start = -testReturnLimit;

            int totalCount;
            do
            {
                start += testReturnLimit;
                //TODO Update query to get the test name back

                var folder = TestFolder.Contains("/") ? " folder: {path: \"" + TestFolder + "\"}" : "";

                var query = "query { getTests(projectId: \"" + ProjectId + "\" limit: " + testReturnLimit + " start: " + start + folder + ") {total results { issueId projectId testType {name} steps {id action data result} jira(fields: [\"summary\", \"key\"])}}}";


                dynamic rsp;
                lock (Locker)
                {
                    Wait();
                }
                rsp = ApiObj.PostCall(Endpts.Graph, new { query }, "", 0);
                CheckCall(rsp);

                totalCount = (int)rsp.data.getTests.total;

                var tests = ApiHelper.GetRspList(rsp.data.getTests.results);

                foreach (var test in tests)
                {
                    //add test to full list
                    Tests.Add(new XRayTest(test));
                }
            } while (start < totalCount && totalCount < 5000);
        }

        private void UpdateTestCase(string testName, XRayTest test, List<TestCaseStep> testSteps)
        {
            //check diff count
            var diff = test.StepDiff(testSteps);

            if (diff > EditTestThreshold)
            {
                RemoveTestFromTestRun(test.IssueId);
                MoveTestCase(test.IssueId, RetiredFolder);
                CreateTestCase(testName, testSteps);
            }
            else if (diff > 5)
            {
                var mutations = new List<string>();
                var removeItems = true;
                int index;
                for (index = 0; index < testSteps.Count; index++)
                {
                    //if testSteps is out of range, break
                    if (index >= test.Steps.Count)
                    {
                        removeItems = false;
                        break;
                    }
                    else if (!((string)test.Steps[index].action).Equals(testSteps[index].StepDescription.Replace("\\", "-")))
                    {
                        break;
                        //if old and new steps don't match, break
                        //make sure prior steps are removed from that index on
                    }
                }

                if (testSteps.Count < test.Steps.Count)
                {
                    for (int i = testSteps.Count; i < test.Steps.Count; i++)
                    {
                        mutations.Add($"removeTestStep(stepId: \"{(string)test.Steps[i].id}\")");
                    }
                }

                //update test steps that already exist
                for (int i = index; i < test.Steps.Count && i < testSteps.Count; i++)
                {
                    GenerateTestSteps(new List<TestCaseStep>() { testSteps[i] }, out var str);
                    mutations.Add($"updateTestStep(stepId: \"{(string)test.Steps[i].id}\" step: {str[1..^1]}){{warnings}}");
                }

                //add new test steps
                for (int i = test.Steps.Count; i < testSteps.Count; i++)
                {
                    GenerateTestSteps(new List<TestCaseStep>() { testSteps[i] }, out var str);
                    mutations.Add($"addTestStep(issueId: \"{(string)test.IssueId}\" step: {str[1..^1]}){{id}}");
                }

                for (int i = 0; i < mutations.Count; i += 12)
                {
                    var query = $"mutation {{ {string.Join(" ", GetMutationSubset(mutations, i, 12))} }}";

                    dynamic rsp;
                    if (string.IsNullOrEmpty(Queue))
                    {
                        lock (Locker)
                        {
                            Wait();
                        }

                        query = query.Replace("\n", " ").Replace("\r", " ");
                        rsp = ApiObj.PostCall(Endpts.Graph, new { query }, "", 0);
                        CheckCall(rsp);
                    }
                    else
                    {
                        QueueCall(query);
                    }
                }

                //TODO build call to add new test steps

                //somehow figure out how to update the steps
                //start with finding where steps differ
                //from that point, update existing steps
                //if now fewer steps, then remove excess
                //if now more steps, then add
                //if steps are the same, then make no change
            }
        }

        private List<string> GetMutationSubset(List<string> list, int start, int max = 10)
        {
            var rtn = new List<string>();
            for (int i = start; i < (start + max) && i < list.Count; i++)
            {
                rtn.Add($"val{i}: {list[i]}");
            }

            return rtn;
        }

        private XRayTest FindTestCase(string testName, TestExecutioner te)
        {
            var testSteps = te.RecordedSteps;
            bool hasTest = Tests.Any(x => x.Name.ToLower().Trim().Equals(testName.ToLower().Trim()));

            if (hasTest)
            {
                var test = Tests.First(x => x.Name.Trim().Equals(testName.Trim()));

                if (!te.TestCaseFailed)
                {
                    UpdateTestCase(testName, test, testSteps);
                }

                return test;
            }
            else
            {
                return CreateTestCase(testName, testSteps);
            }
        }

        private XRayTest CreateTestCase(string testName, List<TestCaseStep> testSteps)
        {
            //Potentially use the 'import' instead of this as it may be much quicker
            GenerateTestSteps(testSteps, out var steps);

            var folder = string.IsNullOrEmpty(TestFolder) ? "" : "folderPath: \"" + TestFolder + "\"";

            var query = "mutation {createTest(testType: { name: \"Automated\" }steps: "
                //+"[{action: \"Create first example step\", result: \"First step was created\"},{action: \"Create second example step with data\", data: \"Data for the step\", result: \"Second step was created with data\" }]"
                + steps
                + " jira: {fields: { summary:\"" +
                testName.Trim() + "\", project: {key: \"" + Project + "\"} }}" + folder + ") {test {issueId testType {name} steps {id action data result} jira(fields: [\"key\", \"summary\"])} warnings}}";

            query = query.Replace("\n", " ").Replace("\r", " ");

            dynamic rsp;
            lock (Locker)
            {
                Wait();
            }
            rsp = ApiObj.PostCall(Endpts.Graph, new { query }, "", 0);
            CheckCall(rsp);

            var test = new XRayTest(rsp.data.createTest.test);

            Tests.Add(test);
            return test;
        }

        private void MoveTestCase(string testId, string folder)
        {
            var query = "mutation {addTestsToFolder (projectId: \"" + ProjectId + "\", path: \"" + folder + "\", testIssueIds:[\"" + testId + "\"]) {folder {name path}}}";

            dynamic rsp;
            if (string.IsNullOrEmpty(Queue))
            {
                lock (Locker)
                {
                    Wait();
                }
                rsp = ApiObj.PostCall(Endpts.Graph, new { query }, "", 0);
                CheckCall(rsp);
            }
            else
            {
                QueueCall(query);
            }
        }

        private void AddTestToTestRun(string testId)
        {
            if (Tests.FindIndex(x => testId.Equals(((object)x.IssueId).ToString())) == -1)
            {
                var query = "mutation {addTestsToTestExecution(issueId: \"" + ExecutionId + "\" testIssueIds: [\"" + testId + "\"]) {addedTests warning}}";

                dynamic rsp;
                if (string.IsNullOrEmpty(Queue))
                {
                    lock (Locker)
                    {
                        Wait();
                    }
                    rsp = ApiObj.PostCall(Endpts.Graph, new { query }, "", 0);
                    CheckCall(rsp);
                }
                else
                {
                    QueueCall(query);
                }
            }
        }

        private void RemoveTestFromTestRun(string testId)
        {
            var query = "mutation {removeTestsFromTestExecution(issueId: \"" + ExecutionId + "\" testIssueIds: [\"" + testId + "\"])}";

            dynamic rsp;
            if (string.IsNullOrEmpty(Queue))
            {
                lock (Locker)
                {
                    Wait();
                    //rsp = ApiObj.PostCall(Endpts.Graph, new { query }, "");
                }
                rsp = ApiObj.PostCall(Endpts.Graph, new { query }, "", 0);
                CheckCall(rsp);
            }
            else
            {
                QueueCall(query);
            }
        }

        private void AddSkippedResult(TestExecutioner te, string testKey)
        {
            var start = te.StartTime.ToString("yyyy-MM-dd'T'HH:mm:ssK");
            var end = te.DisposeTime == null ? DateTime.Now : (DateTime)te.DisposeTime;
            var finish = end.ToString("yyyy-MM-dd'T'HH:mm:ssK");

            var comment = $"Total Runtime: {end.Subtract(te.StartTime).ToString().Split('.')[0]} (hh:mm:ss)" + Environment.NewLine;

            comment += "This test was skipped because " + te.NUnitResult.Outcome.Label;

            var info = new
            {
                finishDate = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ssK"),
            };

            //generate this from each test object we get

            var test = new
            {
                testKey,
                start,
                finish,
                comment,
                status = "SKIPPED"
            };

            var body = new
            {
                testExecutionKey = ExecutionKey,
                info,
                tests = new object[] { test }
            };

            AddResultToList(body.tests[0]);

            SubmitResults();
        }

        private void AddTestResults(TestExecutioner te, string testKey)
        {
            var start = te.StartTime.ToString("yyyy-MM-dd'T'HH:mm:ssK");
            var end = te.DisposeTime == null ? DateTime.Now : (DateTime)te.DisposeTime;
            var finish = end.ToString("yyyy-MM-dd'T'HH:mm:ssK");
            var status = te.TestCaseFailed ? "FAILED" : "PASSED";

            var comment = $"*Total Runtime:* {end.Subtract(te.StartTime).ToString().Split('.')[0]} (hh:mm:ss)";
            if (te.TestCaseFailed)
            {
                if(te.TestAuthor.ReportedBugs.Count > 0)
                {
                    var bugBase = Config.GetConfigSetting("BugLinkBase", "");
                    var list = new List<string>();
                    te.TestAuthor.ReportedBugs.ForEach(r => list.Add($"[{r}|{bugBase}{r}]"));
                    status = "BUGGED";
                    comment += Environment.NewLine + "*Likely failing due to bug(s):* " + string.Join(", ", list);
                }

                comment += Environment.NewLine + "*Step Failures:*";
                var index = 0;
                foreach (var step in te.RecordedSteps)
                {
                    index++;
                    if (!step.StepPassed)
                    {
                        comment += Environment.NewLine + $"Step {index}: {step.StepDescription.Replace("\\", "-")}   Expected: {step.ExpectedResult}   Actual: {step.ActualResult}";
                    }
                }

                comment += Environment.NewLine + "*StackTrace:*" + Environment.NewLine + te.TestStackTrace;
            }

            var info = new
            {
                finishDate = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ssK"),
            };

            //generate this from each test object we get

            var test = new
            {
                testKey,
                start,
                finish,
                comment,
                status,
                steps = GenerateTestSteps(te.RecordedSteps, out _, te.TestCaseFailed)
            };

            var body = new
            {
                testExecutionKey = ExecutionKey,
                info,
                tests = new object[] { test }
            };

            byte[] img;
            if (te.TestCaseFailed)
            {
                for (int i = te.RecordedSteps.Count; i > 0; i--)
                {
                    if (te.RecordedSteps[i - 1].ImageData != null)
                    {
                        var rand = new RandomTestData();
                        img = te.RecordedSteps[i - 1].ImageData;

                        var test2 = new
                        {
                            test.testKey,
                            test.start,
                            test.finish,
                            test.comment,
                            evidence = new[]
                            {
                                new
                                {
                                    data = Convert.ToBase64String(img),
                                    filename = $"{rand.GetRandomAlphaNumericString(50)}.png",
                                    contentType = "image/png"
                                }
                            },
                            test.status,
                            test.steps
                        };

                        body = new
                        {
                            testExecutionKey = ExecutionKey,
                            info,
                            tests = new object[] { test2 }
                        };
                        break;
                    }
                }
            }

            AddResultToList(body.tests[0]);

            SubmitResults();
        }

        private void SubmitResults(bool restricted = true)
        {
            var testBody = GetResultsFromList(restricted);

            if(testBody != null)
            {
                var info = new
                {
                    finishDate = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ssK"),
                };

                var body = new
                {
                    testExecutionKey = ExecutionKey,
                    info,
                    tests = testBody.ToArray()
                };

                dynamic rsp;
                if (string.IsNullOrEmpty(Queue))
                {
                    lock (Locker)
                    {
                        Wait();
                    }
                    rsp = ApiObj.PostCall(Endpts.ImportResults, body, "", 0);
                    CheckCall(rsp);
                } else
                {
                    ImportCall(body);
                }
            }
        }

        private void CheckCall(dynamic rsp, [CallerMemberName] string callerName = "")
        {
            if (Convert.ToString((object)rsp).ToLower().Contains("errors"))
            {
                try
                {
                    Console.WriteLine($"TIME: {DateTime.Now.ToString("HH:mm:ss.ff")} Called from: {callerName} {Convert.ToString((object)rsp)}");
                }
                catch { }
            }
        }

        private object[] GenerateTestSteps(List<TestCaseStep> steps, out string xrayStr, bool testFailed = false)
        {
            xrayStr = "[";
            List<object> xraySteps = new List<object>();
            for (int i = 0; i < steps.Count; i++)
            //foreach(var step in steps)
            {
                var step = steps[i];
                var status = step.StepPassed ? "PASSED" : "FAILED";
                if (step.ImageData != null && (!step.StepPassed || (testFailed && i == steps.Count - 1)))
                {
                    status = "FAILED";
                    var rand = new RandomTestData();

                    var evidences = new object[]
                    {
                new {
                        data = Convert.ToBase64String(step.ImageData),
                        filename = $"{rand.GetRandomAlphaNumericString(50)}.png",
                        contentType = "image/png"
                    }
                    };

                    xraySteps.Add(new
                    {
                        status,
                        comment = step.Notes,
                        actualResult = step.ActualResult,
                        evidences
                    });
                }
                else
                {
                    xraySteps.Add(new
                    {
                        status,
                        comment = step.Notes,
                        actualResult = step.ActualResult
                    });
                }

                xrayStr += xraySteps.Count > 1 ? ", " : "";

                xrayStr += "{action: \"" + step.StepDescription.Replace("\\", "-") + "\", result: \"" + step.ExpectedResult + "\"}";//, actualResult: \""+step.ActualResult+"\"}";
            }
            xrayStr += "]";
            return xraySteps.ToArray();
        }

        private string GetAuthentication()
        {
            var token = Config.GetConfigSetting("XRayToken");
            var split = Encoding.Default.GetString(Convert.FromBase64String(token)).Split(':');

            var body = new
            {
                client_id = split[0],
                client_secret = split[1]
            };


            dynamic rsp;
            lock (Locker)
            {
                Wait();
                //rsp = ApiObj.PostCall(Endpts.Authenticate, body, "");
            }
            rsp = ApiObj.PostCall(Endpts.Authenticate, body, "", 0);
            CheckCall(rsp);

            return (string)rsp;
        }
    }

    class XRayTest
    {
        public dynamic RawData;
        public string IssueId { get { return (string)RawData.issueId; } }
        public string IssueKey { get { return (string)RawData.jira.key; } }
        public List<dynamic> Steps;
        public string Name { get { return (string)RawData.jira.summary; } }
        public XRayTest(dynamic data)
        {
            RawData = data;
            Steps = ApiHelper.GetRspList(RawData.steps);
        }

        public int StepDiff(List<TestCaseStep> currSteps)
        {
            var lengthDiff = Steps.Count - currSteps.Count;
            var diffCount = Math.Abs(lengthDiff);

            var maxCount = lengthDiff >= 0 ? currSteps.Count : Steps.Count;

            for (int i = 0; i < maxCount; i++)
            {
                if (!currSteps[i].StepDescription.Replace("\\", "-").Replace("\n", " ").Replace("\r", " ").Equals((string)Steps[i].action))
                {
                    diffCount++;
                }
            }

            return diffCount;
        }
    }
}
