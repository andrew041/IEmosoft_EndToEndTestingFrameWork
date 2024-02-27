using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aUI.Automation.Flutter
{
    public class FlutterElementFactory : CachedElementFactory<FlutterElement>
    {
        public FlutterElementFactory(RemoteWebDriver parentDriver) : base(parentDriver)
        {
        }

        protected override FlutterElement CreateCachedElement(RemoteWebDriver parentDriver, string elementId)
        {
            throw new NotImplementedException();
        }
    }
}
