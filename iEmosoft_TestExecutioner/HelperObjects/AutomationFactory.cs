using aUI.Automation.Authors;
using aUI.Automation.BaseClasses;
using aUI.Automation.Flutter;
using aUI.Automation.Interfaces;
using aUI.Automation.ScreenCaptures;
using aUI.Automation.Test.IEmosoft.com;
using aUI.Automation.UIDrivers;
using System;

namespace aUI.Automation.HelperObjects
{
    public class AutomationFactory
    {
        public IAutomationConfiguration Configuration;

        public AutomationFactory(IAutomationConfiguration config = null)
        {
            if (config == null)
            {
                Configuration = new Config();
            }
            else
            {
                Configuration = config;
            }
        }

        public BaseAuthor CreateAuthor(string rootPath = null)
        {
            string authorTypeName = Configuration.TestExecutionerAuthorTypeName;
            return NewUpAuthor(authorTypeName, rootPath);
        }

        public IUIDriver CreateUIDriver()
        {
            string browserName = Configuration.TestExecutionerUIDriverType;
            IUIDriver driver = browserName switch
            {
                "FIREFOX" => new BrowserDriver(Configuration),
                "IE" => new BrowserDriver(Configuration, BrowserDriver.BrowserDriverEnumeration.IE),
                "CHROME" => new BrowserDriver(Configuration, BrowserDriver.BrowserDriverEnumeration.Chrome),
                "CHROMEHEADLESS" => new BrowserDriver(Configuration, BrowserDriver.BrowserDriverEnumeration.ChromeHeadless),
                "SAUCELABS" => new BrowserDriver(Configuration, BrowserDriver.BrowserDriverEnumeration.SauceLabs),
                "CHROMEREMOTE" => new BrowserDriver(Configuration, BrowserDriver.BrowserDriverEnumeration.ChromeRemote),
                "FIREFOXREMOTE" => new BrowserDriver(Configuration, BrowserDriver.BrowserDriverEnumeration.FirefoxRemote),
                "WINDOWS" => new MobileDriver(Configuration, BrowserDriver.BrowserDriverEnumeration.Windows),
                "ANDROID" => new MobileDriver(Configuration, BrowserDriver.BrowserDriverEnumeration.Android),
                "ANDROIDREMOTE" => new MobileDriver(Configuration, BrowserDriver.BrowserDriverEnumeration.AndroidRemote),
                "IOS" => new MobileDriver(Configuration, BrowserDriver.BrowserDriverEnumeration.IOS),
                "WPF" => new WindowsWhite(),
                "FLUTTER" => new MobileDriver(Configuration, BrowserDriver.BrowserDriverEnumeration.Flutter),
                _ => throw new Exception($"{browserName} is an Unknown UI driver type in config, expected 'WPF', 'CHOME', 'IE','FIREFOX'"),
            };
            if (driver == null)
            {
                //  throw new Exception("Unable to create UI driver, configuration setting should be WPF or WEB, actual value: " + uiDriverType);
            }

            return driver;
        }

        public IScreenCapture CreateScreenCapturer(IUIDriver driver, string rootPathOrURL = null)
        {
            return new HeadlessScreenCapture(rootPathOrURL.isNull() ? Configuration.ScreenCaptureLocalPath : rootPathOrURL, driver);
            //return new LocalScreenCapture(rootPathOrURL.isNull() ? configuration.ScreenCaptureLocalPath : rootPathOrURL);
        }

        public IReportUploader CreateReportFTPUploader()
        {
            return new ReportUploader(Configuration);
        }


        private BaseAuthor NewUpAuthor(string authorTypeName, string rootPath = null)
        {
            BaseAuthor result = null;
            var reportPath = rootPath.isNull() ? Configuration.TestReportFilePath : rootPath;

            switch (authorTypeName)
            {
                case "EXCEL":
                    //result = new ExcelAuthor(reportPath);
                    break;

                case "HTML":
                    result = new HTMLAuthor(reportPath);
                    break;
            }

            if (result == null)
            {
                throw new Exception(string.Format("Config setting (for 'TestExecutionerAuthor / MultiAuthor_Authors' should be / contain 'MULTIPLE', 'EXCEL', 'REMOTE', or 'HTML', actual value '{0}'. Unable to create author object", authorTypeName));
            }

            return result;
        }

    }
}
