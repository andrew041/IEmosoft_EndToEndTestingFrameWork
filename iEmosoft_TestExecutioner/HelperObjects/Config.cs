using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.IO;

namespace aUI.Automation.HelperObjects
{
    public interface IAutomationConfiguration
    {
        //required
        string TestReportFilePath { get; }
        string TestExecutionerUIDriverType { get; }

        //optional
        string TestExecutionerAuthorTypeName { get; }
        string ScreenCaptureLocalPath { get; }
        string FTPUploadURL { get; }
        string FTPUploadUserName { get; }
        string FTPUploadPassword { get; }
        string PostUploadWebAPIServiceURL { get; }
        string ApplicationUnderTest { get; }
        bool FTPFilesInTestProcess { get; }
        bool FTPUpload_DeleteLocalFilesAfterUploadComplete { get; }
        string SauceLabsKey { get; }
        string SauceLabsBrowser { get; }
        string SauceLabsPlatform { get; }
        string RootTestReportPath { get; }
        string SeleniumHubUrl { get; }
        string WebDriverSettings { get; }
        string WindowResolution { get; }
        bool RecordAllSteps { get; }
    }

    public class Config : IAutomationConfiguration
    {
        private static object _locker = new object();
        private static IConfigurationRoot _config;
        public string TestExecutionerAuthorTypeName => GetConfigSetting("TestExecutionerAuthor", "HTML").ToUpper();

        public string ScreenCaptureLocalPath => Path.Combine(TestReportFilePath, "ScreenCapture");

        public string SauceLabsKey => GetConfigSetting("SauceLabsKey");

        public string SauceLabsBrowser => GetConfigSetting("SauceLabsBrowser", "Firefox");
        public string SauceLabsPlatform => GetConfigSetting("SauceLabsPlatform", "Windows 7");

        public bool FTPFilesInTestProcess
        {
            get
            {
                _ = bool.TryParse(GetConfigSetting("FTPFilesInTestProcess", "false"), out bool result);
                return result;
            }
        }
        public string ApplicationUnderTest => GetConfigSetting("ApplicationIdUnderTest");

        public string RootTestReportPath => Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)
            .Parent.Parent.Parent.Parent.Parent.FullName + GetConfigSetting("TestReportFilePath");
        public string FTPUploadURL => GetConfigSetting("FTPUploadURLAndCredentials").Split('|')[0].Trim();

        public string FTPUploadUserName => GetConfigSetting("FTPUploadURLAndCredentials").Split('|')[1].Trim();

        public bool FTPUpload_DeleteLocalFilesAfterUploadComplete
        {
            get
            {
                bool result = false;

                //ftp://www.serverURL.com|userName|password|true
                string[] temp = GetConfigSetting("FTPUploadURLAndCredentials").Split('|');
                if (temp.Length > 3)
                {
                    _ = bool.TryParse(temp[3], out result);
                }

                return result;
            }
        }
        public string FTPUploadPassword => GetConfigSetting("FTPUploadURLAndCredentials").Split('|')[2].Trim();

        public string TestExecutionerUIDriverType => GetConfigSetting("TestExecutionerUIDriver").ToUpper();

        public string PostUploadWebAPIServiceURL => GetConfigSetting("PostUploadWebAPIServiceURL");

        private string RemoveInvalidChars(string str)
        {
            char[] chars = "!@#$%^&*<>:;?.|/\"".ToCharArray();
            foreach (var chr in chars)
            {
                str = str.Replace(chr.ToString(), "-");
            }
            return str.Trim();
        }
        public string TestName
        {
            get
            {
                var name = TestContext.CurrentContext.Test.FullName;
                var a = TestContext.CurrentContext.Test.Name;
                var prams = name.Substring(0, name.Length - TestContext.CurrentContext.Test.Name.Length).Split('(');
                var leadingName = "";
                if (prams.Length > 1)
                {
                    leadingName = prams[1].Split(')')[0].Replace("\"","") + " - ";
                }

                if (TestContext.CurrentContext.Test.FullName.Contains(".API."))
                {
                    leadingName = $"API {leadingName}";
                }
                return RemoveInvalidChars($"{leadingName}{TestContext.CurrentContext.Test.Name}".Replace("_", " "));
            }
        }

        public string TestReportFilePath => RootTestReportPath + TestName + @"/";

        public string SeleniumHubUrl => GetConfigSetting("SeleniumHubUrl").ToUpper();

        public string WebDriverSettings => GetConfigSetting("WebDriverSettings").ToUpper();

        public string WindowResolution => GetConfigSetting("WindowResolution", "1920x1080").ToUpper();

        public bool RecordAllSteps
        {
            get
            {
                var value = GetConfigSetting("RecordAllSteps", "False");
                try
                {
                    return bool.Parse(value);
                }
                catch
                {
                    return false;
                }
            }
        }

        public static string GetEnvironment()
        {
            return Directory.GetParent(Directory.GetCurrentDirectory()).Name;
        }

        public static string GetConfigSetting(string settingName, string resultIfNotFound = null)
        {
            lock (_locker)
            {
                var primary = Environment.GetEnvironmentVariable(settingName);

                if (!string.IsNullOrEmpty(primary))
                {
                    return primary;
                }

                try
                {
                    if (_config == null)
                    {
                        var path = Directory.GetCurrentDirectory();
                        var env = Directory.GetParent(path).Name;

                        path = Directory.GetParent(path).Parent.Parent.FullName;

                        _config = new ConfigurationBuilder().SetBasePath(path)
                            .AddJsonFile("appsettings.json", false, true)
                            .AddJsonFile($"appsettings.{env}.json", true, true)
                            .Build();
                    }
                    if (_config.GetSection($"appSettings:{settingName}").Value == null)
                    {
                        return resultIfNotFound;
                    }

                    return _config.GetSection($"appSettings:{settingName}").Value;

                    //                var result = System.Configuration.ConfigurationManager.AppSettings[settingName];
                    //                return result.ToString();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Get Config Setting Error: " + e.Message);
                    return resultIfNotFound;
                    //throw new Exception(string.Format("Unable to find '{0}' in the config file, this is required", settingName));
                }
            }
        }
    }
}
