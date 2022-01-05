using aUI.Automation.HelperObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Appium.Windows;
using System;

namespace aUI.Automation.UIDrivers
{
    public class MobileDriver : BrowserDriver
    {
        readonly BrowserDriverEnumeration BrowserVendor = BrowserDriverEnumeration.Android;
        public MobileDriver(IAutomationConfiguration configuration, BrowserDriverEnumeration browserVendor = BrowserDriverEnumeration.Android) : base(configuration, browserVendor)
        {
            BrowserVendor = browserVendor;

            var ops = new AppiumOptions();
            ops.AddAdditionalCapability(MobileCapabilityType.DeviceName, Config.GetConfigSetting("AppiumDeviceName", ""));
            ops.AddAdditionalCapability(MobileCapabilityType.PlatformName, Config.GetConfigSetting("AppiumPlatformName", ""));
            ops.AddAdditionalCapability(MobileCapabilityType.PlatformVersion, Config.GetConfigSetting("AppiumPlatformVersion", ""));
            ops.AddAdditionalCapability(MobileCapabilityType.App, Config.GetConfigSetting("AppiumApp", ""));
            ops.AddAdditionalCapability(MobileCapabilityType.BrowserName, Config.GetConfigSetting("AppiumBrowserName", ""));
            ops.AddAdditionalCapability(MobileCapabilityType.NewCommandTimeout, "120");//unsure if this is enough/too much
            ops.AddAdditionalCapability(MobileCapabilityType.Orientation, Config.GetConfigSetting("AppiumOrientation", "PORTRAIT"));

            var uri = Config.GetConfigSetting("AppiumServerUri", "http://127.0.01:4723/wd/hub");

            switch (BrowserVendor)
            {
                case BrowserDriverEnumeration.Windows:
                    RawWebDriver = new WindowsDriver<IWebElement>(new Uri(uri), ops);
                    break;
                case BrowserDriverEnumeration.Android:
                    RawWebDriver = new AndroidDriver<IWebElement>(new Uri(uri), ops);
                    break;
                case BrowserDriverEnumeration.IOS:
                    RawWebDriver = new IOSDriver<IWebElement>(new Uri(uri), ops);
                    break;
            }
        }

        public override void Dispose()
        {
            switch (BrowserVendor)
            {
                case BrowserDriverEnumeration.Windows:
                    ((WindowsDriver<IWebElement>)RawWebDriver).CloseApp();
                    break;
                case BrowserDriverEnumeration.Android:
                    ((AndroidDriver<IWebElement>)RawWebDriver).CloseApp();
                    break;
                case BrowserDriverEnumeration.IOS:
                    ((IOSDriver<IWebElement>)RawWebDriver).CloseApp();
                    break;
            }

            RawWebDriver.Dispose();
        }
    }
}
