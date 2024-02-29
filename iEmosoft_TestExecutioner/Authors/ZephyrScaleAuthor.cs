using aUI.Automation.BaseClasses;
using aUI.Automation.Enums;
using aUI.Automation.HelperObjects;
using aUI.Automation.ModelObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aUI.Automation.Authors
{
    public class ZephyrScaleAuthor : BaseAuthor, IDisposable
    {
        enum Zephyr
        {
            [Api("/testcases")] GetTests,
            [Api("/environments")] Environments,
            [Api("/testcycles")] TestCycle,
        }
        //file all api calls through the same method for throttling as needed
        //use var to manage the throttling time
        //look at pulling the 'level of parallelism' to dynamically set this value
        //possibly look at having a timer/counter/var to track the last time an endpoint was hit to delay only when needed

        //get list of test case names and ids from tool
        //Create an object to hold this data, other 'tag' data, current test steps, and possibly the new test steps

        //If new test, create the new test
        //do this in bunches if possible

        //Check if test needs to be updated
        //do this in bunches if possible

        //Update test results periodically during run
        //somehow check for 'low' times to make this call?

        //TestExecutioner needs to be able to send data in
        //ensure a thread-safe way of doing this

        private readonly string ProjectKey = Config.GetConfigSetting("ProjectKey");
        private readonly string ZephyrBase = Config.GetConfigSetting("ZephyrBase");
        private readonly string ZephyrToken = Config.GetConfigSetting("ZephyrToken");
        //TODO look at using folder name instead of id
        private readonly string ZephyrFolder = Config.GetConfigSetting("ZephyrFolderId");
        private readonly Api ApiHelp;
        private List<ZephyrData> TestCases = new List<ZephyrData>();

        public ZephyrScaleAuthor()
        {
            ApiHelp = new Api(null, ZephyrBase);
            //setup authorization
        }

        public override string SaveReport()
        {
            //close run
            //complete any remaining items

            return null;
        }

        public override bool StartNewTestCase(TestCaseHeaderData headerData) //complete test case???
        {
            throw new NotImplementedException();
        }

        private void GetTestCases()
        {
            var folder = string.IsNullOrEmpty(ZephyrFolder) ? "" : $"&folderId={ZephyrFolder}";
            var rsp = ApiHelp.GetCall(Zephyr.GetTests, $"?projectKey={ProjectKey}&maxResults=999999999{folder}");
            var tests = ApiHelper.GetRspList(rsp.values);

            //TODO determine what data is actually needed for an update
            foreach(var test in tests)
            {
                var data = new ZephyrData() { Id = test.id, Name = test.name, TestData = test };
                //call 'get test script' to get the test steps
                rsp = ApiHelp.GetCall(Zephyr.GetTests, $"/{test.key}/teststeps");

                data.StepData = ApiHelper.GetRspList(rsp.values);

                TestCases.Add(data);
            }
        }

        private void CreateTestRun()
        {
            var envId = -1;
            //get environments and get id that matches current env if a match is found
            var rsp = ApiHelp.GetCall(Zephyr.Environments, $"?projectKey={ProjectKey}");

            foreach(var env in ApiHelper.GetRspList(rsp.values))
            {
                if (Config.GetEnvironment().ToLower().Equals(Convert.ToString(env.name)))
                {
                    envId = env.id;
                    break;
                }
            }

            ApiHelp.PostCall(Zephyr.TestCycle, new { projectKey = ProjectKey, name = "New Test name" }, "");
            //TODO make post call
        }

        private void CreateNewTestCase()
        {

        }

        private void UpdateTestCase()
        {
            //check test case for general attributes??
            //call 'Post test steps' to update test steps
        }

        private void CheckIfTestNeedsUpdating()
        {

        }

        private void UpdateResults()
        {

        }
    }
    
    class ZephyrData
    {
        public int Id;
        public string Name;
        public dynamic TestData;
        public List<dynamic> StepData;

        public int CheckStepDiff(List<List<TestCaseStep>> testSteps)
        {

            return 0;
        }
    }
}
