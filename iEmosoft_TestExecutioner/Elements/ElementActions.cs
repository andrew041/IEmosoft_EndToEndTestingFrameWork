using aUI.Automation.Flutter;
using aUI.Automation.HelperObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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

        // Flutter specific
        FlutterAncestor,
        FlutterText,
        FlutterDescendant,
        FlutterSemanticLabel,
        FlutterTooltipMessage,
        FlutterType,
        FlutterValueKey,
        FlutterPageBack,
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
        private int Counter = 0;
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

            TE.CheckTestTimeLimit();

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
                else if (IsElementTypeFlutter(ele.EleType))
                {
                    ele.Scroll = ele.ScrollableParent is not null; // Only try to scroll if element has a Parent
                    if (starter is not null)
                    {
                        if (starter.RawEle is FlutterElement flutterElement)
                        {
                            element = FindFlutterElement(ele, flutterElement);
                        }
                        else
                        {
                            throw new InvalidOperationException("Starter element must be a Flutter Element");
                        }
                    }
                    else
                    {
                        element = FindFlutterElement(ele);
                    }
                }
                else
                {
                    var finder = ElementFinder(ele);
                    element = FindElement(ele, finder, starter?.RawEle);
                }
            }
            TE.CheckTestTimeLimit();
            return CompleteAction(ele, element);
        }

        public List<ElementResult> ExecuteActions(ElementObject ele, ElementResult starter = null)
        {
            TE.CheckTestTimeLimit();

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
            else if (IsElementTypeFlutter(ele.EleType))
            {
                throw new NotImplementedException("ExecuteActions not supported for flutter elements");
            }
            else
            {
                var finder = ElementFinder(ele);
                elements = FindElements(ele, finder, starter?.RawEle);
            }
            var rtn = new List<ElementResult>();
            foreach (var element in elements)
            {
                TE.CheckTestTimeLimit();
                rtn.Add(CompleteAction(ele, element));
            }
            return rtn;
        }

        private void CircleEle(ElementResult er)
        {
            if (Config.GetConfigSetting("TrackCoverage", "false").ToLower().Equals("true"))
            {
                var ele = er.RawEle;
                var baseFolder = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.Parent.Parent.FullName;
                var baseFileLoc = Config.GetConfigSetting("CoverageFilePath", "");
                var url = TE.CurrentFormName_OrURL;
                url = url.Replace("https://", "").Replace("http://", "");
                var ind = url.IndexOf('/');
                url = url.Substring(ind + 1);
                if (!url.EndsWith('/'))
                {
                    url += "/";
                }

                var lastSection = url.Split('/');
                if (lastSection.Length > 1)
                {
                    var itm = lastSection[^2].Split('?')[0];
                    var num = int.TryParse(itm, out _);
                    var uuid = Guid.TryParse(itm, out _);
                    if (num || uuid)
                    {
                        url = url.Replace(lastSection[^2], "{id}");
                    }
                }

                var filePath = $"{baseFolder}{baseFileLoc}{url}";
                var fileName = $"{filePath}{er.ElementName}+{DateTime.Now.Ticks}.png";//ScreenCapture.NewFileName;

                Screenshot sc = null;
                Counter++;
                try
                {
                    IJavaScriptExecutor jsExecutor = Driver as IJavaScriptExecutor;
                    jsExecutor.ExecuteScript("arguments[0].setAttribute('style', arguments[1]);", ele, "color: fuchsia; border: 7px solid fuchsia;");

                    sc = ((ITakesScreenshot)Driver).GetScreenshot();
                    Bitmap bmp;
                    using (var ms = new MemoryStream(sc.AsByteArray))
                    {
                        bmp = new Bitmap(ms);
                    }

                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }

                    bmp.Save(fileName);

                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    jsExecutor.ExecuteScript("arguments[0].setAttribute('style', arguments[1]);", ele, "");
                }
                catch { }
            }
        }

        private ElementResult CompleteAction(ElementObject eleObj, ElementResult eleRes)
        {
            SelectElement select;
            var ele = eleRes.RawEle;
            var rsp = new ElementResult(TE) { Success = false, RawEle = ele, ElementName = eleObj.ElementName };

            if (ele == null && !(eleObj.Action == ElementAction.Wait && eleObj.WaitType == Wait.Invisible))
            {
                return rsp;
            }

            if (eleObj.Random)
            {
                eleObj.Text = TE.Rand.GetRandomString(eleObj.RandomLength);
            }

            try
            {
                var IsFlutter = IsElementTypeFlutter(eleObj.EleType);
                if (eleObj.Scroll)
                {
                    
                    if (IsFlutter)
                    {
                        if (eleObj.ScrollableParent is null)
                        {
                            throw new InvalidOperationException("The element object is missing a Parent. Cannot scroll.");
                        }
                        ((FlutterDriver)Driver).ScrollTo(FlutterFinder(eleObj.ScrollableParent), FlutterFinder(eleObj));
                    }
                    else
                    {
                        try
                        {
                            rsp.ScrollTo(eleObj.ScrollLoc);
                        } catch { }
                    }
                }
                switch (eleObj.Action)
                {
                    case ElementAction.Click:
                        TE.VerboseLog(new Dictionary<string, string>
                        {
                            {"Action Type", "Click" },
                            {"Element Name", GetElementString(eleObj) },
                        }, "Element Action");
                        CircleEle(rsp);
                        ele.Click();
                        break;
                    case ElementAction.Hover:
                        var act = new Actions(Driver);
                        act.MoveToElement(ele).Build().Perform();
                        TE.Pause(150);//default pause for hover to take effect
                        break;
                    case ElementAction.EnterText:
                        CircleEle(rsp);
                        if (eleObj.Random)
                        {
                            eleObj.Text = TE.Rand.GetRandomString(eleObj.RandomLength);
                        }
                        if (eleObj.Clear)
                        {
                            ele.Clear();

                            ele.SendKeys(Keys.Control + "a");
                            ele.SendKeys(Keys.Backspace);
                        }
                        ele.SendKeys(eleObj.Text);
                        TE.VerboseLog(new Dictionary<string, string>
                        {
                            {"Action Type", "Enter Text" },
                            {"Element Name", GetElementString(eleObj) },
                            {"Text", eleObj.Text },
                        }, "Element Action");
                        break;
                    case ElementAction.Dropdown:
                        CircleEle(rsp);
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
                        TE.VerboseLog(new Dictionary<string, string>
                        {
                            {"Action Type", "Set Dropdown" },
                            {"Element Name", GetElementString(eleObj) },
                            {"Selecting",  eleObj.Random ? "Random" : eleObj.Text },
                        }, "Element Action");
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
                        CircleEle(rsp);
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
                        CircleEle(rsp);
                        select = new SelectElement(ele);
                        if (select.IsMultiple && eleObj.Clear)
                        {
                            select.DeselectAll();
                        }
                        TE.VerboseLog(new Dictionary<string, string>
                        {
                            {"Action Type", "Multidropdown" },
                            {"Select", $"{eleObj.Text}" },
                        }, "Element Actions");
                        foreach (var option in eleObj.Text.Split('|'))
                        {
                            SetDropdown(select, option);
                        }
                        break;
                    case ElementAction.RadioBtn:
                        CircleEle(rsp);
                        bool clicked = false;
                        if (!eleObj.Text.ToLower().Equals(ele.Selected.ToString().ToLower()))
                        {
                            ele.Click();
                            clicked = true;
                        }
                        TE.VerboseLog(new Dictionary<string, string>
                        {
                            {"Action Type", "Radio Button" },
                            {"Element Name", GetElementString(eleObj) },
                            {"Clicked",  $"{clicked}"}
                        }, "Element Action");
                        break;
                    case ElementAction.GetCheckbox:
                        rsp.Text = ele.Selected.ToString();
                        break;
                    case ElementAction.GetText:
                        if (!IsFlutter && ele.TagName.Equals("select"))
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
                            try
                            {
                                rsp.Text = ele.GetAttribute("value");
                            }
                            catch (Exception e)
                            {
                                //ignore errors, just log
                                TE.VerboseLog(new Dictionary<string, string> { { "GetText Error", e.ToString() } }, "Element Action");
                            }
                        }
                        bool found = true;
                        if (rsp.Text == null)
                        {
                            rsp.Text = "";
                            found = false;
                        }
                        TE.VerboseLog(new Dictionary<string, string>
                        {
                            { "Action Type", "Get Text" },
                            { "Element Name", GetElementString(eleObj) },
                            { "Result Text", $"{rsp.Text}" },
                            { "Found", $"{found}" },
                        }, "Element Action");
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
                        //check if wait was successful or not
                        var rtn = false;
                        if ((eleObj.WaitType == Wait.Invisible) == (eleRes.RawEle != null))
                        {
                            rtn = true;
                            rsp.Success = false;
                        }
                        TE.VerboseLog(new Dictionary<string, string>
                            {
                                {"Action Type", "Wait" },
                                {"Element Name", GetElementString(eleObj) },
                                {"Wait Type", $"{eleObj.WaitType}" },
                                {"Max Wait Time", $"{eleObj.MaxWait} seconds" },
                            }, "Element Action");
                        if (rtn)
                        {
                            return rsp;
                        }
                        break;
                    default:
                        throw new NotImplementedException("This action has not been implemented. Please implement it.");
                }
            }
            catch (Exception e)
            {
                TE.CheckTestTimeLimit();
                rsp.Exception = e;
                rsp.Success = false;
                return rsp;
            }
            TE.CheckTestTimeLimit();
            rsp.Success = true;
            return rsp;
        }

        private string GetElementString(ElementObject ele)
        {
            return $"{ele.ElementName} ([{ele.EleType}] {ele.EleRef})";
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
        private FlutterBy FlutterFinder(ElementObject element, ElementObject parent = null)
        {
            return element.EleType switch
            {
                ElementType.FlutterAncestor => throw new NotImplementedException("Ancestor & descendent don't work yet"),
                ElementType.FlutterText => FlutterBy.Text(element.EleRef),
                ElementType.FlutterDescendant => FlutterBy.Descendant(FlutterFinder(element), FlutterFinder(parent)),
                ElementType.FlutterSemanticLabel => FlutterBy.SemanticsLabel(element.EleRef),
                ElementType.FlutterTooltipMessage => FlutterBy.TooltipMessage(element.EleRef),
                ElementType.FlutterType => FlutterBy.Type(element.EleRef),
                ElementType.FlutterValueKey => FlutterBy.ValueKey(element.EleRef),
                ElementType.FlutterPageBack => FlutterBy.PageBack(),
                _ => throw new NotImplementedException("THIS SHOULDNT HAPPEN. Got a none flutter element type")
            };
        }
        private ElementResult FindFlutterElement(ElementObject ele, FlutterElement starter = null)
        {
            FlutterElement element = null;
            var by = FlutterFinder(ele);
            if (ele.UseChild)
            {
                by = FlutterBy.Descendant(FlutterFinder(ele), FlutterFinder(ele.FindFrom));
            }

            // Scroll if necessary to find element
            if (ele.Scroll)
            {
                ((FlutterDriver)Driver).ScrollTo(FlutterFinder(ele.ScrollableParent), by);
                ele.Scroll = false; // We no longer need to scroll before performing actions on the element
            }

            var maxMilliseconds = ele.MaxWait * 1000;
            var startTime = DateTime.Now;
            while (DateTime.Now.Subtract(startTime).TotalMilliseconds < maxMilliseconds && element is null)
            {
                if (starter == null)
                {
                    element = ((FlutterDriver)Driver).FindElement(by);
                }
                else
                {
                    element = (FlutterElement)starter.FindElement(by);
                }
            }

            // Return Success = true if element is not null
            return new ElementResult(TE) { RawEle = element, Success = element is not null };
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
            if (eleRef.MaxWait == 0)
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
                    catch
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
        public static bool IsElementTypeFlutter(ElementType type)
        {
            List<ElementType> flutterTypes = new()
            {
                ElementType.FlutterAncestor,
                ElementType.FlutterText,
                ElementType.FlutterDescendant,
                ElementType.FlutterSemanticLabel,
                ElementType.FlutterTooltipMessage,
                ElementType.FlutterType,
                ElementType.FlutterValueKey,
                ElementType.FlutterPageBack,
            };
            return flutterTypes.Contains(type);
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

            js.ExecuteScript($"arguments[0].scrollIntoView({{block: \"{location}\", inline: \"center\"}});", elementRef.RawEle);
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
