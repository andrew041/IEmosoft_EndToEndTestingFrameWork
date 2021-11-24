﻿using aUI.Automation.HelperObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Appium.Service;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Remote;
using System;
using System.Net.Http;

namespace aUI.Automation.UIDrivers
{
    public class MobileDriver : BrowserDriver
    {
        readonly AppiumLocalService Local = null;
        readonly BrowserDriverEnumeration BrowserVendor = BrowserDriverEnumeration.Android;
        public MobileDriver(IAutomationConfiguration configuration, BrowserDriverEnumeration browserVendor = BrowserDriverEnumeration.Android) : base(configuration, browserVendor)
        {
            BrowserVendor = browserVendor;
            var options = ConfigBuilder();

            if (browserVendor.ToString().Contains("REMOTE"))
            {
                var uri = Config.GetConfigSetting("AppiumServerUri");

                HttpClientHandler clientHandler = new()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
                };

                // Pass the handler to httpclient(from you are calling api)
                HttpClient client = new HttpClient(clientHandler);

                switch (browserVendor)
                {
                    case BrowserDriverEnumeration.WindowsRemote:
                        RawWebDriver = new WindowsDriver<IWebElement>(new Uri(uri), options);
                        break;
                    case BrowserDriverEnumeration.AndroidRemote:
                        RawWebDriver = new AndroidDriver<IWebElement>(new Uri(uri), options);
                        break;
                }
            }
            else
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
        }

        private static AppiumOptions ConfigBuilder()
        {
            var ops = new AppiumOptions();
            
            ops.AddAdditionalCapability(MobileCapabilityType.DeviceName, Config.GetConfigSetting("AppiumDeviceName", ""));
            ops.AddAdditionalCapability(MobileCapabilityType.PlatformName, Config.GetConfigSetting("AppiumPlatformName", ""));
            ops.AddAdditionalCapability(MobileCapabilityType.PlatformVersion, Config.GetConfigSetting("AppiumPlatformVersion", ""));
            ops.AddAdditionalCapability(MobileCapabilityType.App, Config.GetConfigSetting("AppiumApp", ""));
            ops.AddAdditionalCapability(MobileCapabilityType.BrowserName, Config.GetConfigSetting("AppiumBrowserName", ""));
            ops.AddAdditionalCapability(MobileCapabilityType.NewCommandTimeout, "120");//unsure if this is enough/too much
            ops.AddAdditionalCapability(MobileCapabilityType.Orientation, Config.GetConfigSetting("AppiumOrientation", "PORTRAIT"));
            //build appium config
            /* 
             * language
             * locale
             * udid
             */
            return ops;
        }

        public override void Dispose()
        {
            try
            {
                ((AndroidDriver<IWebElement>)RawWebDriver).CloseApp();
            }
            catch {
                ((AndroidDriver<IWebElement>)RawWebDriver).Close();
            }
            if(Local != null)
            {
                Local.Dispose();
            }
            RawWebDriver.Dispose();
        }
    }
}
