using aUI.Automation.Flutter.Interfaces;
using OpenQA.Selenium.Appium.Interfaces;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;

namespace aUI.Automation.Flutter
{
    public class FlutterElement : RemoteWebElement, IWebElementCached, IFindByAncestor, IFindByText, IFindByValueKey, IFindByDescendant, IFindBySemanticsLabel, IFindByTooltipMessage
    {
        private readonly FlutterDriver Driver;
        public readonly FlutterBy Finder;
        public FlutterElement(FlutterDriver driver, string id) : base(driver, id)
        {
            Driver = driver;
        }
        public FlutterElement(FlutterDriver driver, FlutterBy finder) : base(driver, finder.Encoded())
        {
            Driver = driver;
            Finder = finder;
        }

        #region Finders
        /// <summary>
        /// Looks for matching element as a descendant of current element
        /// </summary>
        /// <param name="by">element to look for</param>
        /// <returns>matching FlutterElement</returns>
        public FlutterElement FindElement(FlutterBy by)
        {
            return FindElementByDescendant(by);
        }
        
        /// <summary>
        /// Find an element that is an ancestor of the current element
        /// </summary>
        /// <param name="matching"></param>
        /// <param name="matchRoot"></param>
        /// <param name="firstMatchOnly"></param>
        /// <returns></returns>
        public FlutterElement FindElementByAncestor(FlutterBy matching, bool matchRoot = true, bool firstMatchOnly = false)
        {
            return Driver.FindElement(FlutterBy.Ancestor(Finder, matching, matchRoot, firstMatchOnly));
        }

        /// <summary>
        /// Find an element that is a descendant of the current element
        /// </summary>
        /// <param name="matching">element we are looking for</param>
        /// <param name="matchRoot">can it match this, usually should be true</param>
        /// <param name="firstMatchOnly">this option practically does nothing, appium-flutter-driver does not support multiple elements returned</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public FlutterElement FindElementByDescendant(FlutterBy matching, bool matchRoot = true, bool firstMatchOnly = false)
        {
            return Driver.FindElement(FlutterBy.Descendant(Finder, matching, matchRoot, firstMatchOnly));
        }

        /// <summary>
        /// Find an element with text `text` which is a child of this.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public FlutterElement FindElementByText(string text)
        {
            return FindElement(FlutterBy.Text(text));
        }

        /// <summary>
        /// Find an element that is a descendent of this with ValueKey valueKey
        /// </summary>
        /// <param name="valueKey">matching ValueKey</param>
        /// <returns>matching FlutterElement</returns>
        public FlutterElement FindElementByValueKey(string valueKey)
        {
            return FindElement(FlutterBy.ValueKey(valueKey));
        }
        
        /// <summary>
        /// Find a element with semantic label `label` which is a child of this.
        /// </summary>
        /// <param name="label">matching label</param>
        /// <returns>matching FlutterElement</returns>
        public FlutterElement FindElementBySemanticsLabel(string label)
        {
            return FindElement(FlutterBy.Descendant(Finder, FlutterBy.SemanticsLabel(label)));
        }

        /// <summary>
        /// Find a element with a tooltip message which is a child of this.
        /// </summary>
        /// <param name="tooltipMessage">matching tooltip message</param>
        /// <returns>matching FlutterElement</returns>
        public FlutterElement FindElementByTooltipMessage(string tooltipMessage)
        {
            return FindElement(FlutterBy.Descendant(Finder, FlutterBy.TooltipMessage(tooltipMessage)));
        }

        #endregion

        public void SetCacheValues(Dictionary<string, object> cacheValues)
        {
            throw new NotImplementedException();
        }

        public void DisableCache()
        {
            throw new NotImplementedException();
        }

        public void ClearCache()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Assumes current element is a scrollable type like ListView or GridView, and tries to scroll this until by is visible
        /// </summary>
        /// <param name="by"></param>
        public void ScrollTo(FlutterBy by)
        {
            Driver.ScrollTo(Finder, by);
        }
    }
}
