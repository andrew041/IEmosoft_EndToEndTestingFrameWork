using System;
using aUI.Automation.Flutter;
using aUI.Automation.HelperObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Appium.Service;
using OpenQA.Selenium.Appium.Windows;

namespace aUI.Automation.UIDrivers
{
    public class MobileDriver : BrowserDriver
    {
        private readonly AppiumLocalService Local = null;
        private readonly BrowserDriverEnumeration BrowserVendor = BrowserDriverEnumeration.Android;
        public MobileDriver(IAutomationConfiguration configuration, BrowserDriverEnumeration browserVendor = BrowserDriverEnumeration.Android) : base(configuration, browserVendor)
        {
            BrowserVendor = browserVendor;
            var options = ConfigBuilder();

            var appiumServer = Config.GetConfigSetting("RemoteServer", "");
            var local = string.IsNullOrEmpty(appiumServer);
            if (local)
            {
                Local = new AppiumServiceBuilder().UsingAnyFreePort().Build();
                Local.Start();

                switch (browserVendor)
                {
                    case BrowserDriverEnumeration.Windows:
                        RawWebDriver = new WindowsDriver<IWebElement>(Local, options);
                        break;
                    case BrowserDriverEnumeration.Android:
                        RawWebDriver = new AndroidDriver<IWebElement>(Local, options);
                        break;
                    case BrowserDriverEnumeration.IOS:
                        RawWebDriver = new IOSDriver<IWebElement>(Local, options);
                        break;
                }
            }
            else
            {

                var uri = new Uri(appiumServer);
                RawWebDriver = browserVendor switch
                {
                    BrowserDriverEnumeration.Windows => new WindowsDriver<IWebElement>(uri, options),
                    BrowserDriverEnumeration.AndroidRemote => new AndroidDriver<IWebElement>(uri, options),
                    BrowserDriverEnumeration.IOS => new IOSDriver<IWebElement>(uri, options),
                    BrowserDriverEnumeration.Flutter => new FlutterDriver(uri, options),
                    _ => throw new NotSupportedException()
                };
            }
        }



        private AppiumOptions ConfigBuilder()
        {
            var options = new AppiumOptions();

            options.AddAdditionalCapability("appium:appPackage", Config.GetConfigSetting("AppPackage", ""));
            options.AddAdditionalCapability("appium:appActivity", Config.GetConfigSetting("AppActivity", ""));
            options.AddAdditionalCapability("appium:deviceName", Config.GetConfigSetting("DeviceName", ""));
            options.AddAdditionalCapability("appium:automationName", Config.GetConfigSetting("AutomationName", ""));
            options.AddAdditionalCapability("appium:newCommandTimeout", Config.GetConfigSetting("CommandTimeout", "10"));

            return options;
        }

        public override void Dispose()
        {
            RawWebDriver.Quit();
            Local?.Dispose();
            RawWebDriver.Dispose();
        }
    }
}
