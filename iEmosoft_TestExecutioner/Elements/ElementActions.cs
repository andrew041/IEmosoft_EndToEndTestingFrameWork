using aUI.Automation.HelperObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Internal;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace aUI.Automation.Elements
{
    public enum ElementAction
    {
        //active
        Click,
        EnterText,
        Dropdown,
        DropdownIndex,
        RadioBtn,
        MultiDropdown,
        Hover,

        //passive
        GetText,
        GetCheckbox,
        GetAttribute,
        GetCSS,
        GetProperty,
        Wait,
        GetDropdown,
    }

    public enum ElementType
    {
        Id,
        Class,
        Name,
        Xpath,
        CSS,
        LinkText,
        Tag,
        PartialLinkText,

        //Appium
        AccessabilityId,
    }

    public enum Wait
    {
        Visible,
        Clickable,
        Selected,
        Invisible,
        ContainsText,
        Custom,
        Presence,
    }

    public class ElementActions
    {
        private TestExecutioner TE;
        private IWebDriver Driver;
        private string MobileMultiMap = Config.GetConfigSetting("MobileEleIdMap", "content-desc");
        //TODO add & improve assertions at this level
        //this will require a class for customized assertions to track

        public ElementActions(TestExecutioner tE)
        {
            TE = tE ?? throw new ArgumentNullException(nameof(tE));
            Driver = TE.RawSeleniumWebDriver_AvoidCallingDirectly;

        }

        //general method to handle actions

        //general method to handle action on existing elements

        //helper method to complete the work/actions



        //ensure good assertions

        //https://stackoverflow.com/questions/2082615/pass-method-as-parameter-using-c-sharp

        public ElementResult ExecuteAction(ElementObject ele, ElementResult starter = null)
        {
            if(TE.TestTimeLimit < DateTime.Now)
            {
                TE.Assert.Fail($"Test Exceeded Max Time Limit of: {TE.TestTimeLimit.Subtract(TE.StartTime)} (hh:mm:ss)");
            }

            var eleName = starter == null ? ele.ElementName : starter.ElementName;
            if (ele.ReportStep)
            {
                switch (ele.Action)
                {
                    case ElementAction.Click:
                    case ElementAction.EnterText:
                    case ElementAction.Dropdown:
                    case ElementAction.DropdownIndex:
                    case ElementAction.RadioBtn:
                    case ElementAction.MultiDropdown:
                    case ElementAction.Hover:
                        TE.BeginTestCaseStep($"Execute action {ele.Action} on element: {eleName}",
                            ele.Random || ele.ProtectedValue ? "Random Value" : ele.Text);
                        break;
                }
            }
            var element = starter;
            //check if 'ele' has an element in it or not.
            if (!string.IsNullOrEmpty(ele.EleRef))
            {
                if (ele.EleType == ElementType.AccessabilityId)
                {
                    element = FindAppiumElement(ele, starter?.RawEle);
                }
                else
                {
                    var finder = ElementFinder(ele);
                    element = FindElement(ele, finder, starter?.RawEle);
                }
            }

            return CompleteAction(ele, element);
        }

        public List<ElementResult> ExecuteActions(ElementObject ele, ElementResult starter = null)
        {
            if (TE.TestTimeLimit < DateTime.Now)
            {
                TE.Assert.Fail($"Test Exceeded Max Time Limit of: {TE.TestTimeLimit.Subtract(TE.StartTime).TotalSeconds} seconds");
            }

            var eleName = starter == null ? ele.ElementName : starter.ElementName;

            if (ele.ReportStep)
            {
                switch (ele.Action)
                {
                    case ElementAction.Click:
                    case ElementAction.EnterText:
                    case ElementAction.Dropdown:
                    case ElementAction.DropdownIndex:
                    case ElementAction.RadioBtn:
                    case ElementAction.MultiDropdown:
                    case ElementAction.Hover:
                        TE.BeginTestCaseStep($"Execute action {ele.Action} on elements: {eleName}",
                            ele.Random || ele.ProtectedValue ? "Random Value" : ele.Text);
                        break;
                }
            }
            List<ElementResult> elements = null;
            if (ele.EleType == ElementType.AccessabilityId)
            {
                elements = FindAppiumElements(ele, starter?.RawEle);
            }
            else
            {
                var finder = ElementFinder(ele);
                elements = FindElements(ele, finder, starter?.RawEle);
            }
            var rtn = new List<ElementResult>();
            elements.ForEach(x => rtn.Add(CompleteAction(ele, x)));
            return rtn;
        }

        private ElementResult CompleteAction(ElementObject eleObj, ElementResult eleRes)
        {
            SelectElement select;
            var ele = eleRes.RawEle;
            var rsp = new ElementResult(TE) { Success = false, RawEle = ele, ElementName = eleObj.ElementName };

            if (ele == null)
            {
                return rsp;
            }

            if (eleObj.Random)
            {
                eleObj.Text = TE.Rand.GetRandomString(eleObj.RandomLength);
            }

            try
            {
                if (eleObj.Scroll)
                {
                    try
                    {
                        rsp.ScrollTo(eleObj.ScrollLoc);
                    }
                    catch
                    {
                        try
                        {
                            //scroll till element is within the center 50% of the screen??????????? Just not sure how to drag..
                            //TouchActions action = new TouchActions(Driver);

                            //var visibleText = "Submit";
                            //((AndroidDriver<IWebElement>)Driver).FindElementByAndroidUIAutomator("new UiScrollable(new UiSelector().scrollable(true).instance(0)).scrollIntoView(new UiSelector().textContains(\"" + visibleText + "\").instance(0))");

                            //TouchActions action = new TouchActions(Driver);
                            //action.Scroll(ele, 10, 100).Perform();
                        }
                        catch (Exception e) { Console.WriteLine(e.Message); }
                    }
                    //                    (((IWrapsDriver)ele).WrappedDriver as IJavaScriptExecutor).ScrollToElement(ele);
                }

                switch (eleObj.Action)
                {
                    case ElementAction.Click:
                        ele.Click();
                        break;
                    case ElementAction.Hover:
                        var act = new Actions(Driver);
                        act.MoveToElement(ele).Build().Perform();
                        TE.Pause(150);//default pause for hover to take effect
                        break;
                    case ElementAction.EnterText:
                        if (eleObj.Clear)
                        {
                            ele.Clear();

                            ele.SendKeys(Keys.Control + "a");
                            ele.SendKeys(Keys.Backspace);
                        }
                        ele.SendKeys(eleObj.Text);
                        break;
                    case ElementAction.Dropdown:
                        if (eleObj.Random)
                        {
                            eleObj.Action = ElementAction.DropdownIndex;
                            return CompleteAction(eleObj, eleRes);
                        }

                        select = new SelectElement(ele);
                        if (select.IsMultiple && eleObj.Clear)
                        {
                            select.DeselectAll();
                        }
                        rsp.Text = SetDropdown(select, eleObj.Text);
                        break;
                    case ElementAction.GetDropdown:
                        select = new SelectElement(ele);
                        if (select.IsMultiple)
                        {
                            var tempLst = new List<string>();
                            select.AllSelectedOptions.ToList().ForEach(x => tempLst.Add(x.Text));
                            rsp.Text = string.Join('|', tempLst);
                        }
                        else
                        {
                            rsp.Text = select.SelectedOption.Text;
                        }
                        break;
                    case ElementAction.DropdownIndex:
                        select = new SelectElement(ele);
                        if (select.IsMultiple && eleObj.Clear)
                        {
                            select.DeselectAll();
                        }

                        var start = eleObj.Text.Length > 1 ? 1 : 0;

                        var index = TE.Rand.Rnd.Next(start, select.Options.Count);
                        if (int.TryParse(eleObj.Text, out int indexVal) || !eleObj.Random)
                        {
                            index = indexVal;
                        }
                        select.SelectByIndex(index);
                        rsp.Text = select.Options[index].Text;
                        break;
                    case ElementAction.MultiDropdown:
                        select = new SelectElement(ele);
                        if (select.IsMultiple && eleObj.Clear)
                        {
                            select.DeselectAll();
                        }

                        foreach (var option in eleObj.Text.Split('|'))
                        {
                            SetDropdown(select, option);
                        }
                        break;
                    case ElementAction.RadioBtn:
                        if (!eleObj.Text.ToLower().Equals(ele.Selected.ToString().ToLower()))
                        {
                            ele.Click();
                        }
                        break;
                    case ElementAction.GetCheckbox:
                        rsp.Text = ele.Selected.ToString();
                        break;
                    case ElementAction.GetText:
                        if (ele.TagName.Equals("select"))
                        {
                            select = new SelectElement(ele);
                            if (select.IsMultiple)
                            {
                                var ops = new List<string>();
                                select.AllSelectedOptions.ToList().ForEach(x => ops.Add(x.Text));
                                rsp.Text = string.Join("\n", ops);
                            }
                            else
                            {
                                rsp.Text = select.SelectedOption.Text;
                            }
                        }
                        else
                        {
                            rsp.Text = ele.Text;
                        }

                        if (string.IsNullOrEmpty(rsp.Text))
                        {
                            rsp.Text = ele.GetAttribute("value");
                        }

                        if (rsp.Text == null)
                        {
                            rsp.Text = "";
                        }

                        break;
                    case ElementAction.GetAttribute:
                        rsp.Text = ele.GetAttribute(eleObj.Text);
                        break;
                    case ElementAction.GetCSS:
                        rsp.Text = ele.GetCssValue(eleObj.Text);
                        break;
                    case ElementAction.GetProperty:
                        rsp.Text = ele.GetProperty(eleObj.Text);
                        break;
                    case ElementAction.Wait:
                        //nothing to do here
                        break;
                    default:
                        throw new NotImplementedException("This action has not been implemented. Please implement it.");
                }
            }
            catch (Exception e)
            {
                rsp.Exception = e;
                rsp.Success = false;
                return rsp;
            }

            rsp.Success = true;
            return rsp;
        }

        private string SetDropdown(SelectElement select, string desired)
        {
            var optionList = new List<string>();
            foreach (var option in select.Options)
            {
                optionList.Add(option.Text);
            }

            var found = optionList.Contains(desired);
            if (!found)
            {
                desired = optionList.FirstOrDefault(x => x.ToLower().Contains(desired.ToLower()));
                found = !string.IsNullOrEmpty(desired);
            }

            if (found)
            {
                select.SelectByText(desired);
            }
            else
            {
                select.SelectByValue(desired);
            }

            return desired;
        }

        public By ElementFinder(ElementObject ele)
        {
            return ele.EleType switch
            {
                ElementType.Id => By.Id(ele.EleRef),
                ElementType.Class => By.ClassName(ele.EleRef),
                ElementType.Name => By.Name(ele.EleRef),
                ElementType.Xpath => By.XPath(ele.EleRef),
                ElementType.CSS => By.CssSelector(ele.EleRef),
                ElementType.LinkText => By.LinkText(ele.EleRef),
                ElementType.Tag => By.TagName(ele.EleRef),
                ElementType.PartialLinkText => By.PartialLinkText(ele.EleRef),
                _ => null,
            };
        }

        private ElementResult FindAppiumElement(ElementObject ele, IWebElement starter = null)
        {
            var start = DateTime.Now;

            while (DateTime.Now.Subtract(start).TotalSeconds < ele.MaxWait)
            {
                try
                {
                    IWebElement temp;
                    if (starter == null)
                    {
                        temp = ((AppiumDriver<IWebElement>)Driver).FindElementByAccessibilityId(ele.EleRef);
                    }
                    else
                    {
                        temp = starter.FindElement(By.XPath($".//*[@{MobileMultiMap}='{ele.EleRef}']"));
                    }

                    var element = new ElementResult(TE) { RawEle = temp, Success = false };

                    switch (ele.WaitType)
                    {
                        case Wait.Visible:
                            element.Success = temp.Displayed;
                            break;
                        case Wait.Clickable:
                            element.Success = temp.Displayed & temp.Enabled;
                            break;
                        case Wait.Selected:
                            element.Success = temp.Selected;
                            break;
                        case Wait.ContainsText:
                            element.Success = temp.Text.Contains(ele.Text);
                            break;
                        case Wait.Custom:
                            //TODO handle this case
                            start = DateTime.Now.Subtract(new TimeSpan(100, 0, 0));
                            throw new NotImplementedException();
                        case Wait.Presence:
                            element.Success = temp != null;
                            break;
                    }
                    if (element.Success)
                    {
                        return element;
                    }
                }
                catch
                {
                    if (ele.WaitType == Wait.Invisible)
                    {
                        return new ElementResult(TE) { Success = true };
                    }
                }
            }

            return new ElementResult(TE) { Success = false };
        }

        private List<ElementResult> FindAppiumElements(ElementObject ele, IWebElement starter = null)
        {
            var start = DateTime.Now;

            while (DateTime.Now.Subtract(start).TotalSeconds < ele.MaxWait)
            {
                try
                {
                    IReadOnlyCollection<IWebElement> elements;
                    if (starter == null)
                    {
                        elements = ((AppiumDriver<IWebElement>)Driver).FindElementsByAccessibilityId(ele.EleRef);
                    }
                    else
                    {
                        elements = starter.FindElements(By.XPath($".//*[@{MobileMultiMap}='{ele.EleRef}']"));
                    }

                    var success = false;
                    var eleList = new List<ElementResult>();
                    foreach (var temp in elements)
                    {
                        var element = new ElementResult(TE) { RawEle = temp, Success = false };

                        switch (ele.WaitType)
                        {
                            case Wait.Visible:
                                element.Success = temp.Displayed;
                                break;
                            case Wait.Clickable:
                                element.Success = temp.Displayed & temp.Enabled;
                                break;
                            case Wait.Selected:
                                element.Success = temp.Selected;
                                break;
                            case Wait.ContainsText:
                                element.Success = temp.Text.Contains(ele.Text);
                                break;
                            case Wait.Custom:
                                //TODO handle this case
                                start = DateTime.Now.Subtract(new TimeSpan(100, 0, 0));
                                throw new NotImplementedException();
                            case Wait.Presence:
                                element.Success = temp != null;
                                break;
                        }
                        if (!element.Success)
                        {
                            success = false;
                        }
                        eleList.Add(element);
                    }
                    if (success)
                    {
                        return eleList;
                    }
                }
                catch
                {
                    if (ele.WaitType == Wait.Invisible)
                    {
                        return new List<ElementResult>() { new ElementResult(TE) { Success = true } };
                    }
                }
            }

            return new List<ElementResult>() { new ElementResult(TE) { Success = false } };
        }

        private ElementResult FindElement(ElementObject eleRef, By by, IWebElement starter = null)
        {
            if(eleRef.MaxWait == 0)
            {
                try
                {
                    if (starter == null)
                    {
                        return new ElementResult(TE) { RawEle = Driver.FindElement(by), Success = true };
                    }
                    return new ElementResult(TE) { RawEle = starter.FindElement(by), Success = true };
                }
                catch
                {
                    return new ElementResult(TE) { RawEle = null, Success = false };
                }
                
            }
            var wait = new WebDriverWait(Driver, new TimeSpan(0, 0, eleRef.MaxWait));
            wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(ElementNotVisibleException),
                typeof(ElementNotInteractableException));
            var sucess = false;
            IWebElement element = null;
            try
            {
                sucess = wait.Until(condition =>
                {
                    try
                    {
                        if (starter == null)
                        {
                            //TODO Enable custom filter for elements from other elements
                            if (eleRef.WaitType == Wait.Custom)
                            {
                                if (eleRef.CustomCondition != null)
                                {
                                    var rsp = eleRef.CustomCondition(Driver, by);
                                    element = rsp.Item1;
                                    return rsp.Item2;
                                }
                            }

                            element = Driver.FindElement(by);
                        }
                        else
                        {
                            element = starter.FindElement(by);
                        }

                        return eleRef.WaitType switch
                        {
                            Wait.Clickable => element.Displayed && element.Enabled,
                            Wait.Visible => element.Displayed,
                            Wait.Selected => element.Selected,
                            Wait.Invisible => !element.Displayed,
                            Wait.Presence => element != null,
                            Wait.ContainsText => element.Text.Contains(eleRef.Text),
                            _ => true,

                        };
                    }
                    catch (Exception e)
                    {
                        return eleRef.WaitType == Wait.Invisible;
                    }
                });
            }
            catch
            {
                return new ElementResult(TE) { RawEle = null, Success = false };
            }

            return new ElementResult(TE) { RawEle = element, Success = sucess };
        }

        private List<ElementResult> FindElements(ElementObject eleRef, By by, IWebElement starter = null)
        {
            var retur = new List<ElementResult>();

            if (eleRef.MaxWait == 0)
            {
                try
                {
                    List<IWebElement> found;
                    if (starter == null)
                    {
                        found = Driver.FindElements(by).ToList();
                    }
                    else
                    {
                        found = starter.FindElements(by).ToList();
                    }
                    found.ForEach(x => retur.Add(new ElementResult(TE) { RawEle = x, Success = true }));
                    return retur;
                }
                catch
                {
                    return retur;
                }

            }
            var wait = new WebDriverWait(Driver, new TimeSpan(0, 0, eleRef.MaxWait));
            wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(ElementNotVisibleException),
                typeof(ElementNotInteractableException));

            List<IWebElement> elements = null;

            try
            {
                var sucess = wait.Until(condition =>
                {
                    try
                    {
                        if (starter == null)
                        {
                            if (eleRef.WaitType == Wait.Custom)
                            {
                                if (eleRef.CustomConditionMulti != null)
                                {
                                    var rsp = eleRef.CustomConditionMulti(Driver, by);
                                    elements = rsp.Item1;
                                    return rsp.Item2;
                                }
                            }

                            elements = Driver.FindElements(by).ToList();
                        }
                        else
                        {
                            elements = starter.FindElements(by).ToList();
                        }

                        var pass = true;

                        foreach (var element in elements)
                        {
                            var val = eleRef.WaitType switch
                            {
                                Wait.Clickable => element.Displayed && element.Enabled,
                                Wait.Visible => element.Displayed,
                                Wait.Selected => element.Selected,
                                Wait.Invisible => !element.Displayed,
                                Wait.Presence => element != null,
                                Wait.ContainsText => element.Text.Contains(eleRef.Text),
                                _ => true,
                            };

                            if (!val)
                            {
                                pass = false;
                            }
                        }

                        return pass;
                    }
                    catch (Exception)
                    {
                        return eleRef.WaitType == Wait.Invisible;
                    }
                });

                elements.ForEach(x => retur.Add(new ElementResult(TE) { RawEle = x, Success = sucess }));
            }
            catch //(Exception e) 
            {
                //TE.FailCurrentStep(e);
                //throw new NotFoundException("The desired element was not found in expected state");
            }

            return retur;
        }
    }

    public static class ElementActionExtender
    {
        public static ElementResult ExecuteAction(this ElementResult elementRef, ElementObject ele)
        {
            if (ele == null) { ele = new ElementObject(); }
            var ea = new ElementActions(elementRef.TE);
            return ea.ExecuteAction(ele, elementRef);
        }

        public static List<ElementResult> ExecuteActions(this ElementResult elementRef, ElementObject ele)
        {
            if (!elementRef.Success)
            {
                return new List<ElementResult>() { new ElementResult(elementRef.TE) };
            }

            if (ele == null) { ele = new ElementObject(); }
            var ea = new ElementActions(elementRef.TE);
            return ea.ExecuteActions(ele, elementRef);
        }

        #region Single Element Actions
        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementRef"></param>
        /// <param name="location">'start' 'center' 'end' or 'nearest'</param>
        public static void ScrollTo(this ElementResult elementRef, string location = "center")
        {
            IJavaScriptExecutor js = elementRef.TE.RawSeleniumWebDriver_AvoidCallingDirectly as IJavaScriptExecutor;

            if (string.IsNullOrEmpty(location))
            {
                js.ExecuteScript($"arguments[0].scrollIntoView(true);", elementRef.RawEle);
                return;
            }

            js.ExecuteScript($"arguments[0].scrollIntoView({{block: \"{location}\"}});", elementRef.RawEle);
        }
        public static ElementResult Click(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.Click;
            return elementRef.ExecuteAction(ele);
        }
        public static ElementResult Hover(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.Hover;
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult EnterText(this ElementResult elementRef, string text)
        {
            var ele = new ElementObject { Action = ElementAction.EnterText, Text = text };
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult EnterText(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.EnterText;
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult SelectDropdown(this ElementResult elementRef, string option)
        {
            var ele = new ElementObject { Action = ElementAction.Dropdown, Text = option };
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult SelectDropdown(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.Dropdown;
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult MultiDropdown(this ElementResult elementRef, string option)
        {
            var ele = new ElementObject { Action = ElementAction.MultiDropdown, Text = option };
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult MultiDropdown(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.MultiDropdown;
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult DropdownIndex(this ElementResult elementRef, int index)
        {
            var ele = new ElementObject { Action = ElementAction.DropdownIndex, Text = index.ToString() };
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult DropdownIndex(this ElementResult elementRef, ElementObject ele = null)
        {
            ele.Action = ElementAction.DropdownIndex;
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult RadioBtn(this ElementResult elementRef, bool selected = true)
        {
            var ele = new ElementObject { Action = ElementAction.RadioBtn, Text = selected.ToString() };
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult RadioBtn(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.RadioBtn;
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult GetText(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.GetText;
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult GetText(this ElementResult elementRef, Enum ele)
        {
            var element = new ElementObject(ele)
            {
                Action = ElementAction.GetText
            };
            return elementRef.ExecuteAction(element);
        }

        public static ElementResult GetDropdown(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.GetDropdown;
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult GetDropdown(this ElementResult elementRef, Enum ele)
        {
            var element = new ElementObject(ele)
            {
                Action = ElementAction.GetDropdown
            };
            return elementRef.ExecuteAction(element);
        }

        public static ElementResult GetCheckbox(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null)
            {
                ele = new ElementObject { Action = ElementAction.GetCheckbox };
            }
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult GetAttribute(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.GetAttribute;
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult GetAttribute(this ElementResult elementRef, string attribute)
        {
            var ele = new ElementObject() { Text = attribute, Action = ElementAction.GetAttribute };
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult GetCSS(this ElementResult elementRef)
        {
            var ele = new ElementObject { Action = ElementAction.GetCSS };
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult GetProperty(this ElementResult elementRef)
        {
            var ele = new ElementObject { Action = ElementAction.GetProperty };
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult WaitFor(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.Wait;
            return elementRef.ExecuteAction(ele);
        }

        public static ElementResult WaitFor(this ElementResult elementRef, Enum ele)
        {
            var element = new ElementObject(ele)
            {
                Action = ElementAction.Wait
            };
            return elementRef.ExecuteAction(element);
        }
        #endregion

        #region Multi Element Actions

        public static List<ElementResult> ClickAll(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.Click;
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> EnterTexts(this ElementResult elementRef, string text)
        {
            var ele = new ElementObject { Action = ElementAction.EnterText, Text = text };
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> EnterTexts(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.EnterText;
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> SelectDropdowns(this ElementResult elementRef, string option)
        {
            var ele = new ElementObject { Action = ElementAction.Dropdown, Text = option };
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> SelectDropdowns(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.Dropdown;
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> MultiDropdowns(this ElementResult elementRef, string option)
        {
            var ele = new ElementObject { Action = ElementAction.MultiDropdown, Text = option };
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> MultiDropdowns(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.MultiDropdown;
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> DropdownIndexes(this ElementResult elementRef, int index)
        {
            var ele = new ElementObject { Action = ElementAction.DropdownIndex, Text = index.ToString() };
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> DropdownIndexes(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.DropdownIndex;
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> RadioBtns(this ElementResult elementRef, bool selected = true)
        {
            var ele = new ElementObject { Action = ElementAction.RadioBtn, Text = selected.ToString() };
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> RadioBtns(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.RadioBtn;
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> GetTexts(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.GetText;
            //var ele = new ElementObject { Action = ElementAction.GetText };
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> GetTexts(this ElementResult elementRef, Enum ele)
        {
            var element = new ElementObject(ele) { Action = ElementAction.GetText };
            return elementRef.ExecuteActions(element);
        }

        public static List<ElementResult> GetCheckboxes(this ElementResult elementRef)
        {
            var ele = new ElementObject { Action = ElementAction.GetCheckbox };
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> GetAttributes(this ElementResult elementRef)
        {
            var ele = new ElementObject { Action = ElementAction.GetAttribute };
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> GetCSSs(this ElementResult elementRef)
        {
            var ele = new ElementObject { Action = ElementAction.GetCSS };
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> GetProperties(this ElementResult elementRef)
        {
            var ele = new ElementObject { Action = ElementAction.GetProperty };
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> WaitForAll(this ElementResult elementRef, string text)
        {
            var ele = new ElementObject { Action = ElementAction.Wait, Text = text };
            return elementRef.ExecuteActions(ele);
        }

        public static List<ElementResult> WaitForAll(this ElementResult elementRef, ElementObject ele = null)
        {
            if (ele == null) { ele = new ElementObject(); }
            ele.Action = ElementAction.Wait;
            return elementRef.ExecuteActions(ele);
        }
        #endregion
    }
}
