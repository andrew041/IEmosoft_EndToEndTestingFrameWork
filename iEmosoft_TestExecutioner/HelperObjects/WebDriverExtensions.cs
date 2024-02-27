using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace aUI.Automation.HelperObjects
{
    public static class WebDriverExtensions
    {
        public static string ToDeepMessage(this Exception exp)
        {
            string result = exp.Message;

            var innerExp = exp.InnerException;
            while (innerExp != null)
            {
                result += " -> " + innerExp.Message;
                innerExp = innerExp.InnerException;
            }

            return result;
        }
        public static void ClickElement(this IWebDriver driver, By by)
        {
            driver.FindElement(by).Click();
        }

        public static void SetTextOnElement(this IWebDriver driver, By by, string text)
        {
            driver.FindElement(by).SendKeys(text);
        }

        public static IntPtr GetFirefoxWindowsHandle(this IWebDriver driver)
        {
            IntPtr result = IntPtr.Zero;

            var title = driver.Title;
            title = title.IsNull() ? "Something we don't want to seach for ASFSFSA!!" : title;

            var firefoxBrowsers = Process.GetProcessesByName("firefox");
            if (firefoxBrowsers.Length > 1)
            {
                IntPtr activeWindowHandle = GetActiveWindow();

                foreach (var browser in firefoxBrowsers)
                {
                    if (browser.MainWindowTitle == title || browser.MainWindowHandle == activeWindowHandle)
                    {
                        result = browser.MainWindowHandle;
                    }
                }

                return result;
            }

            return firefoxBrowsers.ElementAt(0).MainWindowHandle;
        }

        public static bool DoesElementExist(this IWebDriver driver, By by)
        {
            try
            {
                return driver.FindElement(by) != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool DoesElementExist(this IWebDriver driver, string elementID)
        {
            return DoesElementExist(driver, By.Id(elementID));
        }

        public static void SetTextOnElement(this IWebDriver driver, string elementToFind, string text)
        {
            IWebElement element = null;

            try
            {
                element = driver.FindElement(By.Id(elementToFind));
            }
            catch { }

            if (element == null)
            {
                element = driver.FindElement(By.XPath(elementToFind));
            }

            if (elementToFind != null)
                element.SendKeys(text);

        }

        public static string GetTextOnElement(this IWebDriver driver, By by)
        {
            var element = driver.FindElement(by);
            string rtnVal = element.Text;

            if (string.IsNullOrEmpty(rtnVal))
                rtnVal = element.GetAttribute("value");

            return rtnVal;
        }


        public static string GetSelectedTextOnDropdown(this IWebDriver driver, By by)
        {
            var dropdown = (SelectElement)driver.FindElement(by);

            return dropdown.SelectedOption.Text;
        }

        public static bool PageContains(this IWebDriver driver, string lookFor)
        {
            return driver.PageSource.ToLower().Contains(lookFor.ToLower());
        }

        public static string GetSelectedValueOnDropdown(this IWebDriver driver, By by)
        {
            var dropdown = (SelectElement)driver.FindElement(by);

            return dropdown.SelectedOption.GetAttribute("value");
        }

        public static void NavigateTo(this IWebDriver driver, string url)
        {
            driver.Navigate().GoToUrl(url);
        }

        public static object ExecuteScript(this IWebDriver driver, string rawJavaScript)
        {
            IJavaScriptExecutor jsExecutor = driver as IJavaScriptExecutor;
            return jsExecutor.ExecuteScript(rawJavaScript);
        }

        public static string MineForValue(this IWebElement element)
        {
            string result = "";

            try
            {
                result = element.GetAttribute("value");
            }
            catch
            {
            }

            if (result.isNull())
            {
                try
                {
                    result = element.GetAttribute("Value");
                }
                catch
                {
                }
            }

            if (result.isNull())
            {
                try
                {
                    result = element.Text;
                }
                catch { }
            }

            return result;

        }
        public static string MineForTextValue(this IWebElement element, IJavaScriptExecutor driver)
        {
            string result = element.Text;
            if (result.isNull())
            {
                string id = element.GetAttribute("id");
                if (!id.isNull())
                {
                    string script = string.Format("return $('#{0}').val()", id);
                    result = driver.ExecuteScript(script).ToString();
                }
            }

            return result;
        }

        public static IWebElement MineForElement(this IWebElement element, string attributeName, string attributeValue, string elementName = "")
        {
            try
            {
                return element.MineForElements(attributeName, attributeValue, elementName)[0];
            }
            catch { }

            return null;
        }

        public static List<IWebElement> MineForElements(this IWebElement element, string attributeName, string attributeValue, string elementName = "")
        {
            string query = string.Format("{0}[{1}*='{2}']", elementName, attributeName, attributeValue);

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var results = element.FindElements(By.CssSelector(query));
                    if (results != null && results.Count > 0)
                    {
                        return results.ToList();
                    }
                }
                catch { }

                Thread.Sleep(200);
            }

            return new List<IWebElement>();
        }

        public static IWebElement MineForElement(this IWebDriver driver, string idOrCssSelector,
            int retryForSeconds = 10)
        {
            IWebElement rtnVal = null;

            int seconds = retryForSeconds == 0 ? 1 : retryForSeconds > 60 ? 60 : retryForSeconds;
            int retryAttemps = seconds < 0 ? 1 : (seconds * 1000) / 200;

            for (var i = 0; i < retryAttemps; i++)
            {
                rtnVal = QueryForElement(driver, By.Id(idOrCssSelector));
                if (rtnVal != null)
                    break;

                rtnVal = QueryForElement(driver, By.CssSelector(idOrCssSelector));
                if (rtnVal != null)
                    break;

                Thread.Sleep(200);
            }

            if (rtnVal == null)
            {
                throw new Exception(string.Format("Unable to find '{0}' in DOM", idOrCssSelector));
            }

            return rtnVal;
        }

        public static string InnerHTML(this IWebDriver driver, IWebElement element)
        {
            if (element.GetAttribute("id").isNull())
                return null;

            string script = string.Format("return $('#{0}').innerHTML()'", element.GetAttribute("id"));
            return driver.ExecuteScript(script).ToString();
        }

        public static IWebElement MineForElement(this IWebDriver driver, string attributeName, string attributeValue,
            string controlType = "",
            bool useWildCardSearch = true, int retryForSeconds = 10)
        {
            IWebElement result = null;
            string query = "";

            int seconds = retryForSeconds == 0 ? 1 : retryForSeconds > 60 ? 60 : retryForSeconds;
            int retryAttemps = retryForSeconds < 0 ? 1 : (seconds * 1000) / 200;

            for (var i = 0; i < retryAttemps; i++)
            {
                query = string.Format("{0}[{1}='{2}']", controlType, attributeName, attributeValue);
                result = QueryForElement(driver, By.CssSelector(query));

                if (result == null && useWildCardSearch)
                {
                    query = string.Format("{0}[{1}*='{2}']", controlType, attributeName, attributeValue);
                    result = QueryForElement(driver, By.CssSelector(query));
                }

                if (result != null)
                    break;

                Thread.Sleep(200);
            }

            if (result == null)
            {
                throw new Exception(string.Format("Unable to find '{0}' in DOM", query));
            }

            return result;
        }


        public static List<IWebElement> MineForElements(this IWebDriver driver, string attributeName, string attributeValue, string controlType = "",
            bool useWildCardSearch = true, int retryIfCountIsZeroHowManyTimes = 10)
        {
            List<IWebElement> rtnVal = null;

            for (var i = 0; i <= retryIfCountIsZeroHowManyTimes; i++)
            {
                var query = string.Format("{0}[{1}='{2}']", controlType, attributeName, attributeValue);
                rtnVal = QueryForElements(driver, By.CssSelector(query));

                if ((rtnVal == null || rtnVal.Count == 0) && useWildCardSearch)
                {
                    query = string.Format("{0}[{1}*='{2}']", controlType, attributeName, attributeValue);
                    rtnVal = QueryForElements(driver, By.CssSelector(query));
                }

                if ((rtnVal == null || rtnVal.Count == 0) && controlType.isNull() == false && i > 6 && useWildCardSearch)
                {
                    //Sometimes the *= doesn't really work
                    var tempList = QueryForElements(driver, By.TagName(controlType));
                    if (tempList != null)
                    {
                        var attLower = attributeValue.ToLower();

                        for (var j = tempList.Count - 1; j >= tempList.Count; j--)
                        {
                            string checkValue = tempList[j].GetAttribute(attributeName);
                            if (checkValue.isNull() || checkValue.ToLower().Contains(attLower) == false)
                            {
                                tempList.RemoveAt(j);
                            }
                        }

                        if (tempList.Count > 0)
                        {
                            return tempList;
                        }
                    }
                }

                if (rtnVal != null && rtnVal.Count > 0)
                {
                    break;
                }

                Thread.Sleep(200);
            }

            return rtnVal;
        }

        public static List<IWebElement> MineForElements(this IWebDriver driver, string idOrCssSelector,
            int retryIfCountIsZeroHowManyTimes = 10)
        {
            List<IWebElement> rtnVal = null;

            for (var i = 0; i <= retryIfCountIsZeroHowManyTimes; i++)
            {
                rtnVal = QueryForElements(driver, By.CssSelector(idOrCssSelector));
                if (rtnVal != null && rtnVal.Count > 0)
                    break;

                rtnVal = QueryForElements(driver, By.Id(idOrCssSelector));
                if (rtnVal != null && rtnVal.Count > 0)
                    break;

                Thread.Sleep(200);
            }

            return rtnVal;
        }

        private static IWebElement QueryForElement(IWebDriver driver, By by)
        {
            try
            {
                return driver.FindElement(by);
            }
            catch
            {
                return null;
            }
        }

        private static List<IWebElement> QueryForElements(IWebDriver driver, By by)
        {
            try
            {
                return driver.FindElements(by).ToList();
            }
            catch
            {
                return null;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetActiveWindow();

        public static bool IsNull(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static void ScrollToElement(this IJavaScriptExecutor js, IWebElement element)
        {
            js.ExecuteScript("arguments[0].scrollIntoView({block: \"center\"});", element);
        }
    }
}
