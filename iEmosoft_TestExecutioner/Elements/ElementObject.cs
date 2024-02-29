using aUI.Automation.Enums;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;

namespace aUI.Automation.Elements
{
    public class ElementObject
    {
        public ElementType EleType = ElementType.Id;
        public string EleRef = "";
        public int MaxWait = 10;
        //build a few helper methods to create 'CustomCondition'
        public Func<IWebDriver, By, (IWebElement, bool)> CustomCondition = null;
        public Func<IWebDriver, By, (List<IWebElement>, bool)> CustomConditionMulti = null;

        public Wait WaitType = Wait.Visible;
        public string Text = "";
        public ElementAction Action = ElementAction.GetText;

        public bool Scroll = true;
        public string ScrollLoc = "center";
        public bool Random = false;
        public bool Clear = true;
        public bool ProtectedValue = false;
        public int RandomLength = 25;

        public string ElementName = "";

        public bool UseChild;
        public ElementObject FindFrom;  // Represents a parent element if using descendant, or child element if using ancestor
        public ElementObject ScrollableParent;

        /// <summary>
        /// Option to replace parts of a preset enum with runtime values.
        /// Key - what to replace
        /// Value - what to replace it with
        /// </summary>
        public Dictionary<string, string> RuntimeRefUpdate = new();
        public string ExpectedValue = "";
        public bool ReportStep = true;

        public ElementObject()
        {
        }

        public ElementObject(ElementType type, string eleRef, string desiredText = "")
        {
            EleType = type;
            EleRef = eleRef;
            ElementName = $"{type} Element Specified";
            Text = desiredText;
            ExpectedValue = desiredText;
        }

        public ElementObject(Enum eleRef, string desiredText = "")
        {
            //TODO Expand constructors
            EleType = eleRef.Type();
            EleRef = eleRef.Ref();
            ElementName = eleRef.ToString();
            Text = desiredText;
            ExpectedValue = desiredText;
            ScrollableParent = eleRef.ScrollOn();
            var child = eleRef.Child();
            UseChild = child is not null;
            FindFrom = child;    // Will be set properly if descendant, otherwise it will be null
            // TODO: add ancestor
        }

        public ElementObject(Enum eleRef, int randLength) : this(eleRef)
        {
            //TODO Expand constructors
            Random = true;
            RandomLength = randLength;
        }

        public ElementObject(Enum scrollParent, ElementType type, string eleRef) : this(type, eleRef)
        {
            ScrollableParent = new(scrollParent);
        }

        public ElementObject(Enum eleRef, Dictionary<string, string> runtimeUpdate) : this(eleRef)
        {
            RuntimeRefUpdate = runtimeUpdate;
            RuntimeUpdate();
        }

        public void SetChild(ElementObject child)
        {
            UseChild = child is not null;
            FindFrom = child;    // Will be set properly if descendant, otherwise it will be null
        }

        public void RuntimeUpdate()
        {
            foreach (var itm in RuntimeRefUpdate)
            {
                EleRef = EleRef.Replace(itm.Key, itm.Value);
            }

            if (!ElementName.Contains("With Runtime Update"))
            {
                ElementName += " With Runtime Update";
            }
        }


        /* CustomCondition Example:
         *
        private void help()
        {
            Func<IWebDriver, By, (IWebElement, bool)> test = (driver, by) => 
            {
                var element = driver.FindElement(by);
                if (element.GetAttribute("id").Contains("bob"))
                {
                    return (element, true);
                }
                return (null, false);
            };
        }
        */
    }
}
