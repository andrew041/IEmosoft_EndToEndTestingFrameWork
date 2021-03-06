﻿using aUI.Automation.ModelObjects;
using System;
using System.Collections.Generic;
using System.IO;

namespace aUI.Automation.BaseClasses
{
    public abstract class BaseAuthor : IDisposable
    {
        private int NextStepNumber = 0;

        public List<TestCaseStep> RecordedSteps { get; private set; } = new List<TestCaseStep>();
        public TestCaseHeaderData TestCaseHeader { get; private set; }

        protected string TestCaseTemplatePath;
        protected string NewTestCasePath = "";
        protected string NewTestCaseName = "";
        protected string RootTestCasesFolder = "";

        protected TestCaseStep CurrentTestCaseStep = null;

        protected bool FileIsDirty = false;
        protected bool TemplateWasFound = true;

        public bool TestCaseFailed
        {
            get
            {
                bool result = false;

                for (var i = 0; i < RecordedSteps.Count; i++)
                {
                    if (RecordedSteps[i].StepPassed == false)
                    {
                        result = true;
                        break;
                    }
                }

                return result;
            }
        }

        //** ABSTRACT METHODS **
        public abstract string SaveReport();
        public abstract bool StartNewTestCase(TestCaseHeaderData headerData);

        public bool AddTestStep(string stepDescription, string expectedResult = "", string suppliedData = "", bool wasSuccessful = true, string actualResult = "", string imageFile = "")
        {
            return false;
        }

        public void BeginTestCaseStep(string stepDescription, string expectedResult = "", string suppliedData = "")
        {
            FileIsDirty = true;

            CurrentTestCaseStep = new TestCaseStep()
            {
                StepDescription = stepDescription,
                ExpectedResult = expectedResult,
                SuppliedData = suppliedData,
                StepPassed = true
            };

            RecordedSteps.Add(CurrentTestCaseStep);
        }

        public TestCaseStep CurrentStep
        {
            get { return CurrentTestCaseStep; }
        }

        protected bool InitialzieNewTestCase(TestCaseHeaderData testCaseHeader)
        {
            NextStepNumber = 0;
            TestCaseHeader = testCaseHeader;
            NewTestCaseName = testCaseHeader.TestName;

            string subFolder = string.IsNullOrEmpty(testCaseHeader.SubFolder) ? "" : "\\" + testCaseHeader.SubFolder;
            NewTestCasePath = string.Format("{0}{1}", RootTestCasesFolder, subFolder);

            //            templateWasFound = File.Exists(testCaseTemplatePath);
            //            if (!templateWasFound)
            //            {
            //                return false;
            //            }

            if (!Directory.Exists(NewTestCasePath) && !string.IsNullOrEmpty(NewTestCasePath))
            {
                Directory.CreateDirectory(NewTestCasePath);
            }

            TestCaseHeader = testCaseHeader;
            RecordedSteps = new List<TestCaseStep>();

            FileIsDirty = true;
            return true;
        }

        protected string GetNextFileName()
        {
            string result = NewTestCasePath;
            int ctr = 0;

            while (File.Exists(result))
            {
                ctr += 1;
                result = NewTestCasePath.Replace(".", ctr.ToString() + ".");
            }

            if (TestCaseFailed)
            {
                string fileName = Path.GetFileName(result);

                result = result.Replace(fileName, "Failed - " + fileName);
            }

            result = result.Replace("\"", "").Replace("*", "").Replace("|", "-");

            return result;
        }

        protected string GetNextStepSequenceNumberString()
        {
            NextStepNumber += 1;
            return (NextStepNumber * 10).ToString();
        }

        public void Dispose()
        {
            //Subclasses can hide this and implement their own dispose if they wish
            GC.SuppressFinalize(this);
        }
    }
}
