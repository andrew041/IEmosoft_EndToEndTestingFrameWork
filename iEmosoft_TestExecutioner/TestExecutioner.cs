using aUI.Automation.BaseClasses;
using aUI.Automation.Elements;
using aUI.Automation.HelperObjects;
using aUI.Automation.Interfaces;
using aUI.Automation.ModelObjects;
using aUI.Automation.Test.IEmosoft.com;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace aUI.Automation
{
    public class TestExecutioner : IDisposable
    {
        private IUIDriver UiDriver = null;
        private IScreenCapture ScreenCapture = null;
        public BaseAuthor TestAuthor = null;
        private bool ReportingEnabled = true;
        private bool TestPassed = true;
        public DateTime StartTime { get; private set; }
        public DateTime TestTimeLimit { get; private set; } = DateTime.Now.AddHours(12);
        public DateTime? DisposeTime { get; private set; } = null;
        private List<string> AllTestFiles = new();
        private PoolState poolState = new() { IsAvailable = true, IsPartOfTestExecutionerPool = false };
        private bool ProcessTestResultCalled = false;
        private Config Config = new();
        public RandomTestData Rand = new();
        public ElementActions Action;
        public AssertHelp Assert;
        public TestContext.ResultAdapter NUnitResult;

        public delegate void ReportSavedEventCallback_Delegate(string locationOfReport);
        public ReportSavedEventCallback_Delegate reportSavedCallback { get; set; }

        public TestExecutioner(string testCaseNumber, string testCaseName = "", IUIDriver uiDriver = null, BaseAuthor author = null, IScreenCapture capture = null)
        {
            var testCaseHeader = new TestCaseHeaderData()
            {
                ExecutedByName = "Mike Emo Automation Test Executioner",
                ExecutedOnDate = DateTime.Now.ToShortDateString(),
                TestName = testCaseName.Replace("_", " "),
                TestNumber = testCaseNumber,
                TestWriter = "Mike Emo Automation Test Executioner"
            };

            Initialize(testCaseHeader, uiDriver, author, capture);
            SetDefaultMaxTime();
            Assert = new AssertHelp(this);
            Action = new ElementActions(this);
        }

        public TestExecutioner(TestCaseHeaderData testCaseHeader, IUIDriver uiDriver = null, BaseAuthor author = null, IScreenCapture capture = null)
        {
            Initialize(testCaseHeader, uiDriver, author, capture);
            SetDefaultMaxTime();
            Assert = new AssertHelp(this);
            Action = new ElementActions(this);
        }

        public TestExecutioner(TestCaseHeaderData testCaseHeader, bool apiTest)
        {
            if (apiTest)
            {
                //TODO Modify Dispose to deal with null test author
                //TODO deal with null driver on screenshot failures
                var factory = new AutomationFactory();
                if (Directory.Exists(factory.Configuration.TestReportFilePath))
                {
                    Directory.Delete(factory.Configuration.TestReportFilePath, true);
                }

                TestAuthor = factory.CreateAuthor();
                TestAuthor.StartNewTestCase(testCaseHeader);

                StartTime = DateTime.Now;
            }
            else
            {
                Initialize(testCaseHeader, null, null, null);
            }

            SetDefaultMaxTime();
            Assert = new AssertHelp(this);
            
            if (!apiTest)
            {
                Action = new ElementActions(this);
            }
        }

        public TestExecutioner(bool useConfigFile = true)
        {
            ReportingEnabled = false;
            if (useConfigFile)
            {
                Initialize(null, null, null, null);
            }
            else
            {
                UiDriver = new UIDrivers.BrowserDriver(new Config(), UIDrivers.BrowserDriver.BrowserDriverEnumeration.Firefox);
            }
            SetDefaultMaxTime();
            Assert = new AssertHelp(this);
            Action = new ElementActions(this);
        }

        public void SetMaxTime(int min = 10, int seconds = 0)
        {
            TestTimeLimit = StartTime.AddMinutes(min).AddSeconds(seconds);
        }

        private void SetDefaultMaxTime()
        {
            var timeM = Config.GetConfigSetting("TestTimeLimitMin", "0");
            var timeS = Config.GetConfigSetting("TestTimeLimitSec", "0");
            var tm = int.TryParse(timeM, out int tmv);
            var ts = int.TryParse(timeS, out int tsv);

            if (timeM.Equals("0") && timeS.Equals("0") || (!tm && !ts))
            {
                SetMaxTime(720, 0);
            }
            else
            {
                SetMaxTime(tmv, tsv);
            }
        }

        public PoolState PoolState
        {
            get
            {
                return poolState;
            }
            set
            {
                poolState = value;
            }
        }

        public bool LoadJQuery()
        {
            try
            {
                string script = "if (!window.jQuery) { var jq = document.createElement('script'); jq.type = 'text/javascript'; jq.src = 'https://code.jquery.com/jquery-2.2.1.min.js'; document.getElementsByTagName('head')[0].appendChild(jq);}";
                ExecuteJavaScript(script);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Initialize(TestCaseHeaderData testCaseHeader, IUIDriver injectedDriver, BaseAuthor author, IScreenCapture capture)
        {
            var factory = new AutomationFactory();

            UiDriver = injectedDriver ?? factory.CreateUIDriver();

            if (testCaseHeader != null)
            {
                if (Directory.Exists(factory.Configuration.TestReportFilePath))
                {
                    Directory.Delete(factory.Configuration.TestReportFilePath, true);
                }

                if (capture != null)
                {
                    ScreenCapture = capture;
                }
                else
                {
                    ScreenCapture = factory.CreateScreenCapturer(UiDriver);
                }

                TestAuthor = author ?? factory.CreateAuthor();
                TestAuthor.StartNewTestCase(testCaseHeader);
            }

            StartTime = DateTime.Now;
        }

        public void FireChangeEvent(UIQuery query)
        {
            FireChangeEvent(query.AttributeName, query.AttributeValue, query.ControlTypeName);
        }

        public void FireChangeEvent(string attributeNameOrElementId, string attributeValue = "", string elementName = "")
        {
            string script = "";

            if (attributeValue.isNull())
            {
                script = "#" + attributeNameOrElementId;
            }
            else
            {
                if (elementName != "")
                {
                    //eg. $('input [ng-model*="firstName"]')
                    script = elementName + " ";
                }

                script += string.Format("[{0}*=\"{1}\"]", attributeNameOrElementId, attributeValue);
            }

            script = "$('" + script + "').change()";
            ExecuteJavaScript(script);
        }

        public void FireChangeEvent(JQuerySelector selector)
        {
            var newId = CreateRandomIdAttributeOnSelector(selector);
            FireChangeEvent(newId);
        }

        public void RefreshWebPage()
        {
            RawSeleniumWebDriver_AvoidCallingDirectly.Navigate().Refresh();
        }

        public void WaitForDownload(string path)
        {
            for (var i = 0; i < 10; i++)
            {
                if (File.Exists(path)) { break; }
                Pause(1000);
            }
            var length = new FileInfo(path).Length;
            for (var i = 0; i < 30; i++)
            {
                Pause(750);
                var newLength = new FileInfo(path).Length;
                if (newLength == length && length != 0) { break; }
                length = newLength;
            }
        }

        public void WaitForAjaxCalls(int wait = 15)
        {
            var start = DateTime.Now;
            var done = false;

            while(!done && DateTime.Now.Subtract(start).TotalSeconds < wait)
            {
                try
                {
                    done = !(bool)ExecuteJavaScript("return $(\"body[block-ui='main']\").hasClass(\"block-ui-visible\")");
                }
                catch { return; }
            }
        }

        public object ExecuteJavaScript(string script)
        {
            var fireFoxDriver = UiDriver as aUI.Automation.UIDrivers.BrowserDriver;
            return fireFoxDriver.RawWebDriver.ExecuteScript(script);
        }

        public BugCreator BugCreator { get; set; }

        public string CurrentFormName_OrURL { get { return UiDriver.CurrentFormName_OrPageURL; } }

        public void Pause(int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }

        public IWebDriver RawSeleniumWebDriver_AvoidCallingDirectly
        {
            get
            {
                var fireFox = UiDriver as aUI.Automation.UIDrivers.BrowserDriver;
                return fireFox.RawWebDriver;
            }
        }

        public void ClickAlert(bool clickOK)
        {
            var alert = GetAlert();

            if (alert != null)
            {
                if (clickOK)
                {
                    alert.Accept();
                }
                else
                {
                    alert.Dismiss();
                }
            }
        }

        public string GetAlertText()
        {
            var alert = GetAlert();
            if (alert != null)
            {
                return alert.Text;
            }

            return null;
        }

        private IAlert GetAlert()
        {
            var wait = new WebDriverWait(RawSeleniumWebDriver_AvoidCallingDirectly, new TimeSpan(0, 0, 5));
            wait.Until(ExpectedConditions.AlertIsPresent());
            return RawSeleniumWebDriver_AvoidCallingDirectly.SwitchTo().Alert();
        }

        public void NavigateTo(string url, string expectedResult = "")
        {
            if (!string.IsNullOrEmpty(expectedResult) && ReportingEnabled)
            {
                BeginTestCaseStep("Navigate to " + url, expectedResult);
            }

            UiDriver.MaximizeWindow();
            UiDriver.NavigateTo(url);
        }

        public void Quit()
        {
            if (UiDriver != null)
            {
                UiDriver.Dispose();
            }
        }

        public string CaptureScreen(string textToWriteOnScreenCapture = "")
        {
            if (ScreenCapture == null || ReportingEnabled == false)
            {
                return null;
            }
            else if (TestAuthor.CurrentStep != null)
            {
                var fileName = ScreenCapture.NewFileName;
                ScreenCapture.CaptureDesktop(fileName, textToWriteOnScreenCapture);
                TestAuthor.CurrentStep.ImageFilePath = fileName;
                TestAuthor.CurrentStep.ImageData = ScreenCapture.LastImageCapturedAsByteArray;
                AllTestFiles.Add(fileName);
                return fileName;
            }
            else
            {
                return null;
            }

            /*
            string fileName = screenCapture.NewFileName;

            var process = Process.GetProcesses().FirstOrDefault(x => x.MainWindowTitle.Contains(RawSeleniumWebDriver_AvoidCallingDirectly.Title));

            if (process != null)
            {
                BringToFront(process.MainWindowHandle);
            }

            uiDriver.MaximizeWindow();

            screenCapture.CaptureDesktop(fileName, textToWriteOnScreenCapture);
            if (testAuthor != null && testAuthor.CurrentStep != null)
            {
                testAuthor.CurrentStep.ImageFilePath = fileName;
                testAuthor.CurrentStep.ImageData = screenCapture.LastImageCapturedAsByteArray;
            }

            allTestFiles.Add(fileName);
            return fileName;
            */
        }

        public bool TestCaseFailed
        {
            get {
                var initialStatus = TestAuthor != null ? TestAuthor.TestCaseFailed : false;
                var nunit = TestContext.CurrentContext.Result.FailCount > 0;
                var nothing = TestAuthor.RecordedSteps.Count == 0;
                return initialStatus || nunit || nothing;
            }
        }

        public string TestStackTrace
        {
            get {
                return TestContext.CurrentContext.Result.StackTrace;
            }
        }

        public Exception FailCurrentStep(Exception unexpectedException)
        {
            if (CurrentStep == null)
            {
                FailTest(unexpectedException);
            }
            else
            {
                string msg = "An Unexpected error occurred. " + unexpectedException.ToDeepMessage();
                FailCurrentStep(null, msg, true);
                Assert.True(false, msg);
            }

            return unexpectedException;

        }
        public void FailCurrentStep(string expectedResult, string actualResult, bool isShowStoppingError = false)
        {
            TestPassed = false;
            var currentStep = CurrentStep;
            if (currentStep != null)
            {
                currentStep.StepPassed = false;

                if (!expectedResult.isNull())
                {
                    currentStep.ExpectedResult = expectedResult;
                }

                if (!actualResult.isNull())
                {
                    currentStep.ActualResult = actualResult;
                }

                CaptureScreen(actualResult);
            }

            if (isShowStoppingError)
            {
                Assert.True(false, string.Format("Expected: {0}, Actual: {1}", expectedResult, actualResult));
                Assert.True(false, string.Format("Expected: {0}, Actual: {1}", expectedResult, actualResult));
            }
        }

        public void BeginTestCaseStep(string stepDescription, string expectedResult = "", string actualResult = "", bool captureImage = false)
        {
            stepDescription = stepDescription.Replace(Environment.NewLine, "").Replace("\"","");
            if (TestAuthor != null)
            {
                TestAuthor.BeginTestCaseStep(stepDescription, expectedResult, actualResult);
            }

            if (captureImage || Config.RecordAllSteps) //add second parameter
            {
                CaptureScreen();
            }
        }

        public void AssociateBug(string bugNumber)
        {
            TestAuthor.AssociateBug(bugNumber);
        }

        public void DisassociateBug(string bugNumber)
        {
            TestAuthor.DisassociateBug(bugNumber);
        }

        public TestCaseStep CurrentStep
        {
            get
            {
                return TestAuthor?.CurrentStep;
            }
        }

        public void ReleaseFromPool()
        {
            if (PoolState.IsPartOfTestExecutionerPool)
            {
                ProcessTestResults();
                PoolState.WasAlreadyInPool = true;

                if (!PoolState.LangingPageURL.isNull())
                {
                    NavigateTo(PoolState.LangingPageURL);
                }
                PoolState.IsAvailable = true;
            }
            else
            {
                Dispose();
            }
        }

        public string WriteReport()
        {
            return ProcessTestResults();
        }

        private string ProcessTestResults()
        {
            string reportFile = null;
            DisposeTime = DateTime.Now;

            if (TestAuthor != null)
            {
                if (ReportingEnabled && Config.TestReportFilePath.IsNotNull())
                {
                    reportFile = TestAuthor.SaveReport();
                    AllTestFiles.Add(reportFile);

                    if (reportSavedCallback != null)
                    {
                        reportSavedCallback(reportFile);
                    }

                }
                TestAuthor.Dispose();
            }

            if (BugCreator != null)
                BugCreator.Dispose();

            string ftpReportPath = FTPReport();
            if (!ftpReportPath.isNull())
            {
                CallRestService(ftpReportPath);
            }

            ProcessTestResultCalled = true;
            return reportFile;
        }

        private string FTPReport()
        {
            try
            {
                var uploader = new ReportUploader(Config);
                var testName = TestCaseHeader.TestName;
                return uploader.UploadReport(testName, AllTestFiles);
            }
            catch
            {
                return null;
            }
        }

        public static bool FailTestBeforeItEvenRan(TestCaseHeaderData testCaseHeader, string reason)
        {
            try
            {
                using (var e = new TestExecutioner(testCaseHeader))
                {
                    e.BeginTestCaseStep("Unable to launch test, test failed before it began");
                    e.FailCurrentStep("", reason, true);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void CallRestService(string ftpReportPath)
        {
            var dto = new TestRunDTO()
            {
                ApplicationId = Config.ApplicationUnderTest,
                FTPPath = ftpReportPath,
                Status = TestPassed ? (int)TestRunDTO.TestRunStatusEnumeration.Passed : (int)TestRunDTO.TestRunStatusEnumeration.Failed,
                TestNumber = TestCaseHeader.TestNumber,
                TestTime = (DateTime)DisposeTime - StartTime,
                RunDate = StartTime
            };

            RestClient restClient = null;

            try
            {
                restClient = new RestClient();
                restClient.RecordTestRun(dto);
            }
            catch (Exception exp)
            {
                if (exp.Message.Contains("Unable to find test number"))
                {
                    restClient.RegisterTest(dto.TestNumber, TestCaseHeader.TestFamily, TestCaseHeader.TestName, TestCaseHeader.TestDescription, DateTime.Now);
                    Pause(3000);
                    restClient.RecordTestRun(dto);
                }
            }
        }

        public bool WaitForURLChange(string urlSnippet, bool endsWith = false, int waitSeconds = 20)
        {
            bool result = false;

            for (int i = 0; i <= (waitSeconds * 2); i++)
            {
                if ((CurrentFormName_OrURL.ToLower().Contains(urlSnippet.ToLower()) && !endsWith) || (endsWith && CurrentFormName_OrURL.ToLower().EndsWith(urlSnippet.ToLower())))
                {
                    result = true;
                    break;
                }

                Pause(500);
            }

            return result;
        }

        public bool PageContains(string lookFor)
        {
            try
            {
                var element = UiDriver.RawWebDriver.FindElement(By.TagName("body"));
            }
            catch { };

            return UiDriver.ScreenContains(lookFor);
        }

        public void AssertPageContains(string lookFor, bool continueIfFails = false)
        {
            TestAuthor.BeginTestCaseStep(string.Format("Verify page contains string '{0}'", lookFor));

            if (!PageContains(lookFor))
            {
                string msg = string.Format("Unable to find '{0}' on current page, see image for details.", lookFor);

                if (TestAuthor.CurrentStep.ActualResult.IsNull() == false && TestAuthor.CurrentStep.ActualResult.Length > 0)
                    TestAuthor.CurrentStep.ActualResult += "  ";

                TestAuthor.CurrentStep.ActualResult = msg;
                TestAuthor.CurrentStep.StepPassed = false;

                //captuer the screen with an error message
                CaptureScreen(msg);

                if (!continueIfFails)
                {
                    throw new Exception(msg);
                }
                TestPassed = false;
            }
            else
            {
                //No error message, capture the screen (with no error message);
                CaptureScreen();
            }
        }


        public bool AmOnScreen(string urlSnippet)
        {
            return UiDriver.AmOnSceen(urlSnippet);
        }

        public void AssertAmOnScreen(string urlSnippet)
        {
            if (!AmOnScreen(urlSnippet))
            {
                if (TestAuthor.CurrentStep != null)
                {
                    if (TestAuthor.CurrentStep.ActualResult.Length > 0)
                        TestAuthor.CurrentStep.ActualResult += "  ";

                    TestAuthor.CurrentStep.ActualResult = string.Format("Am not on the expected page, url does not contain '{0}'", urlSnippet);
                    TestAuthor.CurrentStep.StepPassed = false;
                }

                TestPassed = false;
                throw new Exception("Am not on the expected page.  Url does not contain '" + urlSnippet + "'");
            }
        }

        public void AssertPageNotContain(string lookFor)
        {
            if (PageContains(lookFor))
            {
                string msg = string.Format("Page contains text ('{0}'), that should not exist, we may not be on the page expected.", lookFor);

                if (TestAuthor.CurrentStep != null)
                {
                    if (TestAuthor.CurrentStep.ActualResult.Length > 0)
                        TestAuthor.CurrentStep.ActualResult += "  ";

                    TestAuthor.CurrentStep.ActualResult = msg;
                    TestAuthor.CurrentStep.StepPassed = false;
                }

                TestPassed = false;
                throw new Exception(msg);
            }
        }

        public void SwitchWindows(bool first = true)
        {
            var window = RawSeleniumWebDriver_AvoidCallingDirectly.WindowHandles[^1];
            
            if (first)
            {
                window = RawSeleniumWebDriver_AvoidCallingDirectly.WindowHandles[0];
            }

            RawSeleniumWebDriver_AvoidCallingDirectly.SwitchTo().Window(window);
        }

        public void CloseWindow()
        {
            RawSeleniumWebDriver_AvoidCallingDirectly.Close();
            SwitchWindows(false);
        }

        public List<TestCaseStep> RecordedSteps
        {
            get { return TestAuthor.RecordedSteps; }
        }

        public void StartNewTestCase(TestCaseHeaderData header)
        {
            if (header != null)
            {
                var factory = new AutomationFactory();
                if (ScreenCapture == null)
                {
                    ScreenCapture = factory.CreateScreenCapturer(UiDriver);
                }

                if (TestAuthor == null)
                {
                    TestAuthor = factory.CreateAuthor();
                    TestAuthor.StartNewTestCase(header);
                }

                StartTime = DateTime.Now;
                ProcessTestResultCalled = false;
            }
        }

        public TestCaseHeaderData TestCaseHeader
        {
            get { return TestAuthor.TestCaseHeader; }
        }

        private string CreateRandomIdAttributeOnSelector(JQuerySelector seletor)
        {
            string newId = Guid.NewGuid().ToString().Replace("-", "");
            string jqSelector = seletor.JQuerySelectorScript;

            if (!jqSelector.StartsWith("$"))
            {
                jqSelector = jqSelector.Replace("'", "\"");
                jqSelector = "$('" + jqSelector + "')";
            }
            string script = string.Format("{0}.attr('id', '{1}')", jqSelector, newId);

            ExecuteJavaScript(script);
            return newId;
        }

        private bool IsAngularApp
        {
            get
            {
                try
                {
                    string script = "return angular == null ? 'FALSE' : 'TRUE'";
                    object result = ExecuteJavaScript(script);

                    return result.ToString() == "TRUE";
                }
                catch
                {
                    return false;
                }
            }
        }

        private string GetNgModelAttribute(string idOrCss)
        {
            var element = RawSeleniumWebDriver_AvoidCallingDirectly.MineForElement(idOrCss);
            string ngModelValue = element.GetAttribute("ng-model");

            return ngModelValue;
        }

        public Exception FailTest(Exception exp)
        {
            BeginTestCaseStep("An expected error occurred", "", "");
            CurrentStep.ActualResult = exp.Message;
            CurrentStep.StepPassed = false;
            TestPassed = false;
            return exp;
        }

        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(String lpClassName, String lpWindowName);

        [DllImport("USER32.DLL")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static void BringToFront(IntPtr handle)
        {
            // Make Calculator the foreground application
            SetForegroundWindow(handle);
        }

        public void NavBack()
        {
            TestAuthor.BeginTestCaseStep("Navigate back to the prior page");
            RawSeleniumWebDriver_AvoidCallingDirectly.Navigate().Back();
        }

        //TODO get dropdown values

        #region Element Helper Methods
        #region Singles
        public ElementResult UploadImage(ElementObject ele)
        {
            ele.Action = ElementAction.Wait;
            var temp = Action.ExecuteAction(ele);
            
            try
            {
                temp.RawEle.SendKeys(ele.Text);
            }
            catch
            {
                temp.Success = false;
            }

            return temp;
        }

        public ElementResult UploadImage(Enum ele, string path)
        {
            var obj = new ElementObject(ele, path) { Action = ElementAction.Wait, WaitType = Wait.Presence };
            return UploadImage(obj);
        }

        //TODO Add wait for element and/or 'get element'
        public ElementResult Click(ElementObject ele)
        {
            ele.Action = ElementAction.Click;
            return Action.ExecuteAction(ele);
        }

        public ElementResult Click(Enum ele)
        {
            var obj = new ElementObject(ele) { Action = ElementAction.Click, WaitType = Wait.Clickable };
            return Action.ExecuteAction(obj);
        }
        public ElementResult Hover(ElementObject ele)
        {
            ele.Action = ElementAction.Hover;
            return Action.ExecuteAction(ele);
        }

        public ElementResult Hover(Enum ele)
        {
            var obj = new ElementObject(ele) { Action = ElementAction.Hover };
            return Action.ExecuteAction(obj);
        }

        public ElementResult EnterText(ElementObject ele)
        {
            ele.Action = ElementAction.EnterText;
            return Action.ExecuteAction(ele);
        }

        public ElementResult EnterText(Enum ele, string text = "", int random = -1)
        {
            var obj = new ElementObject(ele, text) { Action = ElementAction.EnterText, RandomLength = random, Random = random > 0 };
            return Action.ExecuteAction(obj);
        }

        public ElementResult Dropdown(ElementObject ele)
        {
            ele.Action = ElementAction.Dropdown;
            return Action.ExecuteAction(ele);
        }

        public ElementResult Dropdown(Enum ele, string text = "", int random = -1)
        {
            var obj = new ElementObject(ele, text) { Action = ElementAction.Dropdown, RandomLength = random, Random = random > 0 };
            return Action.ExecuteAction(obj);
        }

        public ElementResult DropdownIndex(ElementObject ele)
        {
            ele.Action = ElementAction.DropdownIndex;
            return Action.ExecuteAction(ele);
        }

        public ElementResult DropdownIndex(Enum ele, int index = 0)
        {
            var obj = new ElementObject(ele) { Action = ElementAction.DropdownIndex, Text = index.ToString() };
            return Action.ExecuteAction(obj);
        }

        public ElementResult RadioBtn(ElementObject ele)
        {
            ele.Action = ElementAction.RadioBtn;
            return Action.ExecuteAction(ele);
        }

        public ElementResult RadioBtn(Enum ele, string text = "", int random = -1)
        {
            var obj = new ElementObject(ele, text) { Action = ElementAction.RadioBtn, RandomLength = random, Random = random > 0 };
            return Action.ExecuteAction(obj);
        }

        public ElementResult MultiDropdown(ElementObject ele)
        {
            ele.Action = ElementAction.MultiDropdown;
            return Action.ExecuteAction(ele);
        }

        public ElementResult MultiDropdown(Enum ele, string text = "", bool random = false)
        {
            var obj = new ElementObject(ele, text) { Action = ElementAction.MultiDropdown, Random = random };
            return Action.ExecuteAction(obj);
        }

        public ElementResult GetText(ElementObject ele)
        {
            ele.Action = ElementAction.GetText;
            return Action.ExecuteAction(ele);
        }

        public ElementResult GetText(Enum ele)
        {
            var obj = new ElementObject(ele) { Action = ElementAction.GetText };
            return Action.ExecuteAction(obj);
        }

        public ElementResult GetDropdown(ElementObject ele)
        {
            ele.Action = ElementAction.GetDropdown;
            return Action.ExecuteAction(ele);
        }

        public ElementResult GetDropdown(Enum ele)
        {
            var obj = new ElementObject(ele) { Action = ElementAction.GetDropdown };
            return Action.ExecuteAction(obj);
        }

        public ElementResult GetCheckbox(ElementObject ele)
        {
            ele.Action = ElementAction.GetCheckbox;
            return Action.ExecuteAction(ele);
        }

        public ElementResult GetCheckbox(Enum ele)
        {
            var obj = new ElementObject(ele) { Action = ElementAction.GetCheckbox };
            return Action.ExecuteAction(obj);
        }

        public ElementResult GetAttribute(ElementObject ele)
        {
            ele.Action = ElementAction.GetAttribute;
            return Action.ExecuteAction(ele);
        }

        public ElementResult GetAttribute(Enum ele, string text = "")
        {
            var obj = new ElementObject(ele, text) { Action = ElementAction.GetAttribute };
            return Action.ExecuteAction(obj);
        }

        public ElementResult GetCSS(ElementObject ele)
        {
            ele.Action = ElementAction.GetCSS;
            return Action.ExecuteAction(ele);
        }

        public ElementResult GetCSS(Enum ele, string text = "")
        {
            var obj = new ElementObject(ele, text) { Action = ElementAction.GetCSS };
            return Action.ExecuteAction(obj);
        }

        public ElementResult GetProperty(ElementObject ele)
        {
            ele.Action = ElementAction.GetProperty;
            return Action.ExecuteAction(ele);
        }

        public ElementResult GetProperty(Enum ele, string text = "")
        {
            var obj = new ElementObject(ele, text) { Action = ElementAction.GetProperty };
            return Action.ExecuteAction(obj);
        }
        public ElementResult WaitFor(ElementObject ele, Wait? wait = null, int maxWait = -1)
        {
            ele.Action = ElementAction.Wait;
            if (wait.HasValue)
            {
                ele.WaitType = (Wait)wait;
            }
            if (maxWait >= 0)
            {
                ele.MaxWait = maxWait;
            }
            return Action.ExecuteAction(ele);
        }

        public ElementResult WaitFor(Enum ele, Wait wait = Wait.Visible, int maxWait = 10)
        {
            var obj = new ElementObject(ele) { Action = ElementAction.Wait, WaitType = wait, MaxWait = maxWait };
            return Action.ExecuteAction(obj);
        }
        #endregion

        #region Multi's
        public List<ElementResult> ClickAll(ElementObject ele)
        {
            ele.Action = ElementAction.Click;
            return Action.ExecuteActions(ele);
        }

        public List<ElementResult> ClickAll(Enum ele)
        {
            var obj = new ElementObject(ele) { Action = ElementAction.Click };
            return Action.ExecuteActions(obj);
        }

        public List<ElementResult> EnterTextAll(ElementObject ele)
        {
            ele.Action = ElementAction.EnterText;
            return Action.ExecuteActions(ele);
        }

        public List<ElementResult> EnterTextAll(Enum ele, string text = "", int random = -1)
        {
            var obj = new ElementObject(ele, text) { Action = ElementAction.EnterText, RandomLength = random, Random = random > 0 };
            return Action.ExecuteActions(obj);
        }

        public List<ElementResult> DropdownAll(ElementObject ele)
        {
            ele.Action = ElementAction.Dropdown;
            return Action.ExecuteActions(ele);
        }

        public List<ElementResult> DropdownAll(Enum ele, string text = "", bool random = false)
        {
            var obj = new ElementObject(ele) { Action = ElementAction.Dropdown, Text = text, Random = random };
            return Action.ExecuteActions(obj);
        }

        public List<ElementResult> DropdownIndexAll(ElementObject ele)
        {
            ele.Action = ElementAction.DropdownIndex;
            return Action.ExecuteActions(ele);
        }

        public List<ElementResult> DropdownIndexAll(Enum ele, int index = 0)
        {
            var obj = new ElementObject(ele) { Action = ElementAction.DropdownIndex, Text = index.ToString() };
            return Action.ExecuteActions(obj);
        }

        public List<ElementResult> RadioBtnAll(ElementObject ele)
        {
            ele.Action = ElementAction.RadioBtn;
            return Action.ExecuteActions(ele);
        }

        public List<ElementResult> RadioBtnAll(Enum ele, string text = "", bool random = false)
        {
            var obj = new ElementObject(ele, text) { Action = ElementAction.RadioBtn, Random = random };
            return Action.ExecuteActions(obj);
        }

        public List<ElementResult> MultiDropdownAll(ElementObject ele)
        {
            ele.Action = ElementAction.MultiDropdown;
            return Action.ExecuteActions(ele);
        }

        public List<ElementResult> MultiDropdownAll(Enum ele, string text = "", bool random = false)
        {
            var obj = new ElementObject(ele, text) { Action = ElementAction.MultiDropdown, Random = random };
            return Action.ExecuteActions(obj);
        }

        public List<ElementResult> GetAllText(ElementObject ele)
        {
            ele.Action = ElementAction.GetText;
            return Action.ExecuteActions(ele);
        }

        public List<ElementResult> GetAllText(Enum ele)
        {
            var obj = new ElementObject(ele) { Action = ElementAction.GetText };
            return Action.ExecuteActions(obj);
        }

        public List<ElementResult> GetCheckboxes(ElementObject ele)
        {
            ele.Action = ElementAction.GetCheckbox;
            return Action.ExecuteActions(ele);
        }

        public List<ElementResult> GetCheckboxes(Enum ele)
        {
            var obj = new ElementObject(ele) { Action = ElementAction.GetCheckbox };
            return Action.ExecuteActions(obj);
        }

        public List<ElementResult> GetAttributes(ElementObject ele)
        {
            ele.Action = ElementAction.GetAttribute;
            return Action.ExecuteActions(ele);
        }

        public List<ElementResult> GetAttributes(Enum ele, string text = "")
        {
            var obj = new ElementObject(ele) { Action = ElementAction.GetAttribute, Text = text };
            return Action.ExecuteActions(obj);
        }

        public List<ElementResult> GetCSSes(ElementObject ele)
        {
            ele.Action = ElementAction.GetCSS;
            return Action.ExecuteActions(ele);
        }

        public List<ElementResult> GetCSSes(Enum ele, string text = "")
        {
            var obj = new ElementObject(ele) { Action = ElementAction.GetCSS, Text = text };
            return Action.ExecuteActions(obj);
        }

        public List<ElementResult> GetProperties(ElementObject ele)
        {
            ele.Action = ElementAction.GetProperty;
            return Action.ExecuteActions(ele);
        }

        public List<ElementResult> GetProperties(Enum ele, string text = "")
        {
            var obj = new ElementObject(ele, text) { Action = ElementAction.GetProperty };
            return Action.ExecuteActions(obj);
        }

        public List<ElementResult> WaitForAll(ElementObject ele, Wait wait = Wait.Visible)
        {
            ele.Action = ElementAction.Wait;
            ele.WaitType = wait;
            return Action.ExecuteActions(ele);
        }

        public List<ElementResult> WaitForAll(Enum ele, Wait wait = Wait.Visible)
        {
            var obj = new ElementObject(ele) { Action = ElementAction.Wait, WaitType = wait };
            return Action.ExecuteActions(obj);
        }
        #endregion

        #region Specialty Element Helpers
        public (List<ElementResult> header, List<List<ElementResult>> body) ReadTable(Enum tableRef, bool scroll = false, ElementResult start = null)
        {
            var table = WaitFor(tableRef);
            if(start != null)
            {
                table = start.WaitFor(tableRef);
            }

            var headers = table.GetTexts(new ElementObject(ElementType.Tag, "th"));

            var body = table.WaitFor(new ElementObject(ElementType.Tag, "tbody") { Scroll = scroll, ScrollLoc = "start", MaxWait = 0 });
            var rows = body.WaitForAll(new ElementObject(ElementType.Tag, "tr") { Scroll = scroll, ScrollLoc = "start", MaxWait = 0 });//set scroll to false
            var tableBody = new List<List<ElementResult>>();

            foreach (var row in rows)
            {
                var cells = row.GetTexts(new ElementObject(ElementType.Tag, "td") { Scroll = scroll, ScrollLoc = "start", MaxWait = 0 });
                tableBody.Add(cells);
            }

            return (headers, tableBody);
        }

        public (List<ElementResult> header, List<List<ElementResult>> body) ReadTableColumn(Enum tableRef, string header, bool scroll = false)
        {
            var table = WaitFor(tableRef);
            var headers = table.GetTexts(new ElementObject(ElementType.Tag, "th"));

            var index = headers.FindIndex(x => x.Text.ToLower().Equals(header.ToLower()));

            if(index < 0)
            {
                return (null, null);
            }

            var body = table.WaitFor(new ElementObject(ElementType.Tag, "tbody") { Scroll = scroll, ScrollLoc = "start", MaxWait = 0 });
            var rows = body.WaitForAll(new ElementObject(ElementType.Tag, "tr") { Scroll = scroll, ScrollLoc = "start", MaxWait = 0 });//set scroll to false
            var tableBody = new List<List<ElementResult>>();

            foreach (var row in rows)
            {
                var cells = row.WaitForAll(new ElementObject(ElementType.Tag, "td") { Scroll = scroll, ScrollLoc = "start", MaxWait = 0 });
                tableBody.Add(new List<ElementResult>() { cells[index].GetText(new ElementObject() { Scroll = false, MaxWait = 0 }) });
            }

            return (new List<ElementResult>() { headers[index] }, tableBody);
        }
        #endregion
        #endregion

        public void FailLastStepIfFailureNotTriggered()
        {
            try
            {
                NUnitResult = TestContext.CurrentContext.Result;
                if (NUnitResult.FailCount > 0 || (NUnitResult.PassCount == 0 && NUnitResult.SkipCount == 0))
                {
                    if (CurrentStep == null)
                    {
                        FailTest(new Exception("Test failed before any steps were initiated."));
                    }
                    else if (CurrentStep.StepPassed)
                    {
                        CurrentStep.StepPassed = false;
                        CurrentStep.ActualResult = $"Step failed due to: {NUnitResult.Message}";
                        if (UiDriver.RawWebDriver != null && UiDriver.RawWebDriver.WindowHandles.Count > 0)
                        {
                            CaptureScreen("");
                        }
                    }
                }
            }
            catch { }
        }

        public void Dispose()
        {
            try
            {
                NUnitResult = TestContext.CurrentContext.Result;
                if (NUnitResult.FailCount > 0 || (NUnitResult.PassCount == 0 && NUnitResult.SkipCount == 0))
                {
                    if (CurrentStep == null)
                    {
                        FailTest(new Exception("Test failed before any steps were initiated."));
                    }
                    else if (CurrentStep.StepPassed)
                    {
                        CurrentStep.StepPassed = false;
                        CurrentStep.ActualResult = $"Step failed due to: {NUnitResult.Message}";
                        if (UiDriver.RawWebDriver != null && UiDriver.RawWebDriver.WindowHandles.Count > 0)
                        {
                            CaptureScreen("");
                        }
                    }
                }
                //Make sure 'WriteReport' wasn't already called
                //if (!ProcessTestResultCalled)
                //{
                //}
            }
            catch { }

            try
            {
                ProcessTestResults();
            }
            catch { }
            //Close the browser
            Quit();
            GC.SuppressFinalize(this);
        }
    }

    public class PoolState
    {
        public bool IsPartOfTestExecutionerPool { get; set; }
        public bool IsAvailable { get; set; }

        public bool WasAlreadyInPool { get; set; }

        public string LangingPageURL { get; set; }
    }
}
