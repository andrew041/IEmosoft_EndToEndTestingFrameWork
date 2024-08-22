using aUI.Automation.HelperObjects;
using aUI.Automation.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace aUI.Automation.UIDrivers
{
    public class BrowserDriver : IUIDriver
    {
        public enum BrowserDriverEnumeration
        {
            Chrome,
            ChromeHeadless,
            ChromeRemote,
            Firefox,
            FirefoxRemote,
            IE,
            SauceLabs,
            Windows,
            Android,
            AndroidRemote,
            IOS,
            Flutter
        }

        private IWebDriver Browser = null;

        public string DriverType { get; private set; }


        public List<string> FailedBrowsers { get { return new List<string> { DriverType }; } }

        public BrowserDriver(IAutomationConfiguration configuration, BrowserDriverEnumeration browserVendor = BrowserDriverEnumeration.Firefox)
        {
            //add default screen size
            //add ability for custom settings in config
            //Add 'uri' for remote execution
            var uri = Config.GetConfigSetting("SeleniumHubUrl");

            switch (browserVendor)
            {
                case BrowserDriverEnumeration.Firefox:
                    var ffService = FirefoxDriverService.CreateDefaultService();
                    Browser = new FirefoxDriver(ffService);
                    DriverType = "FireFox";
                    break;

                case BrowserDriverEnumeration.FirefoxRemote:
                    var ffRemoteService = new FirefoxOptions();
                    Browser = new RemoteWebDriver(new Uri(uri), ffRemoteService);
                    DriverType = "FireFox";
                    CodePagesEncodingProvider.Instance.GetEncoding(437);
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    ((RemoteWebDriver)Browser).FileDetector = new LocalFileDetector();
                    break;
                case BrowserDriverEnumeration.Chrome:
                    var chromeOps = new ChromeOptions();
                    chromeOps.AddArgument("force-device-scale-factor=0.75");
                    chromeOps.AddArgument("high-dpi-support=0.75");
                    Browser = new ChromeDriver("./", chromeOps);
                    DriverType = "Chrome";
                    break;
                case BrowserDriverEnumeration.ChromeHeadless:
                    var chromeHeadlessOps = new ChromeOptions();
                    chromeHeadlessOps.AddArgument("headless");
                    chromeHeadlessOps.AddArgument("--no-sandbox");
                    var windowResolution = Config.GetConfigSetting("WindowResolution");
                    chromeHeadlessOps.AddArgument($"window-size={windowResolution.Replace("x",",")}");
                    Browser = new ChromeDriver("./", chromeHeadlessOps);
                    DriverType = "Chrome";
                    break;
                case BrowserDriverEnumeration.ChromeRemote:
                    var chromeROps = new ChromeOptions();
                    chromeROps.AddArgument("--disable-dev-shm-usage");
                    chromeROps.AddArgument("--no-sandbox");
                    Browser = new RemoteWebDriver(new Uri(uri), chromeROps);
                    DriverType = "Chrome";
                    CodePagesEncodingProvider.Instance.GetEncoding(437);
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    ((RemoteWebDriver)Browser).FileDetector = new LocalFileDetector();
                    break;
                case BrowserDriverEnumeration.IE:
                    var ieOptions = new InternetExplorerOptions();
                    Browser = new InternetExplorerDriver(ieOptions);
                    DriverType = "IE";
                    break;
                case BrowserDriverEnumeration.SauceLabs:
                    var capabilities = GetDesiredCapabilities(configuration);
                    var url = new Uri("http://" + configuration.SauceLabsKey + "@ondemand.saucelabs.com:80/wd/hub");
                    Browser = new RemoteWebDriver(url, capabilities);
                    CodePagesEncodingProvider.Instance.GetEncoding(437);
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    ((RemoteWebDriver)Browser).FileDetector = new LocalFileDetector();
                    break;
            }
        }

        private DriverOptions GetDesiredCapabilities(IAutomationConfiguration config)
        {
            DriverOptions result = config.SauceLabsBrowser switch
            {
                "IE" => new InternetExplorerOptions(),
                "Chrome" => new ChromeOptions(),
                _ => new FirefoxOptions(),
            };

            string[] usernameKey = config.SauceLabsKey.Split(':');

            if (usernameKey.Length != 2)
            {
                throw new Exception(string.Format("SauceLabsKey found in config file is not as expected.  Expected username:key in the value attribute"));
            }

            result.AddAdditionalCapability("username", usernameKey[0]);
            result.AddAdditionalCapability("accessKey", usernameKey[1]);
            result.AddAdditionalCapability("platform", config.SauceLabsPlatform);
            return result;
        }

        public bool ScreenContains(string lookFor)
        {
            return Browser.PageSource.Contains(lookFor);
        }

        public void SetTextOnControl(string controlIdOrCssSelector, string textToSet)
        {
            IWebElement element = Browser.MineForElement(controlIdOrCssSelector);
            SetTextOnControl(element, textToSet);
        }

        public void SetTextOnControl(string attributeName, string attributeValue, string textToSet,
            string controlType = "", bool useWildCardSearch = true, int retryForSeconds = 10)
        {
            IWebElement element = Browser.MineForElement(attributeName, attributeValue, controlType,
                useWildCardSearch, retryForSeconds);

            if (element == null)
            {
                throw new Exception(string.Format("Unable to find {0}[{1}='{2}']", element, attributeName, attributeValue));
            }

            SetTextOnControl(element, textToSet);
        }

        private void SetTextOnControl(IWebElement element, string textToSet)
        {
            if (textToSet.isNull() || textToSet.StartsWith("+=") == false)
            {
                try
                {
                    element.Clear();
                }
                catch { }
            }

            if (!textToSet.isNull())
            {
                if (element.Displayed)
                {
                    element.SendKeys(textToSet);
                }
            }
        }

        public void ClickControl(string controlIdOrCssSelector)
        {
            IWebElement element = Browser.MineForElement(controlIdOrCssSelector);
            ClickElement(element);
        }

        public void ClickControl(string attributeName, string attributeValue, string controlType = "",
            bool useWildCardSearch = true, int retryForSeconds = 10)
        {
            IWebElement element = Browser.MineForElement(attributeName, attributeValue, controlType,
                useWildCardSearch, retryForSeconds);
            ClickElement(element);
        }

        private void ClickElement(IWebElement element)
        {
            try
            {
                element.Click();
            }
            catch (UnhandledAlertException alertExp)
            {
                HandleUnexpectedAlertModal(alertExp);
            }
        }

        private void HandleUnexpectedAlertModal(UnhandledAlertException exp)
        {
            string alertText = "";
            try
            {
                var alert = Browser.SwitchTo().Alert();
                alertText = alert.Text;
                alert.Accept();
            }
            catch (Exception e)
            {
                throw new Exception(
                    string.Format("Unable to accept alert from selenium driver.  Alert Text: {0}", alertText), e);
            }
        }
        public string GetTextOnControl(string controlIdOrCssSelector)
        {
            IWebElement element = Browser.MineForElement(controlIdOrCssSelector);
            return element.MineForTextValue(Browser as IJavaScriptExecutor);
        }

        public string GetTextOnControl(string attributeName, string attributeValue, string controlType = "",
            bool useWildCardSearch = true, int retryForSeconds = 10)
        {
            IWebElement element = Browser.MineForElement(attributeName, attributeValue, controlType,
                useWildCardSearch, retryForSeconds);
            return element.MineForTextValue(Browser as IJavaScriptExecutor);
        }

        public bool AmOnSceen(string snippetToLookFor)
        {
            return Browser.Url.Contains(snippetToLookFor);
        }

        public void SetValueOnDropDown(string controlIdOrCssSelector, string valueToSet)
        {
            var dropdown = (SelectElement)Browser.MineForElement(controlIdOrCssSelector);
            var originalValue = dropdown.SelectedOption.Text;

            dropdown.SelectByText(valueToSet);

            if (originalValue == dropdown.SelectedOption.Text)
            {
                dropdown.SelectByValue(valueToSet);
            }
        }

        public void SetValueOnDropDown(string attributeName, string attributeValue, string valueToSet,
            bool useWildCardSearch = true, int retryForSeconds = 10)
        {

            var selectElement = Browser.MineForElement(attributeName, attributeValue, "select", true);

            try
            {
                var dropdown = (SelectElement)selectElement;
                var originalValue = dropdown.SelectedOption.Text;

                dropdown.SelectByText(valueToSet);

                if (originalValue == dropdown.SelectedOption.Text)
                {
                    dropdown.SelectByValue(valueToSet);
                }
            }
            catch
            {
                selectElement.SendKeys(valueToSet);
            }
        }

        public bool IsCheckBoxChecked(string controlIdOrCssSelector)
        {
            IWebElement element = Browser.MineForElement(controlIdOrCssSelector);
            bool isCurrentlyChecked = element.Selected;

            return isCurrentlyChecked;

        }

        public bool IsCheckBoxChecked(string attributeName, string attributeValue, string controlType = "",
            bool useWildCardSearch = true, int retryForSeconds = 10)
        {
            IWebElement element = Browser.MineForElement(attributeName, attributeValue, controlType,
                useWildCardSearch, retryForSeconds);
            bool isCurrentlyChecked = element.Selected;

            return isCurrentlyChecked;

        }

        public void SetCheckBoxValueTo(string controlIdOrCssSelector, bool valueItShouldBeSetTo)
        {
            IWebElement element = Browser.MineForElement(controlIdOrCssSelector);

            bool isCurrentlyChecked = element.Selected;

            if (isCurrentlyChecked != valueItShouldBeSetTo)
            {
                element.Click();
            }
        }

        public void SetCheckBoxValueTo(string attributeName, string attributeValue, bool valueItShouldBeSetTo,
            string controlType = "", bool useWildCardSearch = true, int retryForSeconds = 10)
        {
            IWebElement element = Browser.MineForElement(attributeName, attributeValue, controlType,
                useWildCardSearch, retryForSeconds);
            bool isCurrentlyChecked = element.Selected;

            if (isCurrentlyChecked != valueItShouldBeSetTo)
            {
                element.Click();
            }
        }

        public string GetTextOnDropDown(string controlIdOrCssSelector)
        {
            var element = (SelectElement)Browser.MineForElement(controlIdOrCssSelector);
            try
            {
                return element.SelectedOption.Text;
            }
            catch
            {
            }

            return "";
        }

        public string GetTextOnDropDown(string attributeName, string attributeValue, string controlType = "",
            bool useWildCardSearch = true, int retryForSeconds = 10)
        {
            try
            {
                var element =
                    (SelectElement)
                        Browser.MineForElement(attributeName, attributeValue, controlType, useWildCardSearch,
                            retryForSeconds);
                return element.SelectedOption.Text;
            }
            catch
            {
            }

            return "";
        }

        public string GetValueOnDropDown(string controlIdOrCssSelector)
        {
            try
            {
                var element = (SelectElement)Browser.MineForElement(controlIdOrCssSelector);
                return element.SelectedOption.GetAttribute("value");
            }
            catch
            {
            }

            return "";

        }

        public string GetValueOnDropDown(string attributeName, string attributeValue, string controlType = "",
            bool useWildCardSearch = true, int retryForSeconds = 10)
        {
            try
            {
                var element =
                    (SelectElement)
                        Browser.MineForElement(attributeName, attributeValue, controlType, useWildCardSearch,
                            retryForSeconds);
                return element.SelectedOption.GetAttribute("value");
            }
            catch
            {
            }

            return "";
        }

        public void NavigateTo(string windowNameOrUri)
        {
            Browser.NavigateTo(windowNameOrUri);
        }

        public void Launch(string appNameOrUri)
        {
            NavigateTo(appNameOrUri);
        }

        public void Pause(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }


        public string CurrentFormName_OrPageURL
        {
            get
            {
                try
                {
                    return Browser.Url;
                }
                catch (UnhandledAlertException alertExp)
                {
                    HandleUnexpectedAlertModal(alertExp);
                    return Browser.Url;
                }
            }
        }

        public void ShowWindow()
        {
            Browser.Manage().Window.Maximize();
        }

        public void MaximizeWindow()
        {
            Browser.Manage().Window.Maximize();
        }

        public virtual void Dispose()
        {
            Browser.Quit();
            GC.SuppressFinalize(this);
        }

        public IWebDriver RawWebDriver { get { return Browser; } protected set { Browser = value; } }
    }
}
