﻿using aUI.Automation.Interfaces;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
namespace aUI.Automation.UIDrivers
{
    public class WindowsWhite : IUIDriver
    {
        public bool ScreenContains(string lookFor)
        {
            throw new NotImplementedException();
        }

        public string DriverType { get { return "White"; } }

        public List<string> FailedBrowsers { get { return new List<string> { DriverType }; } }

        public void SetTextOnControl(string controlIdOrCssSelector, string textToSet)
        {
            throw new NotImplementedException();
        }

        public void SetTextOnControl(string attributeName, string attributeValue, string textToSet, string controlType = "", bool useWildCardSearch = true, int retryForSeconds = 10)
        {
            throw new NotImplementedException();
        }

        public void ClickControl(string controlIdOrCssSelector)
        {
            throw new NotImplementedException();
        }

        public void ClickControl(string attributeName, string attributeValue, string controlType = "", bool useWildCardSearch = true, int retryForSeconds = 10)
        {
            throw new NotImplementedException();
        }

        public string GetTextOnControl(string controlIdOrCssSelector)
        {
            throw new NotImplementedException();
        }

        public IWebDriver RawWebDriver { get { return null; } }

        public string GetTextOnControl(string attributeName, string attributeValue, string controlType = "", bool useWildCardSearch = true, int retryForSeconds = 10)
        {
            throw new NotImplementedException();
        }

        public bool AmOnSceen(string snippetToLookFor)
        {
            throw new NotImplementedException();
        }

        public void SetValueOnDropDown(string controlIdOrCssSelector, string valueToSet)
        {
            throw new NotImplementedException();
        }

        public void SetValueOnDropDown(string attributeName, string attributeValue, string valueToSet = "", bool useWildCardSearch = true, int retryForSeconds = 10)
        {
            throw new NotImplementedException();
        }

        public bool IsCheckBoxChecked(string controlIdOrCssSelector)
        {
            throw new NotImplementedException();
        }

        public bool IsCheckBoxChecked(string attributeName, string attributeValue, string controlType = "", bool useWildCardSearch = true, int retryForSeconds = 10)
        {
            throw new NotImplementedException();
        }

        public void SetCheckBoxValueTo(string controlIdOrCssSelector, bool valueItShouldBeSetTo)
        {
            throw new NotImplementedException();
        }

        public void SetCheckBoxValueTo(string attributeName, string attributeValue, bool valueItShouldBeSetTo, string controlType = "", bool useWildCardSearch = true, int retryForSeconds = 10)
        {
            throw new NotImplementedException();
        }

        public string GetTextOnDropDown(string controlIdOrCssSelector)
        {
            throw new NotImplementedException();
        }

        public string GetTextOnDropDown(string attributeName, string attributeValue, string controlType = "", bool useWildCardSearch = true, int retryForSeconds = 10)
        {
            throw new NotImplementedException();
        }

        public string GetValueOnDropDown(string controlIdOrCssSelector)
        {
            throw new NotImplementedException();
        }

        public string GetValueOnDropDown(string attributeName, string attributeValue, string controlType = "", bool useWildCardSearch = true, int retryForSeconds = 10)
        {
            throw new NotImplementedException();
        }

        public void NavigateTo(string windowNameOrUri)
        {
            throw new NotImplementedException();
        }

        public void Launch(string appNameOrUri)
        {
            throw new NotImplementedException();
        }

        public void Pause(int milliseconds)
        {
            throw new NotImplementedException();
        }


        public string CurrentFormName_OrPageURL
        {
            get { throw new NotImplementedException(); }
        }


        public void MaximizeWindow()
        {
            throw new NotImplementedException();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "<Pending>")]
        public void Dispose()
        {
            throw new NotImplementedException();
        }

    }
}
